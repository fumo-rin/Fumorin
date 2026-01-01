using rinCore;
using UnityEngine;

namespace RinCore
{
    public partial class RinHelper
    {
        public static int RandomSign() => RNG.RandomSign;
        public static Vector2 SeededRandomVector2() => RNG.SeededRandomVector2;
        public static Vector3 SeededRandomVector3() => RNG.SeededRandomVector3;
        public static Vector2 SeededRandomInsideUnitCircle() => RNG.SeededRandomInsideUnitCircle;
        public static Vector3 SeededRandomInsideUnitSphere() => RNG.SeededRandomInsideUnitSphere;
    }
}
