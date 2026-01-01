using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RinCore
{
    #region Drawline
    public static partial class RinHelper // Draw Line
    {
        public static void DrawLine2D(Vector2 from, Vector2 to, Color32? c, float time = 0.5f)
        {
#if UNITY_EDITOR
            Debug.DrawLine(from, to, c == null ? Color.white : c.Value, time);
#endif
        }
    }
    #endregion
    #region Old Benchmark
    public static partial class RinHelper // Benchmark
    {
        public struct BenchmarkOld
        {
            string name;
            System.DateTime startTime;
            public BenchmarkOld(string name, System.Action runAction)
            {
                this.name = name;
                startTime = System.DateTime.Now;
                runAction?.Invoke();
                TimeSpan d = System.DateTime.Now - startTime;
                Debug.Log($"{name} : Time(ms) : {d.Ticks * 0.0001f}");
            }
        }
    }
    #endregion
}