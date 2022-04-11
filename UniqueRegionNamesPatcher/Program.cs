using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UniqueRegionNamesPatcher
{
    internal static class FileParserHelpers
    {
        internal static string RemoveAll(this string s, params char[] ch)
        {
            string rs = string.Empty;
            for (int i = 0; i < s.Length; ++i)
            {
                char c = s[i];
                if (!ch.Contains(c))
                    rs += c;
            }
            return rs;
        }
        internal static string RemoveIf(this string s, Func<char, bool> pred)
        {
            string rs = string.Empty;
            for (int i = 0; i < s.Length; ++i)
            {
                char c = s[i];
                if (!pred(c))
                    rs += c;
            }
            return rs;
        }
        internal static string TrimComments(this string s, char[] comment_chars)
        {
            int i = s.IndexOfAny(comment_chars);
            if (i != -1)
                return s[..i];
            return s;
        }
        internal static string TrimComments(this string s) => TrimComments(s, new[] { ';', '#' });
        internal static string[] ParseArray(this string s)
        {
            List<string> elements = new();
            foreach (string elem in s.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string e = elem.Trim('"');
                if (e.Length > 0)
                    elements.Add(e);
            }
            return elements.ToArray();
        }
        internal static Point? ParsePoint(this string s)
        {
            var split = s.Trim('(', ')').Split(',');
            if (split.Length == 2)
            {
                return new(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]));
            }
            return null;
        }
    }

    internal class RegionMap
    {
        public RegionMap(byte[] fileContent)
        {
            Map = new();
            Parse(new MemoryStream(fileContent));
        }
        public RegionMap(string path)
        {
            Map = new();
            Parse(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        private void Parse(Stream stream)
        {
            using StreamReader sr = new(stream);

            string header = string.Empty;
            bool doRead = false;

            int ln = 0;
            for (string? line = sr.ReadLine(); !sr.EndOfStream; line = sr.ReadLine(), ++ln)
            {
                if (line == null)
                    continue;

                // strip all comments & whitespace
                line = line.TrimComments().RemoveIf(char.IsWhiteSpace);

                if (line.Length == 0)
                    continue;

                // check for an INI header:
                int eq = line.IndexOf('='), open = line.IndexOf('['), close = line.IndexOf(']');

                if (eq == -1 && open != -1 && close != -1)
                {
                    header = line[(open + 1)..close];
                    doRead = header.Equals("HoldMap", StringComparison.Ordinal);
                }
                else if (doRead)
                {
                    if (eq == -1)
                        continue;

                    // parse the key (coordinate)
                    var coord = line[..eq].RemoveAll('(', ')', ' ').ParsePoint();
                    if (coord == null)
                    {
                        Console.WriteLine($"[WARNING]\tLine {ln} contains an invalid coordinate string! ('{line}')");
                        continue;
                    }

                    // parse the value (region name list)
                    string value = line[(eq + 1)..].Trim();
                    var regionNames = value.ParseArray();

                    Map.Add(coord.Value, regionNames);
                    Console.WriteLine($"Added ({coord.Value.X}, {coord.Value.Y}) = {value}");
                }
            }
        }

        public void CreateRegions(ref IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var (coord, objArray) in Map)
            {
                List<object> list = new();
                for (int i = 0, end = objArray.Length; i < end; ++i)
                {
                    if (objArray[i] is string editorID)
                    {
                        var added = state.PatchMod.Regions.AddNew(editorID);
                        if (added != null)
                        {
                            list.Add(new FormLink<IRegionGetter>(added.FormKey));
                            Console.WriteLine($"Linked region {added.EditorID}.");
                        }
                    }
                }
                Map[coord] = list.ToArray();
            }
        }

        public FormLink<IRegionGetter>[] GetFormLinksForPos(Point coord)
        {
            List<FormLink<IRegionGetter>> links = new();

            if (Map.ContainsKey(coord))
            {
                var arr = Map[coord];
                if (arr != null)
                {
                    foreach (object obj in arr)
                    {
                        if (obj is FormLink<IRegionGetter> link)
                            links.Add(link);
                    }
                }
            }

            return links.ToArray();
        }

        public Dictionary<Point, object[]> Map { get; private set; }
    }



    public class Program
    {
        private static Lazy<Settings> _lazySettings = null!;
        private static Settings Settings => _lazySettings.Value;

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
                .SetAutogeneratedSettings("Settings", "settings.json", out _lazySettings, false)
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Console.WriteLine("=== Patcher Begin ===");

            state.PatchMod.Regions.AddNew("");

            RegionMap coordMap = new(Properties.Resources.cellmap);
            coordMap.CreateRegions(ref state);

            long changeCount = 0;

            foreach (var WRLD in state.LoadOrder.PriorityOrder.Worldspace().WinningOverrides())
            {
                var WRLDcopy = WRLD.DeepCopy();
                long changeCopy = changeCount;

                int blockIndex = 0;
                foreach (var block in WRLD.SubCells)
                {
                    var blockCopy = block.DeepCopy();
                    int blockChanges = 0;

                    int subBlockIndex = 0;
                    foreach (var subBlock in block.Items)
                    {
                        var subBlockCopy = subBlock.DeepCopy();
                        int subBlockChanges = 0;

                        int cellIndex = 0;
                        foreach (var cell in subBlock.Items)
                        {
                            var pos = cell.Grid?.Point;
                            if (pos != null)
                            {
                                // convert subblock coordinates to cell coordinates
                                Point coord = new(pos.Value.X, pos.Value.Y);

                                var regions = coordMap.GetFormLinksForPos(coord);

                                if (regions.Length > 0)
                                {
                                    var cellCopy = cell.DeepCopy();

                                    Console.WriteLine($"Found {regions.Length} regions for cell location ({coord.X}, {coord.Y})");

                                    if (cellCopy.Regions == null)
                                        cellCopy.Regions = new();

                                    cellCopy.Regions.AddRange(regions);
                                    subBlockCopy.Items[cellIndex] = cellCopy;
                                    ++subBlockChanges;
                                }
                                else Console.WriteLine($"No regions found for cell location ({coord.X}, {coord.Y})");
                            }
                            ++cellIndex;
                        }

                        if (subBlockChanges > 0)
                        {
                            blockCopy.Items[subBlockIndex] = (WorldspaceSubBlock)subBlockCopy;
                            ++blockChanges;
                        }
                        ++subBlockIndex;
                    }

                    if (blockChanges > 0)
                    {
                        WRLDcopy.SubCells[blockIndex] = blockCopy;
                        ++changeCount;
                    }
                    ++blockIndex;
                }

                if (changeCount > changeCopy)
                    state.PatchMod.Worldspaces.Set(WRLDcopy);
            }


            Console.WriteLine("=== Diagnostics ===");

            if (changeCount > 0)
                Console.WriteLine($"Patched {changeCount} records.");
            else Console.WriteLine("No changes were made, something probably went wrong!");

            Console.WriteLine("=== Patcher Complete ===");
        }
    }
}
