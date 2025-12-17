using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RinCore
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
