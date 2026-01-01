using rinCore;
using RinCore;
using System.Collections.Generic;
using UnityEngine;

namespace RinCore
{
    public static class RandomHelper
    {
        public static IEnumerable<Vector2> Vec2Enumerable(int count, Vector2 origin, float size)
        {
            for (int i = 0; i < count; i++)
            {
                yield return origin + RinHelper.SeededRandomInsideUnitCircle() * size;
            }
        }
        public static int Random255 => RNG.Byte255;
        public static Vector2 Vec2(float size) => RinHelper.SeededRandomVector2() * size;
        public static float Range(float min, float max) => RNG.RandomFloatRange(min, max);
        public static int Sign() => RinHelper.RandomSign();
    }
}
