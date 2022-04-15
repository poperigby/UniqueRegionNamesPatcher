using Mutagen.Bethesda;
using Noggog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace UniqueRegionNamesPatcher.Utility
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
        internal static string TrimComments(this string s) => s.TrimComments(new[] { ';', '#' });
        internal static Point? ParsePoint(this string s)
        {
            var split = s.Trim('(', ')').Split(',');
            if (split.Length == 2)
                return new(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]));
            return null;
        }
        internal static string ParseRegionMapName(this string s)
        {
            if (!s.StartsWith("xxxMap"))
                return s;
            return System.Text.RegularExpressions.Regex.Replace(s[6..], "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }
    }
}
