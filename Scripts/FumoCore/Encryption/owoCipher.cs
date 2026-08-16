using System;
using System.Collections.Generic;
using System.Text;

namespace rinCore
{
    public static class OwOCipher
    {
        private static readonly string[] NibbleToOwo = new string[16]
        {
            "owo", "owO", "oWo", "oWO",
            "Owo", "OwO", "OWo", "OWO",
            "uwu", "uwU", "uWu", "uWU",
            "Uwu", "UwU", "UWu", "UWU"
        };

        private static readonly Dictionary<string, byte> OwoToNibble = new Dictionary<string, byte>(StringComparer.Ordinal);

        static OwOCipher()
        {
            for (byte i = 0; i < NibbleToOwo.Length; i++)
            {
                OwoToNibble[NibbleToOwo[i]] = i;
            }
        }

        /// <summary>
        /// Encodes raw UTF-8 string data directly into dash-separated OwO cipher tokens.
        /// </summary>
        public static string Encode(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            StringBuilder sb = new StringBuilder(bytes.Length * 8);

            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                int highNibble = (b >> 4) & 0x0F;
                int lowNibble = b & 0x0F;

                sb.Append(NibbleToOwo[highNibble]);
                sb.Append("-");
                sb.Append(NibbleToOwo[lowNibble]);

                if (i < bytes.Length - 1)
                {
                    sb.Append(" ");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Decodes dash-separated OwO cipher tokens back into the original UTF-8 string.
        /// </summary>
        public static bool TryDecode(string owoText, out string result)
        {
            result = string.Empty;
            if (string.IsNullOrWhiteSpace(owoText))
                return true;

            string[] tokens = owoText.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<byte> bytes = new List<byte>(tokens.Length);

            foreach (string token in tokens)
            {
                string[] parts = token.Split('-');
                if (parts.Length != 2)
                    return false;

                if (!OwoToNibble.TryGetValue(parts[0], out byte high) ||
                    !OwoToNibble.TryGetValue(parts[1], out byte low))
                {
                    return false;
                }

                byte b = (byte)((high << 4) | low);
                bytes.Add(b);
            }

            try
            {
                result = Encoding.UTF8.GetString(bytes.ToArray());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}