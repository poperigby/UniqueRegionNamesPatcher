using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace UniqueRegionNamesPatcher
{
    public class RegionMapSettings
    {
        public RegionMapSettings(byte[] defaultMap, FormLink<IWorldspaceGetter> worldspace, bool useCustom = false, string customPath = "")
        {
            DefaultMap = defaultMap;
            Worldspace = worldspace;
            UseCustomRegionMap = useCustom;
            CustomRegionMapPath = customPath;
        }

        internal byte[] DefaultMap;

        [SettingName("Use Custom Region Map")]
        public bool UseCustomRegionMap;

        [Tooltip("Optional filepath of an alternative region map file to use instead of the built-in one. This is only used if 'Use Custom Region Map' is enabled.")]
        public string CustomRegionMapPath;

        [SettingName("Worldspace Whitelist"), Tooltip("Only the following worldspaces are processed.")]
        public FormLink<IWorldspaceGetter> Worldspace;

        public Utility.RegionMap GetRegionMap(ref IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Stream? regionMapStream = null;

            if (UseCustomRegionMap)
            {
                Console.WriteLine($"Using custom region map file located at: '{CustomRegionMapPath}'!");
                string path = CustomRegionMapPath;
                if (File.Exists(path))
                    regionMapStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                else throw new FileNotFoundException("'Custom Region Map Path' specifies a file that doesn't exist!", CustomRegionMapPath);
            }
            else
            {
                Console.WriteLine("Using integrated region map file.");
                regionMapStream = new MemoryStream(Properties.Resources.cellmap);
            }

            if (regionMapStream == null)
            {
                throw new Exception("Failed to create a valid stream for parsing the region map!");
            }

            return new(regionMapStream, ref state);
        }
    }
    public class Settings
    {
        public Settings()
        {
        }

        [SettingName("Verbose Log")]
        public bool verbose = true;

        [Tooltip("Don't modify this unless you know what you're doing!")]
        public List<RegionMapSettings> Worldspaces = new()
        {
            new(Properties.Resources.cellmap, Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Worldspace.Tamriel),
        };
    }
}
