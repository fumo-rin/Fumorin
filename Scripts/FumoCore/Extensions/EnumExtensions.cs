using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RinCore
{
    public static class EnumExtensions
    {
        public static IEnumerable<T> Foreach<T>(this Type enumType) where T : Enum
        {
            if (!enumType.IsEnum)
            {
                yield break;
            }

            foreach (var value in Enum.GetValues(enumType))
            {
                yield return (T)value;
            }
        }
        public static string ToSpacedString(this Enum key)
        {
            return key.ToString().SpaceByCapitals();
        }
    }
}
