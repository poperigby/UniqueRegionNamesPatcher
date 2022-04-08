using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniqueRegionNamesPatcher.util.Enum;

namespace UniqueRegionNamesPatcher.util
{
    internal class Matrix
    {
        public Matrix()
        {
            Dictionary = ParseLines(GetLines());
        }

        public Dictionary<CellCoordinate, HoldCapital[]> Dictionary { get; }


        public HoldCapital[]? Get(CellCoordinate cellCoordinate)
        {
            if (!Dictionary.ContainsKey(cellCoordinate))
                return null;
            return Dictionary[cellCoordinate];
        }
        public HoldCapital[]? Get(int subBlockX, int subBlockY) => Get(sub_to_cell(subBlockX), sub_to_cell(subBlockY));

        private int sub_to_cell(int sub) => Convert.ToInt32(Math.Floor(Convert.ToDecimal(sub * 8)));


        private static string[] GetLines()
        {
            List<string> lines = new();
            foreach (string line in Properties.Resources.cellmap.Split('\n'))
            {
                lines.Add(line);
            }
            return lines.ToArray();
        }

        private static (CellCoordinate, HoldCapital[])? ParseLine(string line)
        {
            int eq = line.IndexOf('=');
            if (eq == -1)
                return null;

            string coord = line[..eq].Trim();
            int coordSplit = coord.IndexOf(',');
            CellCoordinate coords = new(Convert.ToInt32(coord[..coordSplit]), Convert.ToInt32(coord[(coordSplit + 1)..]));

            string holdArray = line[(eq + 1)..].Trim(' ', '\r', '\n', '\t', '\v', '[', ']');
            List<HoldCapital> holds = new();
            foreach (var hold in holdArray.Split(','))
            {
                holds.Add(HoldCapitalFunctions.StringToHoldCapital(hold.Trim('\"', '\'')));
            }

            return (coords, holds.ToArray());
        }

        private static Dictionary<CellCoordinate, HoldCapital[]> ParseLines(string[] lines)
        {
            Dictionary<CellCoordinate, HoldCapital[]> dict = new();
            foreach (string line in lines)
            {
                var result = ParseLine(line);
                if (result != null)
                {
                    dict.Add(result.Value.Item1, result.Value.Item2);
                }
            }
            return dict;
        }
    }
}
