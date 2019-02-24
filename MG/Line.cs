using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MG
{
    struct Line
    {
        public readonly int Start;
        public readonly int End;

        public Line(int start, int end)
        {
            Start = start;
            End = end;
        }
    }
}
