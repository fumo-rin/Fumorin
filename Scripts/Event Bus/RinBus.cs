using System;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
#region Jank
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WebSocketSharp;
#endif
namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit { }
}
#endregion

namespace rinCore
{
    public interface IRinEvent { }

    public interface IRinEvent_Consumeable : IRinEvent
    {
        public bool IsConsumed { get; set; }
    }

    #region Editor Only Reset logic
#if UNITY_EDITOR
    public static partial class RinBus
    {
        private static readonly List<Action> clearAllDelegates = new();

        [InitializeOnLoadMethod]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void EditorCleanSlate()
        {
            ClearAll();
        }

        public static void ClearAll()
        {
            lock (clearAllDelegates)
            {
                for (int i = 0; i < clearAllDelegates.Count; i++)
                    clearAllDelegates[i]?.Invoke();
            }
        }
    }
#endif
    #endregion

    public static partial class RinBus
    {
        private static class EventHolder<T>
        {
            #region Editor Reset
#if UNITY_EDITOR
            static EventHolder()
            {
                lock (clearAllDelegates)
                    clearAllDelegates.Add(Clear);
            }
#endif
            #endregion

            private static readonly List<Action<T>> untargettedListeners = new();
            private static readonly Dictionary<int, List<Action<T>>> targetedListeners = new();

            #region Strapping
            public static void Bind(Action<T> listener, int? target = null)
            {
                if (!target.HasValue)
                {
                    if (!untargettedListeners.Contains(listener))
                        untargettedListeners.Add(listener);
                    return;
                }

                int key = target.Value;
                if (!targetedListeners.TryGetValue(key, out var list))
                {
                    list = new List<Action<T>>();
                    targetedListeners[key] = list;
                }
                if (!list.Contains(listener))
                    list.Add(listener);
            }

            public static void Release(Action<T> listener, int? target = null)
            {
                if (!target.HasValue)
                {
                    untargettedListeners.Remove(listener);
                    return;
                }

                int key = target.Value;
                if (targetedListeners.TryGetValue(key, out var list))
                {
                    list.Remove(listener);
                    if (list.Count == 0)
                        targetedListeners.Remove(key);
                }
            }

            public static void Publish(T eventData, int? target = null)
            {
                bool isConsumable = eventData is IRinEvent_Consumeable;
                IRinEvent_Consumeable consumable = isConsumable ? (IRinEvent_Consumeable)eventData : null;

                if (!target.HasValue)
                {
                    for (int i = 0; i < untargettedListeners.Count; i++)
                    {
                        if (isConsumable && consumable.IsConsumed)
                            break;
                        untargettedListeners[i]?.Invoke(eventData);
                    }
                    return;
                }

                if (targetedListeners.TryGetValue(target.Value, out var list))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (isConsumable && consumable.IsConsumed)
                            break;
                        list[i]?.Invoke(eventData);
                    }
                }
            }

            public static void Clear()
            {
                untargettedListeners.Clear();
                targetedListeners.Clear();
            }
            #endregion
        }

        public static void Bind<T>(Action<T> listener, int? target = null) =>
            EventHolder<T>.Bind(listener, target);

        public static void Release<T>(Action<T> listener, int? target = null) =>
            EventHolder<T>.Release(listener, target);

        public static void Publish<T>(T eventData, int? target = null) =>
            EventHolder<T>.Publish(eventData, target);

        public static void Clear<T>() =>
            EventHolder<T>.Clear();
    }

    public readonly struct RinEventFilter
    {
        public readonly string stringIndex;
        public readonly int rawHash;
        public readonly bool hasValue;

        public int? hash => hasValue ? rawHash : null;

        public RinEventFilter(string id)
        {
            stringIndex = id;
            if (!string.IsNullOrEmpty(id))
            {
                rawHash = GetStableHashCode(id);
                hasValue = true;
            }
            else
            {
                rawHash = 0;
                hasValue = false;
            }
        }

        public RinEventFilter(int id)
        {
            stringIndex = null;
            rawHash = id;
            hasValue = true;
        }

        private static int GetStableHashCode(string str)
        {
            unchecked
            {
                int hash = (int)2166136261;
                for (int i = 0; i < str.Length; i++)
                    hash = (hash ^ str[i]) * 16777619;
                return hash;
            }
        }

        public static implicit operator RinEventFilter(string id) => new(id);
        public static implicit operator RinEventFilter(int id) => new(id);
        public static implicit operator int?(RinEventFilter filter) => filter.hash;
    }

    public static class EventBusTriggerExtension
    {
        public static T Publish<T>(this T item, RinEventFilter filter)
            where T : IRinEvent
        {
            RinBus.Publish(item, filter.hash);
            return item;
        }

        public static T Publish<T>(this T item, int? target = null)
            where T : IRinEvent
        {
            RinBus.Publish(item, target);
            return item;
        }
    }
}