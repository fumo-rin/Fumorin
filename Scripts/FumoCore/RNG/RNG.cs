using Pathfinding.Graphs.Grid;
using RinCore;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace rinCore
{
    #region Base RNG Builder
    public static partial class RNG
    {
        #region Random Float
        static float NextFloat01()
        {
            int a = Byte255;
            int b = Byte255;
            int c = Byte255;
            int value = (a << 16) | (b << 8) | c;
            return value / 16777216f;
        }
        public static float SeededRandomFloat01 => NextFloat01();
        [QFSW.QC.Command("-rng-float-range")]
        public static float RandomFloatRange(float min, float max) => min + (max - min) * SeededRandomFloat01;
        #endregion

        #region Base Generator
        static byte[] randomIntTable;
        static int randomIntIndex;
        static System.Random rng;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void FillTable()
        {
            randomIntIndex = 0;
            int maxValue = 255;
            int length = 4096;
            randomIntTable = new byte[length];
            int seed = 3378;
            rng = new System.Random(seed);
            for (int i = 0; i < length; i++)
            {
                randomIntTable[i] = ((byte)rng.Next(0, maxValue));
            }
        }
        [QFSW.QC.Command("-rng-byte")]
        static byte GetRandomByte()
        {
            if (randomIntTable == null)
                FillTable();

            if (randomIntIndex >= randomIntTable.Length)
                randomIntIndex = 0;

            return randomIntTable[randomIntIndex++];
        }
        public static int Int255 => (int)GetRandomByte();
        public static byte Byte255 => GetRandomByte();
        #endregion
        public static int RandomSign => (Byte255 & 1) == 0 ? -1 : 1;
        public static Vector2 SeededRandomVector2
        {
            get
            {
                float x = SeededRandomFloat01;
                float y = SeededRandomFloat01;
                return new Vector2(x, y);
            }
        }
        public static Vector3 SeededRandomVector3
        {
            get
            {
                float x = SeededRandomFloat01;
                float y = SeededRandomFloat01;
                float z = SeededRandomFloat01;
                return new Vector3(x, y, z);
            }
        }
        public static Vector2 SeededRandomInsideUnitCircle
        {
            get
            {
                Vector2 v;
                do
                {
                    v = new Vector2(SeededRandomFloat01 * 2f - 1f, SeededRandomFloat01 * 2f - 1f);
                } while (v.sqrMagnitude > 1f);
                return v;
            }
        }
        public static Vector3 SeededRandomInsideUnitSphere
        {
            get
            {
                Vector3 v;
                do
                {
                    v = new Vector3(SeededRandomFloat01 * 2f - 1f, SeededRandomFloat01 * 2f - 1f, SeededRandomFloat01 * 2f - 1f);
                } while (v.sqrMagnitude > 1f);
                return v;
            }
        }
    }
    #endregion
    #region Float Range
    public static partial class RNG
    {
        public static float FloatRange(float min, float max)
        {
            return RandomFloatRange(min, max);
        }
    }
    #endregion
}
