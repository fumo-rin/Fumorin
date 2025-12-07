using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FumoCore.Tools
{
    public static class IntExtensions
    {
        public static int Clamp(this int i, int min, int max)
        {
            return Mathf.Clamp(i, min, max);
        }
        public static int Min(this int i, int min)
        {
            return Mathf.Min(i, min);
        }
        public static int Max(this int i, int max)
        {
            return Mathf.Max(i, max);
        }
        public static int Abs(this int i)
        {
            return Mathf.Abs(i);
        }
        public static int Scramble(this int i, int seed, int upperLimit, int scrambler = 3378)
        {
            int hash = scrambler.ToString().GetHashCode();
            int seedHash = seed.ToString().GetHashCode();

            int factor = (seedHash * hash);
            return ((i * factor).Abs() % upperLimit);
        }
        public static bool IsWithin(this int i, int min, int max)
        {
            return i >= min && i <= max;
        }
        public static int RandomBetween(this int i, int min, int max)
        {
            i = Random.Range(min, max);
            return i;
        }
        public static int Spread(this int i, float percentage = 5f)
        {
            return (int)((float)i * (Random.Range(1 - percentage.Clamp(0f, 100f) * FloatExtensions.Percent, 1 + percentage.Clamp(0f, 100f) * FloatExtensions.Percent)));
        }
        public static int MultiplyAndFloor(this int i, float multiplier)
        {
            return (int)((float)i).Multiply(multiplier).Floor();
        }
        public static float MultiplyAndFloorAsFloat(this int i, float multiplier)
        {
            return i.AsFloat().Multiply(multiplier);
        }
        public static float AsFloat(this int i, float multiplier = 1f) => (float)i * multiplier;
        public static string ToThousandsString(this int value, int decimals = 0, CultureInfo culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;
            string format = "N" + decimals;
            return value.ToString(format, culture);
        }
        public static string ToShortenedString(this int value)
        {
            if (value >= 1000000)
                return (value / 1000000) + "M";
            if (value >= 1000)
                return (value / 1000) + "K";
            return value.ToString();
        }
        public static string SeperatedNumberString(this int number, string separator = ".")
        {
            string raw = number.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            return raw.Replace(",", separator);
        }
        public static int QuantizeFloor(this int value, int step)
        {
            bool negative = value < 0;
            return (value.Abs() / step) * step * (negative == true ? -1 : 1);
        }
        public static int LerpSteep(this int current, int target, float t, float curvePower = 4f)
        {
            t = Mathf.Clamp(t, 0f, 1f);
            float curvedT = (float)Mathf.Pow(t, curvePower);
            return (int)Mathf.Round(current + (target - current) * curvedT);
        }
    }
}