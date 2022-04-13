using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
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
        internal static string ParseRegionMapName(this string s)
        {
            if (!s.StartsWith("xxxMap"))
                return s;
            return System.Text.RegularExpressions.Regex.Replace(s[6..], "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }
    }

    internal class RegionMap
    {
        public RegionMap(byte[] fileContent, ref IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Map = new();
            Regions = new();
            Parse(new MemoryStream(fileContent), ref state);
        }
        public RegionMap(string path, ref IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Map = new();
            Regions = new();
            Parse(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read), ref state);
        }

        private void Parse(Stream stream, ref IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
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

                    List<FormLink<IRegionGetter>> links = new();

                    foreach (string name in regionNames)
                    {
                        var existing = Regions.FirstOrDefault(r => name.Equals(r.EditorID, StringComparison.Ordinal));
                        if (existing == null)
                        {
                            Region newRegion = state.PatchMod.Regions.AddNew(name);
                            newRegion.Map = new()
                            {
                                Name = name.ParseRegionMapName()
                            };
                            Regions.Add(newRegion);
                            links.Add(new FormLink<IRegionGetter>(newRegion));
                            Console.WriteLine($"Created new region '{newRegion.EditorID}' with name '{newRegion.Map.Name}'");
                        }
                        else
                        {
                            links.Add(new FormLink<IRegionGetter>(existing));
                        }
                    }

                    ///var regions = Regions.Where(r => regionNames.Any(n => n.Equals(r.EditorID, StringComparison.Ordinal))).Cast<IRegionGetter>().ToList();

                    Map.Add(coord.Value, links);
                    //Console.WriteLine($"Added ({coord.Value.X}, {coord.Value.Y}) = {value}");
                }
            }
        }

        public List<FormLink<IRegionGetter>> GetFormLinksForPos(Point coord)
        {
            List<FormLink<IRegionGetter>> links = new();

            if (Map.ContainsKey(coord))
            {
                var arr = Map[coord];
                if (arr != null)
                {
                    foreach (var link in arr)
                    {
                        links.Add(link);
                    }
                }
            }

            return links;
        }

        public List<Region> Regions { get; private set; }
        public Dictionary<Point, List<FormLink<IRegionGetter>>> Map { get; private set; }
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

            RegionMap coordMap = new(Properties.Resources.cellmap, ref state);

            long changeCount = 0;

            var tamrielCells = state.LoadOrder.ListedOrder
                .Where(m => m.Mod != null)
                .SelectMany(mod => mod.Mod!.EnumerateMajorRecords<IWorldspaceGetter>())
                .Where(w => w.EditorID != null && w.EditorID.Equals("Tamriel", StringComparison.Ordinal))
                .SelectMany(t => t.SubCells)
                .SelectMany(b => b.Items)
                .SelectMany(s => s.Items);

            foreach (var world in state.LoadOrder.ListedOrder.Worldspace().WinningOverrides())
            {
                if (world.EditorID == null || !world.EditorID.Equals("Tamriel", StringComparison.Ordinal))
                    continue;

                int worldChanges = 0;

                Worldspace? worldCopy = null;

                int blockIndex = 0;
                foreach (var block in world.SubCells)
                {
                    int blockChanges = 0;
                    WorldspaceBlock? blockCopy = null;

                    int subblockIndex = 0;
                    foreach (var subblock in block.Items)
                    {
                        int subblockChanges = 0;
                        WorldspaceSubBlock? subblockCopy = null;

                        int cellIndex = 0;
                        foreach (var cell in subblock.Items)
                        {
                            var cellCopy = cell.DeepCopy();

                            bool cellChanged = false;
                            if (cell.Grid != null)
                            {
                                Point coord = new(cell.Grid.Point.X, cell.Grid.Point.Y);

                                var regions = coordMap.GetFormLinksForPos(coord);

                                if (regions.Count > 0)
                                {
                                    Console.WriteLine($"Found {regions.Count} regions for cell location ({coord.X}, {coord.Y})");

                                    if (cellCopy.Regions == null)
                                        cellCopy.Regions = new();

                                    cellCopy.Regions.AddRange(regions);
                                    cellChanged = true;
                                }
                                else Console.WriteLine($"No regions found for cell location ({coord.X}, {coord.Y})");
                            }
                            if (cellChanged)
                            {
                                if (subblockCopy == null)
                                    subblockCopy = subblock.DeepCopy();
                                subblockCopy!.Items[cellIndex] = cellCopy;
                                ++subblockChanges;
                            }
                            ++cellIndex;
                        }
                        if (subblockChanges > 0)
                        {
                            if (blockCopy == null)
                                blockCopy = block.DeepCopy();
                            blockCopy!.Items[subblockIndex] = subblockCopy!;
                            ++blockChanges;
                        }
                        ++subblockIndex;
                    } //< SUBBLOCK

                    if (blockChanges > 0)
                    {
                        if (worldCopy == null)
                            worldCopy = world.DeepCopy();
                        worldCopy.SubCells[blockIndex] = blockCopy!;
                        ++worldChanges;
                    }
                    ++blockIndex;
                } //< BLOCK

                if (worldChanges > 0)
                {
                    state.PatchMod.Worldspaces.Set(worldCopy!);
                    ++changeCount;
                }
            } //< WORLDSPACE


            Console.WriteLine("=== Diagnostics ===");

            if (changeCount > 0)
                Console.WriteLine($"Patched {changeCount} records.");
            else Console.WriteLine("No changes were made, something probably went wrong!");

            Console.WriteLine("=== Patcher Complete ===");
        }
    }
}
