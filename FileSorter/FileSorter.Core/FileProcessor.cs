// See https://aka.ms/new-console-template for more information
using FileSorter.Comparers;
using FileSorter.Parsing;
using FileSorter.Structs;

namespace FileSorter.Core;
public class FileProcessor
{
    const int bufferSize = 4 * 1024 * 1024; // 4MB buffer
    const long maxBatchSize = 2 * 1024L * 1024 * 1024; // 2GB batches
    public List<string> SplitFileToFragments(string inputFile, string tempDir)
    {
        var tempFiles = new List<string>();
        using var reader = new StreamReader(
            new BufferedStream(
                new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize),
                bufferSize
            )
        );

        var batch = new List<string>(1_000_000);
        long currentBatchSize = 0;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line != null)
            {
                batch.Add(line);
                currentBatchSize += line.Length * 2;
            }

            if (currentBatchSize >= maxBatchSize)
            {
                tempFiles.Add(SortBatch(batch, tempDir));
                batch.Clear();
                currentBatchSize = 0;
            }
        }

        if (batch.Count > 0)
        {
            tempFiles.Add(SortBatch(batch, tempDir));
        }

        return tempFiles;
    }

    public void MergeFiles(List<string> tempFiles, string outputFile)
    {
        var readers = tempFiles
            .Select(f => new StreamReader(
                new BufferedStream(
                    new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize),
                    bufferSize
                )
            )).ToList();

        var queue = new PriorityQueue<MergeEntry, MergeEntry>(MergeItemComparer.Default);

        foreach (var reader in readers)
        {
            EnqueueNextItem(reader, queue);
        }

        using var writer = new StreamWriter(
            new BufferedStream(
                new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize),
                bufferSize
            )
        );

        while (queue.Count > 0)
        {
            var item = queue.Dequeue();
            writer.WriteLine(item.Line);

            if (item.Reader != null && !item.Reader.EndOfStream)
            {
                EnqueueNextItem(item.Reader, queue);
            }
            else
            {
                item.Reader?.Dispose();
            }
        }
    }

    string SortBatch(List<string> batch, string tempDir)
    {
        var entries = new BatchEntry[batch.Count];

        // Parallel parsing
        Parallel.For(0, batch.Count, i =>
        {
            var (number, firstWord) = LineParser.ParseLine(batch[i]);
            entries[i] = new BatchEntry(firstWord, number, batch[i]);
        });

        Array.Sort(entries, EntryComparer.Default);

        var tempFile = Path.Combine(tempDir, Guid.NewGuid() + ".tmp");
        using var writer = new StreamWriter(
            new BufferedStream(
                new FileStream(
                    tempFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize),bufferSize));
        {

            foreach (var entry in entries)
            {
                writer.WriteLine(entry.Line);
            }
        }

        return tempFile;
    }

    void EnqueueNextItem(StreamReader? reader, PriorityQueue<MergeEntry, MergeEntry> queue)
    {
        var line = reader?.ReadLine();
        if (line == null) return;
        var (number, firstWord) = LineParser.ParseLine(line);
        queue.Enqueue(
            new MergeEntry(firstWord, number, line, reader),
            new MergeEntry(firstWord, number, line, null)
        );
    }
}