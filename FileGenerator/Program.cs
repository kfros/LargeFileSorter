if (args.Length != 2)
{
    Console.WriteLine("Usage: FileGenerator <outputFilePath> <sizeInGb>");
    return;
}

string outputFile = args[0];
if (!double.TryParse(args[1], out double sizeGB))
{
    Console.WriteLine("Invalid size. Please provide a valid number for GB.");
    return;
}

long targetSizeBytes = (long)(sizeGB * 1024 * 1024 * 1024);
Generator generator = new Generator(outputFile, targetSizeBytes);
generator.GenerateTestFile();