using System;
using System.Collections.Generic;

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

                targetedEvents[target.Value] =
                    targetedEvents.GetValueOrDefault(target.Value) + listener;
            }

            public static void Release(Action<T> listener, int? target = null)
            {
                if (!target.HasValue)
                {
                    untargettedEvent -= listener;
                    return;
                }

                if (!targetedEvents.TryGetValue(target.Value, out var action))
                    return;

                action -= listener;

                if (action == null)
                    targetedEvents.Remove(target.Value);
                else
                    targetedEvents[target.Value] = action;
            }

            public static void Publish(T eventData, int? target = null)
            {
                if (!target.HasValue)
                {
                    untargettedEvent?.Invoke(eventData);
                    return;
                }

                if (targetedEvents.TryGetValue(target.Value, out var action))
                    action.Invoke(eventData);
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
    public struct RinEventFilter
    {
        public int? hash => !string.IsNullOrEmpty(stringIndex) ? stringIndex.GetHashCode() : intIndex;
        public string stringIndex;
        public int? intIndex;
        public RinEventFilter(string id)
        {
            stringIndex = id;
            intIndex = null;
        }
        public RinEventFilter(int id)
        {
            stringIndex = null;
            intIndex = id;
        }
    }
    public static class EventBusTriggerExtension
    {
        public static void Publish<T>(this T item, RinEventFilter filter)
            where T : IRinEvent
        {
            RinBus.Publish(item, filter.hash);
        }
        public static void Publish<T>(this T item, int? target = null)
            where T : IRinEvent
        {
            RinBus.Publish(item, target);
        }
    }
}