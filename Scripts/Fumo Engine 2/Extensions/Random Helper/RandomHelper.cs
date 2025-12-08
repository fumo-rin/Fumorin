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
                yield return origin + Helper.SeededRandomInsideUnitCircle() * size;
            }
        }
        public static Vector2 Vec2(float size) => Helper.SeededRandomVector2() * size;
        public static float Range(float min, float max) => Helper.SeededRandomFloat(min, max);
        public static int Sign() => Helper.RandomSign();
    }
}
