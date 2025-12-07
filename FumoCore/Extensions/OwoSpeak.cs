using System.Text.RegularExpressions;
using UnityEngine;

namespace FumoCore.Tools
{
    public static class OwoSpeakExtensions
    {
        private static readonly string[] OwoList = new[]
        {
        "OwO", "owo", "UwU", "uwu", "^w^", ">w<", "(´•ω•`)"
        };
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

            string s = Regex.Replace(msg, @"\|c.*?\|r", ReplaceLink);
            s = Regex.Replace(s, @"\{.*?\}", ReplaceLink);

            s = Regex.Replace(s, @"([lr])(\S*s?)", m =>
            {
                string l = m.Groups[1].Value;
                string following = m.Groups[2].Value;

                if (l == "r" && following == "s")
                    return "rs";

                return "w" + following;
            });

            s = Regex.Replace(s, @"([LR])(\S*S?)", m =>
            {
                string L = m.Groups[1].Value;
                string following = m.Groups[2].Value;

                if (L == "R" && following == "S")
                    return "RS";

                return "W" + following;
            });
            s = Regex.Replace(s, @"U([^VW])", "UW$1");
            s = Regex.Replace(s, @"u([^vw])", "uw$1");

            s = s.Replace("ith ", "if ");

            s = Regex.Replace(s, @"([fps])([aeio]\w+)", "$1w$2");
            s = Regex.Replace(s, @"n([aeiou]\w)", "ny$1");
            s = s.Replace(" th", " d");

            if (Extras)
            {
                s = AddStutterRandom(s);
            }
            if (Extras)
            {
                s = AddOwoEndingRandom(s);
            }
            s = Regex.Replace(s, @"owo(\d+)", m =>
            {
                int index = int.Parse(m.Groups[1].Value) - 1;
                return links[index];
            });

            return s;
        }

        private static string AddStutterRandom(string s)
        {
            var rand = new System.Random();
            return Regex.Replace(s, @"\b(\w+)\b", m =>
            {
                if (rand.Next(12) == 0)
                {
                    string w = m.Groups[1].Value;
                    return $"{w[0]}-{w}";
                }
                return m.Value;
            });
        }

        private static string AddOwoEndingRandom(string s)
        {
            var rand = new System.Random();
            if (rand.Next(10) == 0)
            {
                var owo = OwoList[rand.Next(OwoList.Length)];
                return s + " " + owo;
            }

            return Regex.Replace(s, @"!$", m =>
            {
                var owo = OwoList[rand.Next(OwoList.Length)];
                return "! " + owo;
            });
        }
    }
}
