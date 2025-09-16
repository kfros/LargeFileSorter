using FileSorter.Comparers.Comparands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSorter.Comparers
{
    public class HeapKeyComparer : IComparer<HeapKey>
    {
        public static readonly HeapKeyComparer Instance = new();
        private static readonly StringComparer TextCmp = StringComparer.OrdinalIgnoreCase;
        public int Compare(HeapKey x, HeapKey y)
        {
            int c = TextCmp.Compare(x.Text, y.Text);
            if (c != 0) return c;
            return x.Num.CompareTo(y.Num);
        }
    }
}