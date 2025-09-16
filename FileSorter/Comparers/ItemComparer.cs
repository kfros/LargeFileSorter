using FileSorter.Comparers.Comparands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSorter.Comparers;

public class ItemComparer : IComparer<Item>
{
    public static readonly ItemComparer Instance = new();
    private static readonly StringComparer TextCmp = StringComparer.OrdinalIgnoreCase;
    public int Compare(Item x, Item y)
    {
        int c = TextCmp.Compare(x.Text, y.Text);
        if (c != 0) return c;
        return x.Num.CompareTo(y.Num);
    }
}