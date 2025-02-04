namespace FileSorter.Structs;
struct BatchEntry
{
    public string FirstWord { get; }
    public long Number { get; }
    public string Line { get; }

    public BatchEntry(string firstWord, long number, string line)
    {
        FirstWord = firstWord;
        Number = number;
        Line = line;
    }
}