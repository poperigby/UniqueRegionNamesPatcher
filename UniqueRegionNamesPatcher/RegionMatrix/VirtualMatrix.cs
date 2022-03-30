using System.Collections.Generic;

namespace UniqueRegionNamesPatcher.RegionMatrix
{
    internal class VirtualMatrix<T>
    {
        public VirtualMatrix(int sizeX, int sizeY)
        {
            Size = (sizeX, sizeY);
            _list = new();
        }

        private int _sizeX, _sizeY;
        private readonly List<T> _list;

        protected int to1D(Point pos)
        {
            return _sizeX * pos.Y + pos.X;
        }
        protected Point from1D(int index)
        {
            return new(index / _sizeX, index % _sizeX);
        }

        public (int, int) Size
        {
            get => (_sizeX, _sizeY);
            set => (_sizeX, _sizeY) = value;
        }
        public int Length => _list.Count;

        public T Get(Point pos) => _list[to1D(pos)];
        public void Set(Point pos, T value) => _list[to1D(pos)] = value;
    }
}
