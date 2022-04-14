using Mutagen.Bethesda.Skyrim;

namespace UniqueRegionNamesPatcher.Utility
{
    /// <summary>
    /// Temporary brute-force workaround for directly assigning map data to a <see cref="Region"/>.<br/>
    /// <i><b>TODO:</b> Replace all usages of this object with <b><see cref="Mutagen.Bethesda.Skyrim.RegionMap"/></b>!</i>
    /// </summary>
    internal class RegionMapDataHeader : RegionDataHeader
    {
        public new RegionData.RegionDataType DataType
        {
            get => base.DataType;
            set => base.DataType = value;
        }
        public new RegionData.RegionDataFlag Flags
        {
            get => base.Flags;
            set => base.Flags = value;
        }
    }
}
