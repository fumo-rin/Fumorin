using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FumoCore.Tools
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Initialize : Attribute
    {
        public int LoadOrder;
        public Initialize(int loadOrder)
        {
            LoadOrder = loadOrder;
        }
    }
    #region Benchmark
    public static class BenchmarkHelper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartBenchmark()
        {
            var methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                })
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.GetCustomAttribute<Initialize>() != null);

            var orderedMethods = methods
                .Select(m => new { Method = m, Attr = m.GetCustomAttribute<Initialize>() })
                .Where(x => x.Attr != null)
                .OrderBy(x => x.Attr.LoadOrder);

            foreach (var entry in orderedMethods)
            {
                var method = entry.Method;
                var attr = entry.Attr;

                if (method.GetParameters().Length == 0 && method.ReturnType == typeof(void))
                {
                    InvokeWithBenchmark(method, attr);
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Invalid DialogueCommand method '{method.Name}' — must be 'static void Method()'");
                }
            }
        }
        public static void InvokeWithBenchmark(MethodInfo method, Initialize b, object target = null)
        {
            if (b == null)
            {
                Debug.Log($"Method {method.Name} does not have BenchmarkAttribute.");
                return;
            }

            string name = method.Name;

            var startTime = DateTime.Now;
            method.Invoke(target, null);
            var duration = DateTime.Now - startTime;

            Debug.Log($"Benchmark - {name}({b.LoadOrder}) (Time in ms) : {duration.Ticks * 0.0001f}");
        }
    }
    #endregion
}
