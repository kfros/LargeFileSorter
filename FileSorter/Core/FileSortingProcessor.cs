using FileSorter.Comparers;
using FileSorter.Comparers.Comparands;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
namespace FileSorter.Core
{
    public static class FileSortingProcessor
    {
        public static void SortParallel(
        string inputPath,
        string outputPath,
        string? tempDirectory = null,
        long memoryBudgetBytes = 4L * 1024 * 1024 * 1024,
        int maxChunkWorkers = -1, // Use a sentinel value
        int maxMergeFanIn = 64,
        int? maxParallelMerges = null,
        ProgressReporter? progress = null // Changed to nullable
        )
        {
            if (maxChunkWorkers == -1) // Assign at runtime if sentinel value is used
            {
                maxChunkWorkers = Math.Max(1, Environment.ProcessorCount / 2);
            }

            maxParallelMerges ??= Math.Max(1, Environment.ProcessorCount / 2); // Assign value at runtime

            if (string.IsNullOrWhiteSpace(tempDirectory))
                tempDirectory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".", "tmp-runs");
            Directory.CreateDirectory(tempDirectory);

            var runFiles = GenerateRunsParallel(inputPath, tempDirectory, memoryBudgetBytes, maxChunkWorkers, progress);

            try
            {
                // Parallel merge passes until a single file remains
                while (runFiles.Count > 1)
                {
                    runFiles = MergePassParallel(runFiles, tempDirectory, maxMergeFanIn, maxParallelMerges.Value, progress);
                }

                SafeDelete(outputPath);
                if (runFiles.Count == 1) File.Move(runFiles[0], outputPath);
                else File.WriteAllBytes(outputPath, Array.Empty<byte>());
            }
            finally
            {
                // Cleanup stray runs
                foreach (var f in Directory.EnumerateFiles(tempDirectory, "*.run"))
                    SafeDelete(f);
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(tempDirectory).Any())
                        Directory.Delete(tempDirectory);
                }
                catch { /* ignore */ }
            }
        }


        //  Run generation: producer/consumer 
        private static List<string> GenerateRunsParallel(string inputPath, string tempDir, long memoryBudgetBytes, int maxChunkWorkers, ProgressReporter? progress)
        {
            long targetChunkBytes = Math.Clamp(memoryBudgetBytes * 85 / 100,
                                   256L * 1024 * 1024,
                                   1L * 1024 * 1024 * 1024);
            var runFiles = new ConcurrentBag<string>();

            // Queue of raw lines per chunk (keeps GC pressure low by not keeping huge strings beyond chunk lifetime)
            using var chunks = new BlockingCollection<List<string>>(boundedCapacity: maxChunkWorkers * 2);

            // Workers
            var cts = new CancellationTokenSource();
            var workers = new Task[maxChunkWorkers];
            for (int w = 0; w < maxChunkWorkers; w++)
            {
                workers[w] = Task.Run(() =>
                {
                    foreach (var lines in chunks.GetConsumingEnumerable(cts.Token))
                    {
                        // Parse -> sort -> write run
                        var items = new List<Item>(lines.Count);
                        foreach (var line in lines)
                        {
                            if (Parser.TryParseLine(line, out var text, out var number))
                                items.Add(new Item(text, number, line));
                            else
                                items.Add(new Item("\uFFFF" + line, int.MaxValue, line));
                        }
                        items.Sort(ItemComparer.Instance);

                        string path = Path.Combine(tempDir, $"run_{Guid.NewGuid():N}.run");
                        using var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
                        using var sw = new StreamWriter(fs, new UTF8Encoding(false), bufferSize: 1 << 20);
                        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"[run] writing... size~{items.Count:N0} lines"));
                        foreach (var it in items) sw.WriteLine(it.Raw);
                        runFiles.Add(path);
                    }
                }, cts.Token);
            }

            // Producer: read input sequentially and form chunks under targetChunkBytes
            try
            {
                using var fs = File.OpenRead(inputPath);
                using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1 << 20);

                var current = new List<string>(1 << 20);

                long currentBytes = 0, lines = 0;
                string? line;
                long lastPos = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    current.Add(line);
                    currentBytes += EstimatedSizeOf(line);
                    lines++;
                    if (lines % 100_000 == 0) 
                    {
                        progress?.AddLines(100_000);
                        long pos = fs.Position;                 // actual bytes consumed from file
                        progress?.AddBytes(pos - lastPos);
                        lastPos = pos;
                    }
                    if (currentBytes >= targetChunkBytes)
                    {
                        chunks.Add(current, cts.Token);
                        Console.WriteLine($"[chunk] queued lines={current.Count}");
                        progress?.AddBytes(currentBytes);                                    // << progress
                        current = new List<string>(current.Count);
                        currentBytes = 0;
                    }
                }
                progress?.AddBytes(fs.Position - lastPos);
                if (current.Count > 0) { chunks.Add(current, cts.Token); progress?.AddBytes(currentBytes); }
            }
            finally
            {
                chunks.CompleteAdding();
                progress?.IncRuns();
                try { Task.WaitAll(workers); } catch (AggregateException) { /* bubble via cts if needed */ }
            }
            Console.WriteLine($"[run] done");
            return runFiles.ToList();
        }

        private static List<string> MergePassParallel(List<string> runs, string tempDir, int fanIn, int maxParallelMerges, ProgressReporter? progress)
        {
            var nextPass = new ConcurrentBag<string>();

            Parallel.ForEach(
                Partitioner.Create(0, runs.Count, Math.Max(fanIn, 1)),
                new ParallelOptions { MaxDegreeOfParallelism = maxParallelMerges },
                range =>
                {
                    for (int i = range.Item1; i < range.Item2; i += fanIn)
                    {
                        var group = runs.GetRange(i, Math.Min(fanIn, runs.Count - i));
                        string merged = Path.Combine(tempDir, $"merge_{Guid.NewGuid():N}.run");
                        MergeRuns(group, merged);
                        nextPass.Add(merged);
                        foreach (var f in group) SafeDelete(f);
                        progress?.IncMerges();
                    }
                });

            return nextPass.ToList();
        }
        private static void MergeRuns(IReadOnlyList<string> runFiles, string outputPath)
        {
            var readers = new StreamReader[runFiles.Count];

            try
            {
                // Open all run readers
                for (int i = 0; i < runFiles.Count; i++)
                {
                    readers[i] = new StreamReader(
                        File.Open(runFiles[i], FileMode.Open, FileAccess.Read, FileShare.Read),
                        Encoding.UTF8,

                        detectEncodingFromByteOrderMarks: true,
                        bufferSize: 1 << 20);
                }

                using var outFs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
                using var outSw = new StreamWriter(outFs, new UTF8Encoding(false), bufferSize: 1 << 20);

                var pq = new PriorityQueue<HeapNode, HeapKey>(runFiles.Count, HeapKeyComparer.Instance);

                // Prime the heap
                for (int i = 0; i < readers.Length; i++)
                {
                    if (Parser.TryReadParsed(readers[i], out var line, out var text, out var number))
                    {
                        pq.Enqueue(new HeapNode(i, line, text, number), new HeapKey(text, number));
                    }
                }

                // Merge
                while (pq.Count > 0)
                {
                    pq.TryDequeue(out var node, out _);
                    outSw.WriteLine(node.Raw);

                    if (Parser.TryReadParsed(readers[node.SourceIndex], out var line, out var text, out var number))
                    {
                        pq.Enqueue(new HeapNode(node.SourceIndex, line, text, number), new HeapKey(text, number));
                    }
                }
            }
            finally
            {
                // Dispose all readers even if anything failed above
                for (int i = 0; i < readers.Length; i++)
                {
                    try { readers[i]?.Dispose(); } catch { /* best-effort */ }
                }
            }
        }

        private static long EstimatedSizeOf(string raw) => (raw.Length * 2L) + 64;

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
        }
    }
}