namespace FileSorter.Structs;
struct MergeEntry
{
    public string FirstWord { get; }
    public long Number { get; }
    public string Line { get; }
    public StreamReader? Reader { get; }

    public MergeEntry(string firstWord, long number, string line, StreamReader? reader)
    {
        FirstWord = firstWord;
        Number = number;
        Line = line;
        Reader = reader;
    }
}