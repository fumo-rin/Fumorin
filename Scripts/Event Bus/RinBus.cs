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

            public static Action<T> untargettedEvent;
            private static readonly Dictionary<int, Action<T>> targetedEvents = new();

            #region Strapping
            public static void Bind(Action<T> listener, int? target = null)
            {
                if (!target.HasValue)
                {
                    untargettedEvent += listener;
                    return;
                }

                int key = target.Value;
                if (targetedEvents.TryGetValue(key, out var existing))
                {
                    targetedEvents[key] = existing + listener;
                }
                else
                {
                    targetedEvents[key] = listener;
                }
            }

            public static void Release(Action<T> listener, int? target = null)
            {
                if (!target.HasValue)
                {
                    untargettedEvent -= listener;
                    return;
                }

                int key = target.Value;
                if (!targetedEvents.TryGetValue(key, out var action))
                    return;

                action -= listener;

                if (action == null)
                    targetedEvents.Remove(key);
                else
                    targetedEvents[key] = action;
            }

            public static void Publish(T eventData, int? target = null)
            {
                if (!target.HasValue)
                {
                    untargettedEvent?.Invoke(eventData);
                    return;
                }

                if (targetedEvents.TryGetValue(target.Value, out var action))
                    action?.Invoke(eventData);
            }

            public static void Clear()
            {
                untargettedEvent = null;
                targetedEvents.Clear();
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
                //hashing from hell
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