using System;
using System.Collections.Generic;
using UnityEngine;

namespace rinCore
{
    public class FastUpdate : MonoBehaviour
    {
        public interface IFastRunable
        {
            public int FastIndex { get; protected set; }
            public void UpdateFastIndex(int index)
            {
                FastIndex = index;
            }
            public void RunFastFrame(float dt);
        }
        static FastUpdate Runner;
        static List<MonoBehaviour> items = new();

        void OnEnable()
        {
            Runner = this;
        }
        void Update()
        {
            if (Runner == null)
            {
                Runner = this;
            }
            if (Runner != this)
            {
                return;
            }
            if (GeneralManager.IsPaused)
                return;
            if (Time.timeScale <= 0f)
            {
                return;
            }

            MonoBehaviour item;
            float dt = Time.deltaTime;
            for (int i = 0; i < items.Count; i++)
            {
                item = items[i];
                if (item is not null and IFastRunable f && item != null)
                {
                    f.RunFastFrame(dt);
                }
            }
        }
        void OnDisable()
        {
            if (Runner == this)
                Runner = null;
        }

        public static void RegisterItem<T>(T item) where T : MonoBehaviour, IFastRunable
        {
            items ??= new();
            int index = items.Count;
            items.Add(item);

            if (item is IFastRunable r)
                r.UpdateFastIndex(index);
        }

        public static void UnregisterItem<T>(T item) where T : MonoBehaviour, IFastRunable
        {
            items ??= new();
            int index = item.FastIndex;
            if (items.Count == 0 || index < 0 || index >= items.Count) return;

            int lastIndex = items.Count - 1;
            if (index != lastIndex)
            {
                MonoBehaviour lastItem = items[lastIndex];
                items[index] = lastItem;
                if (lastItem is IFastRunable r)
                    r.UpdateFastIndex(index);
            }
            items.RemoveAt(lastIndex);
        }
    }
}
