using System;
using UniqueRegionNamesPatcher.util.Enum;

namespace UniqueRegionNamesPatcher.util
{
    internal static class HoldCapitalFunctions
    {
        public static HoldCapital StringToHoldCapital(string s)
        {
            if (!System.Enum.TryParse(typeof(HoldCapital), s, true, out object? o) || o == null)
                throw new Exception($"Matrix file is corrupted! ('{s}' is an invalid hold capital name.)");
            return (HoldCapital)o;
        }
    }
}
