using FileCreator.Core;

if (args.Length < 2)
{
    Console.WriteLine("Usage: FileCreator <outputPath> <targetBytes>");
    return;
}

string path = args[0];
if (!long.TryParse(args[1], out long targetBytes) || targetBytes <= 0)
{
    Console.WriteLine("targetBytes must be a positive integer.");
    return;
}

FileGenerator.GenerateTestFile(path, targetBytes);