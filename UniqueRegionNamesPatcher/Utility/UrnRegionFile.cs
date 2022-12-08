using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace UniqueRegionNamesPatcher.Utility
{
    public class UrnRegionFile
    {
        #region Members
        public List<UrnRegionFileSection> Regions;
        #endregion Members

        #region Constructors
        public UrnRegionFile() => Regions = new();
        public UrnRegionFile(Stream stream) => Parse(stream, out Regions);
        #endregion Constructors

        #region Methods
        internal static bool Parse(Stream stream, out List<UrnRegionFileSection> regions)
        {
            regions = new();

            if (!stream.CanRead)
                return false;
            using StreamReader sr = new(stream);

            string? tmpEditorID = null;
            string? tmpMapName = null;
            Color? tmpColor = null;
            byte? tmpPriority = null;

            var insert = delegate (ref List<UrnRegionFileSection> regions)
            {
                if (tmpEditorID != null) regions.Add(new UrnRegionFileSection()
                {
                    EditorID = tmpEditorID,
                    MapName = tmpMapName ?? tmpEditorID.ParseRegionMapName(),
                    Color = tmpColor ?? Color.FromArgb(0, 0, 0),
                    Priority = tmpPriority ?? 60
                });

                tmpEditorID = null;
                tmpMapName = null;
                tmpColor = null;
                tmpPriority = null;
            };

            int ln = 0;
            while (!sr.EndOfStream)
            {
                ++ln;
                string? line = sr.ReadLine()?.TrimComments();

                if (line == null || line.Length == 0)
                    continue;

                int open = line.IndexOf('['),
                    close = line.IndexOf(']');

                if (open > -1 && close > open)
                {
                    insert(ref regions);
                    tmpEditorID = line[(open + 1)..close];
                    continue;
                }

                int eq = line.IndexOf('=');

                if (eq != -1)
                {
                    try
                    {
                        string key = line[..eq].Trim(), value = line[(eq + 1)..].Trim(new[] { '\'', '\"', ' ', '\n', '\r', '\t' });

                        if (key.Equals("color", StringComparison.OrdinalIgnoreCase))
                        {
                            if (value.Length == 6)
                            {
                                tmpColor = Color.FromArgb(Convert.ToInt32(value[0..2], 16), Convert.ToInt32(value[2..4], 16), Convert.ToInt32(value[4..6], 16));
                            }
                            else Console.WriteLine($"[WARN]\tInvalid 3-Channel Hexadecimal RGB Value: '{value}'");
                        }
                        else if (key.Equals("mapName", StringComparison.OrdinalIgnoreCase))
                        {
                            tmpMapName = value;
                        }
                        else if (key.Equals("priority", StringComparison.OrdinalIgnoreCase))
                        {
                            tmpPriority = Convert.ToByte(value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR]\tException '{ex.Message}' at line {ln}");
                    }
                }
            }
            // include the final entry before EOF
            insert(ref regions);

            return true;
        }
        #endregion Methods
    }
}
