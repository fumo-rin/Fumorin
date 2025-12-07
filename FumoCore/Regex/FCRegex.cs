using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FumoCore.Tools
{
    public static partial class FCRegex
    {
        public static bool SplitByChars(in string s, char[] charsToSplitOn, out List<string> result)
            => SplitByChars(s, charsToSplitOn.ToList(), out result);
        public static bool SplitByChars(in string s, List<char> charsToSplitOn, out List<string> result)
        {
            result = null;
            if (string.IsNullOrEmpty(s) || charsToSplitOn == null || charsToSplitOn.Count == 0)
            {
                return false;
            }
            IEnumerable<string> escapedChars = charsToSplitOn.Select(c => Regex.Escape(c.ToString()));
            string pattern = "[" + string.Concat(escapedChars) + "]";
            result = Regex.Split(s, pattern).ToList();
            return result.Count > 0;
        }
        public static string RemoveAfterUnderscore(string input, string endWord)
        {
            string safeEndWord = Regex.Escape(endWord);
            string pattern = "_[^_]*" + safeEndWord + "$";
            return Regex.Replace(input, pattern, "");
        }
    }
}