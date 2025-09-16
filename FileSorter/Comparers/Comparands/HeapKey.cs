using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSorter.Comparers.Comparands;

public readonly record struct HeapKey(string Text, int Num);