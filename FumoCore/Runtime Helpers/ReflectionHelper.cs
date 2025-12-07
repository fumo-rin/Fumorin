using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FumoCore.Tools
{
    public static partial class FCHelper
    {
        public static Type[] GetAllTypes<T>()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
                })
                .Where(t => t.IsSubclassOf(typeof(T)) && !t.IsAbstract)
                .ToArray();
        }
        public static IEnumerable<TField> GetFieldsOfType<TField>(this IEnumerable<object> objects,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (objects == null) yield break;
            foreach (var obj in objects)
            {
                if (obj == null) continue;

                var type = obj.GetType();
                var fields = type.GetFields(bindingFlags);

                foreach (var field in fields)
                {
                    if (typeof(TField).IsAssignableFrom(field.FieldType))
                    {
                        var value = field.GetValue(obj);
                        if (value is TField tValue)
                            yield return tValue;
                    }
                }
            }
        }
    }
}
