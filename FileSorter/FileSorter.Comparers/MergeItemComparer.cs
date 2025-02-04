using FileSorter.Structs;

namespace FileSorter.Comparers;
class MergeItemComparer : IComparer<MergeEntry>
{
    public static readonly MergeItemComparer Default = new();

    public int Compare(MergeEntry x, MergeEntry y)
    {
        int cmp = string.CompareOrdinal(x.FirstWord, y.FirstWord);
        return cmp != 0 ? cmp : x.Number.CompareTo(y.Number);
    }
}