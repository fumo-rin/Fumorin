using UnityEngine;

namespace rinCore
{
    public static class FlagsExtensions
    {
        public static bool Match<T>(this T value, T flag) where T : struct, System.Enum
        {
            long valBits = System.Runtime.CompilerServices.Unsafe.As<T, long>(ref value);
            long flagBits = System.Runtime.CompilerServices.Unsafe.As<T, long>(ref flag);
            return (valBits & flagBits) != 0;
        }
        public static T AddFlag<T>(this T value, T flag) where T : struct, System.Enum
        {
            long valBits = System.Runtime.CompilerServices.Unsafe.As<T, long>(ref value);
            long flagBits = System.Runtime.CompilerServices.Unsafe.As<T, long>(ref flag);
            long result = valBits | flagBits;
            return System.Runtime.CompilerServices.Unsafe.As<long, T>(ref result);
        }

        public static T RemoveFlag<T>(this T value, T flag) where T : struct, System.Enum
        {
            long valBits = System.Runtime.CompilerServices.Unsafe.As<T, long>(ref value);
            long flagBits = System.Runtime.CompilerServices.Unsafe.As<T, long>(ref flag);
            long result = valBits & ~flagBits;
            return System.Runtime.CompilerServices.Unsafe.As<long, T>(ref result);
        }

        public static T SetMatch<T>(this T value, T flag, bool state) where T : struct, System.Enum
        {
            return state ? value.AddFlag(flag) : value.RemoveFlag(flag);
        }
    }
    public readonly struct FlagsRaw_Int
    {
        public const int None = 0;
        public const int All = ~0;

        public const int _1 = 1 << 0;
        public const int _2 = 1 << 1;
        public const int _3 = 1 << 2;
        public const int _4 = 1 << 3;
        public const int _5 = 1 << 4;
        public const int _6 = 1 << 5;
        public const int _7 = 1 << 6;
        public const int _8 = 1 << 7;
        public const int _9 = 1 << 8;
        public const int _10 = 1 << 9;
        public const int _11 = 1 << 10;
        public const int _12 = 1 << 11;
        public const int _13 = 1 << 12;
        public const int _14 = 1 << 13;
        public const int _15 = 1 << 14;
        public const int _16 = 1 << 15;
        public const int _17 = 1 << 16;
        public const int _18 = 1 << 17;
        public const int _19 = 1 << 18;
        public const int _20 = 1 << 19;
        public const int _21 = 1 << 20;
        public const int _22 = 1 << 21;
        public const int _23 = 1 << 22;
        public const int _24 = 1 << 23;
        public const int _25 = 1 << 24;
        public const int _26 = 1 << 25;
        public const int _27 = 1 << 26;
        public const int _28 = 1 << 27;
        public const int _29 = 1 << 28;
        public const int _30 = 1 << 29;
        public const int _31 = 1 << 30;
    }
}
