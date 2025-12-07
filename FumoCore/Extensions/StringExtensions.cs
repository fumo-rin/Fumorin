using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RinCore
{
    public static class GameobjectNameCleaner
    {
        private static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            // Remove Unity duplicate suffixes like ".001", ".002", etc.
            name = Regex.Replace(name, @"\.\d{3}$", "");
            // Remove "(Clone)" and optional whitespace
            name = Regex.Replace(name, @"\s*\(Clone\)$", "");
            name = name.Trim();
            return name;
        }
        public static string SetCleanName(this GameObject go)
        {
            if (go == null) return null;
            go.name = CleanName(go.name);
            return go.name;
        }
    }
    public static class StringExtensions
    {
        public static string Capitalized(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            return s[0].ToString().ToUpper() + s.Substring(1);
        }
        public static string ConditionalString(this bool b, string trueString, string falseString)
        {
            return b switch
            {
                true => trueString,
                false => falseString
            };
        }
        private static string Color(this string str, string clr) => string.Format("<color={0}>{1}</color>", clr, str);
        public static string Color(this string str, Color32 color)
        {
            string stringColor = ColorUtility.ToHtmlStringRGBA(color).ToString();
            str = str.Color("#" + stringColor);
            return str;
        }
        public static string Color(this string str, byte r, byte g, byte b, byte alpha = 255)
        {
            Color32 color = new(r, g, b, alpha);
            return str.Color(color);
        }
        public static string ReplaceLineBreaks(this string s, string lineBreakSequence)
        {
            s = s.Replace(lineBreakSequence, "\n");
            return s;
        }
        public static IEnumerable<char> StringChop(this string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                yield return s[i];
            }
        }
        public static string RemoveAfter(this string input, string cutoff, bool keepCutoff = false)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(cutoff))
                return string.Empty;

            int index = input.IndexOf(cutoff, StringComparison.Ordinal);
            if (index < 0)
                return input;

            return keepCutoff
                ? input.Substring(0, index + cutoff.Length)
                : input.Substring(0, index);
        }
        public static bool RegexChar(this char input, HashSet<char> regex)
        {
            return regex.Contains(input);
        }
        public static string RemoveChar(this string source, char charToRemove)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            return source.Replace(charToRemove.ToString(), "");
        }
        public static string Truncate(this string source, int maxLength)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            if (source.Length <= maxLength)
                return source;

            return source.Substring(0, maxLength);
        }
        public static string Numberize(this string str, string separator = " ")
        {
            string InsertThousandsSeparator(string numberStr, string separator)
            {
                return Regex.Replace(numberStr, @"\B(?=(\d{3})+(?!\d))", separator);
            }
            if (string.IsNullOrEmpty(str))
                return str;
            if (long.TryParse(str, out var number))
            {
                return InsertThousandsSeparator(number.ToString(), separator);
            }
            if (double.TryParse(str, out var doubleNumber))
            {
                var parts = doubleNumber.ToString("F0", CultureInfo.InvariantCulture).Split('.');
                return InsertThousandsSeparator(parts[0], separator);
            }
            return str;
        }
        public static string SpaceByCapitals(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;
            var result = new System.Text.StringBuilder();
            result.Append(input[0]);
            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]) && !char.IsWhiteSpace(input[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(input[i]);
            }
            return result.ToString();
        }
        public static string SafeString(this string input, bool preserveCapitals = true, bool removeSpaces = true, bool preserveUnderscore = true, bool preserveNumbers = true)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sb = new StringBuilder();
            bool started = false;

            foreach (char c in input)
            {
                if (char.IsLetter(c))
                {
                    sb.Append(preserveCapitals ? c : char.ToLowerInvariant(c));
                    started = true;
                }
                else if (char.IsDigit(c))
                {
                    if (preserveNumbers)
                    {
                        sb.Append(c);
                        started = true;
                    }
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (started && !removeSpaces)
                        sb.Append(' ');
                }
                else if (c == '_' && preserveUnderscore)
                {
                    sb.Append('_');
                    started = true;
                }
            }

            return sb.ToString();
        }
        public static string SafeRemoveWords(this string input, params string[] wordsToRemove)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
            var originalWords = input.Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
            var removeSet = wordsToRemove
                .Select(w => w.SafeString(preserveUnderscore: true, removeSpaces: true))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var resultWords = originalWords
                .Where(w => !removeSet.Contains(w.SafeString(preserveUnderscore: true, removeSpaces: true)))
                .ToArray();
            return string.Join(" ", resultWords);
        }
        public static string Humanize(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            StringBuilder result = new StringBuilder(input.Length);
            bool capitalizeNext = true;

            for (int i = 0; i < input.Length; i++)
            {
                char current = input[i];
                if (char.IsLetter(current))
                    current = char.ToLower(current);

                if (capitalizeNext && char.IsLetter(current))
                {
                    result.Append(char.ToUpper(current));
                    capitalizeNext = false;
                }
                else
                {
                    result.Append(current);
                }

                if (current == '.' || current == '!' || current == '?')
                {
                    capitalizeNext = true;
                }
            }
            if (!result.ToString().EndsWith('.') &&
                !result.ToString().EndsWith('!') &&
                !result.ToString().EndsWith('?'))
            {
                result.Append('.');
            }

            return result.ToString();
        }
        public static string ClampLength(this string input, int maxLength, string suffix = "")
        {
            if (string.IsNullOrEmpty(input) || maxLength <= 0)
                return string.Empty;

            if (input.Length <= maxLength)
                return input;

            int contentLength = Math.Max(0, maxLength - suffix.Length);
            return input.Substring(0, contentLength) + suffix;
        }
        /// <summary>
        /// Replaces digit characters in the input string with letters according to a mapping.
        /// By default uses a small builtin mapping (includes 6 -> "b", 1 -> "i").
        /// Provide your own mapping to change behavior.
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="digitMap">Map from digit char ('0'..'9') to replacement string. If null, a default map is used.</param>
        /// <param name="preserveUnmappedDigits">If true leave digits that aren't in the map unchanged; if false, remove them.</param>
        /// <returns>Transformed string.</returns>
        public static string Letterize(this string input, IDictionary<char, string> digitMap = null, bool preserveUnmappedDigits = true)
        {
            if (input == null) return null;
            var defaultMap = new Dictionary<char, string>
            {
                ['0'] = "o",
                ['1'] = "i",
                ['2'] = "z",
                ['3'] = "e",
                ['4'] = "a",
                ['5'] = "s",
                ['6'] = "b",
                ['7'] = "t",
                ['8'] = "b",
                ['9'] = "q"
            };
            var map = digitMap ?? defaultMap;
            var sb = new StringBuilder(input.Length);
            foreach (char ch in input)
            {
                if (ch >= '0' && ch <= '9')
                {
                    if (map != null && map.TryGetValue(ch, out var repl))
                    {
                        sb.Append(repl);
                    }
                    else
                    {
                        if (preserveUnmappedDigits)
                            sb.Append(ch);
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
        public static string ReplaceWordsSpaced(this string source, string word, string replacement)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(word))
                return source;
            string result = source.Replace(word, $" {replacement} ");
            while (result.Contains("  "))
                result = result.Replace("  ", " ");
            return result.Trim();
        }
    }
}
