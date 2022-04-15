using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace UniqueRegionNamesPatcher.Utility
{
    /// <summary>
    /// Simple class object that wraps a <see cref="IRegionGetter"/> formlink, in addition to its EditorID and displayname.
    /// </summary>
    public class RegionWrapper
    {
        public RegionWrapper(string editorID, FormLink<IRegionGetter> link, string? mapName = null)
        {
            EditorID = editorID;
            FormLink = link;
            Name = mapName;
        }
        public string EditorID { get; }
        public string? Name { get; }
        public FormLink<IRegionGetter> FormLink { get; }
    }
}
