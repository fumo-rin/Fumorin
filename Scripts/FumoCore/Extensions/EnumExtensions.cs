using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace rinCore
{
    public static class EnumHelper
    {
        public static IEnumerable<T> Foreach<T>() where T : Enum
        {
            foreach (T value in Enum.GetValues(typeof(T)))
                yield return value;
        }
        public static IEnumerable<string> ForeachReadableNames<T>() where T : Enum
        {
            foreach (T value in Foreach<T>())
            {
                yield return value.ReadableFullString();
            }
        }
    }
    public static class EnumExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasSelectedFlags<T>(this T e, T flags) where T : unmanaged, Enum
        {
            long value = 0;
            long target = 0;

            Unsafe.As<long, T>(ref value) = e;
            Unsafe.As<long, T>(ref target) = flags;

            return (value & target) == target;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyOfFlags<T>(this T e, T flags) where T : unmanaged, Enum
        {
            long value = 0;
            long target = 0;

            Unsafe.As<long, T>(ref value) = e;
            Unsafe.As<long, T>(ref target) = flags;

            return (value & target) != 0;
        }
        public static string ToSpacedString(this Enum key)
        {
            return key.ToString().SpaceByCapitals();
        }
        public static string ReadableFullString(this Enum key)
        {
            if (key == null)
                return string.Empty;

            string enumTypeName = key.GetType().Name;
            string enumValueName = key.ToString();

            return $"{enumTypeName}_{enumValueName}";
        }
    }
}
