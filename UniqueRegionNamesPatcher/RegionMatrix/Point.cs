namespace UniqueRegionNamesPatcher.RegionMatrix
{
    internal struct Point
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
        public Point((int, int) point)
        {
            X = point.Item1;
            Y = point.Item2;
        }

        public int X;
        public int Y;

        public (int, int) Pos
        {
            get => (X, Y);
            set => (X, Y) = value;
        }
    }
}
