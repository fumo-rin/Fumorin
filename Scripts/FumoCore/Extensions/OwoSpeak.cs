using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RinCore
{
    public static class OwoSpeakExtensions
    {
        private static readonly string[] OwoList =
        new[] { "OwO", "owo", "UwU", "uwu", ">w<" };

        //new[] { "OwO", "owo", "UwU", "uwu", "^w^", ">w<", "(´•ω•`)" }; // this list has some weird symbols that are not so great for many fonts.

        [QFSW.QC.Command("-owo")]
        public static string OwoSpeak(this string msg, bool Extras = true)
        {
            if (string.IsNullOrEmpty(msg))
                return msg;

            var links = new System.Collections.Generic.List<string>();

            string ReplaceLink(Match m)
            {
                links.Add(m.Value);
                return $"owo{links.Count}";
            }

            // Preserve links
            string s = Regex.Replace(msg, @"\|c.*?\|r", ReplaceLink);
            s = Regex.Replace(s, @"\{.*?\}", ReplaceLink);

            // Skip Numbers
            s = Regex.Replace(s, @"([lr])([^\d\s]*)", m =>
            {
                string l = m.Groups[1].Value;
                string following = m.Groups[2].Value;

                if (l == "r" && following == "s")
                    return "rs";

                return "w" + following;
            });

            s = Regex.Replace(s, @"([LR])([^\d\s]*)", m =>
            {
                string L = m.Groups[1].Value;
                string following = m.Groups[2].Value;

                if (L == "R" && following == "S")
                    return "RS";

                return "W" + following;
            });

            // Preserve numbers and other characters
            s = Regex.Replace(s, @"U([^VW\d])", "UW$1");
            s = Regex.Replace(s, @"u([^vw\d])", "uw$1");

            s = s.Replace("ith ", "if ");

            s = Regex.Replace(s, @"([fps])([aeio]\w+)", "$1w$2");
            s = Regex.Replace(s, @"n([aeiou]\w)", "ny$1");
            s = s.Replace(" th", " d");

            if (Extras)
            {
                s = AddStutterRandom(s);
                s = AddOwoEndingRandom(s);
            }

            // Restore links
            s = Regex.Replace(s, @"owo(\d+)", m =>
            {
                int index = int.Parse(m.Groups[1].Value) - 1;
                return links[index];
            });

            return s;
        }

        private static string AddStutterRandom(string s)
        {
            return Regex.Replace(s, @"\b(\w+'?\w*)\b", m =>
            {
                string word = m.Groups[1].Value;

                if (word.All(char.IsDigit))
                    return word;

                if (RNG.Int255 <= 20 && word.Length > 0)
                {
                    return $"{word[0]}-{word}";
                }

                return word;
            });
        }

        private static string AddOwoEndingRandom(string s)
        {
            if (RNG.Int255 <= 25)
            {
                var owo = OwoList[RNG.Int255 % OwoList.Length];
                return s + " " + owo;
            }

            return Regex.Replace(s, @"!$", m =>
            {
                var owo = OwoList[RNG.Int255 % OwoList.Length];
                return "! " + owo;
            });
        }
    }
}
