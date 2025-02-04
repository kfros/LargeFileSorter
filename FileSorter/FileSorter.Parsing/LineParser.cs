namespace FileSorter.Parsing;
public static class LineParser
{
    public static (long Number, string FirstWord) ParseLine(string line)
    {
        int dotIndex = line.IndexOf(". ", StringComparison.Ordinal);
        if (dotIndex == -1) throw new FormatException($"Invalid line format: {line}");

        long number = ParseLong(line.Substring(0, dotIndex));
        string strPart = line.Substring(dotIndex + 2).TrimStart();

        int spaceIndex = strPart.IndexOf(' ');
        string firstWord = spaceIndex == -1 ? strPart : strPart.Substring(0, spaceIndex);

        return (number, firstWord);
    }

    static long ParseLong(ReadOnlySpan<char> numberSpan)
    {
        long result = 0;
        foreach (var c in numberSpan)
        {
            result = result * 10 + (c - '0');
        }
        return result;
    }
}