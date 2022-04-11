using System;

namespace UniqueRegionNamesPatcher.util
{
    internal class CellCoordinate
    {
        public CellCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }
        public CellCoordinate((int, int) point)
        {
            X = point.Item1;
            Y = point.Item2;
        }

        public static CellCoordinate FromSubBlock(short x, short y)
        {
            return new CellCoordinate(x * 8, y * 8);
        }

        /// <summary>
        /// Cell Coordinate X-Axis.
        /// </summary>
        public int X { get; set; }
        /// <summary>
        /// Cell Coordinate Y-Axis.
        /// </summary>
        public int Y { get; set; }
        /// <summary>
        /// Cell Coordinates.
        /// </summary>
        public (int, int) Pos => (X, Y);

        /// <summary>
        /// Raw Coordinate X-Axis
        /// </summary>
        public int RawX => Convert.ToInt32(Math.Floor(Convert.ToDecimal(X * 4096)));
        /// <summary>
        /// Raw Coordinate Y-Axis
        /// </summary>
        public int RawY => Convert.ToInt32(Math.Floor(Convert.ToDecimal(Y * 4096)));
        /// <summary>
        /// Raw Coordinates.
        /// </summary>
        public (int, int) Raw => (RawX, RawY);

        /// <summary>
        /// SubBlock Coordinate X-Axis
        /// </summary>
        public int SubBlockX => Convert.ToInt32(Math.Floor(Convert.ToDecimal(X / 8)));
        /// <summary>
        /// SubBlock Coordinate Y-Axis
        /// </summary>
        public int SubBlockY => Convert.ToInt32(Math.Floor(Convert.ToDecimal(Y / 8)));
        /// <summary>
        /// SubBlock Coordinates.
        /// </summary>
        public (int, int) SubBlock => (SubBlockX, SubBlockY);

        /// <summary>
        /// Block Coordinate X-Axis
        /// </summary>
        public int BlockX => Convert.ToInt32(Math.Floor(Convert.ToDecimal(X / 32)));
        /// <summary>
        /// Block Coordinate Y-Axis
        /// </summary>
        public int BlockY => Convert.ToInt32(Math.Floor(Convert.ToDecimal(Y / 32)));
        /// <summary>
        /// Block Coordinates.
        /// </summary>
        public (int, int) Block => (BlockX, BlockY);
    }
}
