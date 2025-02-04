using FileSorter.Structs;

namespace FileSorter.Comparers;
class EntryComparer : IComparer<BatchEntry>
{
    public static readonly EntryComparer Default = new();

    public int Compare(BatchEntry x, BatchEntry y)
    {
        int cmp = string.CompareOrdinal(x.FirstWord, y.FirstWord);
        return cmp != 0 ? cmp : x.Number.CompareTo(y.Number);
    }
}