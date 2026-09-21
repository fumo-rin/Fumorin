using System;
using System.Collections.Generic;

#region Jank
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit { }
}
#endregion
namespace rinCore
{
    public interface IRinEvent { };
    #region Editor Only Reset logic
#if UNITY_EDITOR
    public static partial class EventBus
    {
        private static readonly List<Action> clearAllDelegates = new List<Action>();

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
                {
                    clearAllDelegates[i]?.Invoke();
                }
            }
        }
    }
#endif
    #endregion

    public static partial class EventBus
    {
        private static class EventHolder<T>
        {
            #region Editor Reset
#if UNITY_EDITOR
            static EventHolder()
            {
                lock (clearAllDelegates)
                {
                    clearAllDelegates.Add(Clear);
                }
            }
#endif
            #endregion
            public static Action<T> OnEventRaised;
            public static void Clear()
            {
                OnEventRaised = null;
            }
        }

        public static void Bind<T>(Action<T> listener) => EventHolder<T>.OnEventRaised += listener;
        public static void Release<T>(Action<T> listener) => EventHolder<T>.OnEventRaised -= listener;
        public static void Publish<T>(T eventData) => EventHolder<T>.OnEventRaised?.Invoke(eventData);
        public static void Clear<T>() => EventHolder<T>.Clear();
    }

    public static class EventBusTriggerExtension
    {
        public static void Publish<T>(this T item) where T : IRinEvent
        {
            EventBus.Publish<T>(item);
        }
    }
}