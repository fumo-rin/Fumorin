using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace rinCore
{
    public static class IntExtensions
    {
        #region Primes Gen & Rolling Cache
        private const int MaxRollingCacheSize = 2000;
        private static readonly HashSet<int> basePrimes = new() { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31 };
        private static readonly LinkedList<int> rollingPrimes = new(new int[] { 37, 41, 43, 47 });
        private static int highestCachedPrime = 47;

        private static void AddToRollingCache(int prime)
        {
            if (basePrimes.Contains(prime) || rollingPrimes.Contains(prime)) return;

            rollingPrimes.AddLast(prime);
            highestCachedPrime = prime;

            if (rollingPrimes.Count > MaxRollingCacheSize)
            {
                rollingPrimes.RemoveFirst();
            }
        }

        private static bool IsPrimeCached(int n)
        {
            if (n <= 1) return false;
            if (basePrimes.Contains(n) || rollingPrimes.Contains(n)) return true;

            int limit = (int)Math.Sqrt(n);

            foreach (int p in basePrimes)
            {
                if (p > limit) break;
                if (n % p == 0) return false;
            }

            foreach (int p in rollingPrimes)
            {
                if (p > limit) break;
                if (n % p == 0) return false;
            }
            for (int i = 37; i <= limit; i += 2)
            {
                if (n % i == 0) return false;
            }

            return true;
        }

        public static IEnumerable<int> Primes(this int count, int start = 2)
        {
            if (count <= 0) yield break;
            start = start == int.MinValue ? int.MaxValue : Math.Abs(start);
            if (start < 2) start = 2;

            int found = 0;
            int current = start;

            while (found < count)
            {
                int next = current.NextPrime(0);
                yield return next;
                current = next + 1;
                found++;
            }
        }

        public static int NextPrime(this int current, int skip = 0)
        {
            int candidate = current + 1;
            int skipped = 0;

            while (true)
            {
                if (IsPrimeCached(candidate))
                {
                    AddToRollingCache(candidate);
                    if (skipped == skip) return candidate;
                    skipped++;
                }
                candidate++;
            }
        }
        #endregion
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
            i = RNG.FloatRange(min.AsFloat(), max.AsFloat()).ToInt();
            return i;
        }
        public static int Spread(this int i, float percentage = 5f)
        {
            return (int)((float)i * (RNG.FloatRange(1 - percentage.Clamp(0f, 100f) * FloatExtensions.Percent, 1 + percentage.Clamp(0f, 100f) * FloatExtensions.Percent)));
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
        public static int Add(this int i, int other)
        {
            return (i + other);
        }
        public static int Mod(this int i, int mod)
        {
            return i % mod;
        }
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