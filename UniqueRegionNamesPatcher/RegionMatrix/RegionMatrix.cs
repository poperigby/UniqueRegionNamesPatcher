using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace UniqueRegionNamesPatcher.RegionMatrix
{
    internal class RegionMatrix : VirtualMatrix<FormLink<IRegionGetter>>
    {
        public RegionMatrix() : base(30, 30)
        {
        }


    }
}
