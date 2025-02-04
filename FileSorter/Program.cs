using FileSorter.Core;
using System.Diagnostics;

if (args.Length < 2)
{
    Console.WriteLine("Usage: FileSorter <input file> <output file>");
    return;
}

string inputFile = args[0];
string outputFile = args[1];
string tempDir = Path.Combine(Path.GetTempPath(), "FileSorterTemp");
Directory.CreateDirectory(tempDir);

Stopwatch sw = new Stopwatch();
sw.Start();
try
{
    FileProcessor fp = new FileProcessor();
    Console.WriteLine($"Sorting started at {DateTime.Now:HH:mm:ss tt}");
    var tempFiles = fp.SplitFile(inputFile, tempDir);
    fp.MergeFiles(tempFiles, outputFile);
    sw.Stop();
}
finally
{
    Directory.Delete(tempDir, true);
    Console.WriteLine($"Sorting complete at {DateTime.Now:HH:mm:ss tt}");
    Console.WriteLine($"                 Time elapsed: {sw.Elapsed.TotalSeconds}");
}
