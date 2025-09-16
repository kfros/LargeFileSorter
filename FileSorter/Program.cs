using FileSorter;
using FileSorter.Core;

if (args.Length < 1)
{
    Console.WriteLine("Usage: sorter <input-file> [output-file]");
    return;
}

Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

string inputPath = args[0];
string outputPath = args.Length > 1 ? args[1] : args[0] + ".sorted.txt";


using var progress = new ProgressReporter(); // background heartbeat

try
{
    Console.WriteLine($"Sorting '{inputPath}' into '{outputPath}'...");
    FileSortingProcessor.SortParallel(
                inputPath: inputPath,
                outputPath: outputPath,
                memoryBudgetBytes: 4L * 1024 * 1024 * 1024,   // 4 GB: faster first run
                maxChunkWorkers: Math.Max(1, Environment.ProcessorCount - 2),
                maxMergeFanIn: 64,
                maxParallelMerges: 1,
                progress: progress
    );
    Console.WriteLine("Done!");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}