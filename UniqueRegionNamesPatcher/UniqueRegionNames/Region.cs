using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace UniqueRegionNamesPatcher.UniqueRegionNames
{
    public static partial class UniqueRegionNames
    {
        internal static readonly ModKey ModKey = ModKey.FromNameAndExtension("Unique Region Names.esp");

        public static class Region
        {
            private static FormLink<IRegionGetter> Construct(uint id) => new(ModKey.MakeFormKey(id));
            public static FormLink<IRegionGetter> xxxMapReach => Construct(0x800);
            public static FormLink<IRegionGetter> xxxMapFalkreath => Construct(0x801);
            public static FormLink<IRegionGetter> xxxMapWinterholdHold => Construct(0x802);
            public static FormLink<IRegionGetter> xxxMapWinterhold => Construct(0x803);
            public static FormLink<IRegionGetter> xxxMapFalkreathHold => Construct(0x804);
            public static FormLink<IRegionGetter> xxxMapWhiterunHold => Construct(0x805);
            public static FormLink<IRegionGetter> xxxMapDawnstar => Construct(0x806);
            public static FormLink<IRegionGetter> xxxMapPale => Construct(0x807);
            public static FormLink<IRegionGetter> xxxMapHaafingar => Construct(0x808);
            public static FormLink<IRegionGetter> xxxMapRift => Construct(0x809);
            public static FormLink<IRegionGetter> xxxMapHjaalmarch => Construct(0x80a);
            public static FormLink<IRegionGetter> xxxMapMorthal => Construct(0x80b);
            public static FormLink<IRegionGetter> xxxMapEastmarch => Construct(0x80c);
            public static FormLink<IRegionGetter> xxxMapSea => Construct(0x80d);
            public static FormLink<IRegionGetter> xxxMapCollege => Construct(0x80e);
            public static FormLink<IRegionGetter> xxxMapThroat => Construct(0x80f);
            public static FormLink<IRegionGetter> xxxMapRiverwood => Construct(0x810);
            public static FormLink<IRegionGetter> xxxMapRorikstead => Construct(0x811);
            public static FormLink<IRegionGetter> xxxMapDragonbridge => Construct(0x812);
            public static FormLink<IRegionGetter> xxxMapKarthwasten => Construct(0x813);
            public static FormLink<IRegionGetter> xxxMapShorsstone => Construct(0x814);
            public static FormLink<IRegionGetter> xxxMapKynesgrove => Construct(0x815);
            public static FormLink<IRegionGetter> xxxMapIvarstead => Construct(0x816);
            public static FormLink<IRegionGetter> xxxMapChargenExit => Construct(0x817);
            public static FormLink<IRegionGetter> xxxMapHelgen => Construct(0x818);
            public static FormLink<IRegionGetter> xxxMapCastleVolkihar => Construct(0x819);
        }
    }
}
