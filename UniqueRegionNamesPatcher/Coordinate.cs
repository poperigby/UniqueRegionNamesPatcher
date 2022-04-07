using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniqueRegionNamesPatcher
{
    enum CoordinateType : byte
    {
        Raw,
        Cell,
        SubBlock,
        Block,
    }
    internal class Coordinate
    {
        Coordinate()
        {

        }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
