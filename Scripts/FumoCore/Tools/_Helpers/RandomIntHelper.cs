using UnityEngine;

namespace RinCore
{
    public partial class Helper
    {
        static int[] randomIntTable;
        static int randomIntIndex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void FillTable()
        {
            randomIntIndex = 0;
            int maxValue = 255;
            int length = 4096;
            randomIntTable = new int[length];
            int seed = 3378;
            System.Random r = new System.Random(seed);
            for (int i = 0; i < length; i++)
            {
                randomIntTable[i] = r.Next(0, maxValue);
            }
        }

        static int GetRandomInt()
        {
            if (randomIntTable == null)
                FillTable();

            if (randomIntIndex >= randomIntTable.Length)
                randomIntIndex = 0;

            return randomIntTable[randomIntIndex++];
        }

        public static int SeededRandomInt256 => GetRandomInt();

        public static int RandomSign()
        {
            return Random.value < 0.5f ? -1 : 1;
        }
        public static float SeededRandomFloat()
        {
            return SeededRandomInt256 / 256f;
        }

        public static float SeededRandomFloat(float min, float max)
        {
            return min + (max - min) * SeededRandomFloat();
        }
        public static Vector2 SeededRandomVector2()
        {
            float x = SeededRandomFloat();
            float y = SeededRandomFloat();
            return new Vector2(x, y);
        }
        public static Vector3 SeededRandomVector3()
        {
            float x = SeededRandomFloat();
            float y = SeededRandomFloat();
            float z = SeededRandomFloat();
            return new Vector3(x, y, z);
        }
        public static Vector2 SeededRandomInsideUnitCircle()
        {
            Vector2 v;
            do
            {
                v = new Vector2(SeededRandomFloat() * 2f - 1f, SeededRandomFloat() * 2f - 1f);
            } while (v.sqrMagnitude > 1f);
            return v;
        }
        public static Vector3 SeededRandomInsideUnitSphere()
        {
            Vector3 v;
            do
            {
                v = new Vector3(SeededRandomFloat() * 2f - 1f, SeededRandomFloat() * 2f - 1f, SeededRandomFloat() * 2f - 1f);
            } while (v.sqrMagnitude > 1f);
            return v;
        }
    }
}
