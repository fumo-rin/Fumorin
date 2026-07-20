using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace rinCore
{
    public class GameXYTextDisplay : MonoBehaviour
    {
        #region Text Packet
        [System.Serializable]
        public class textPacket
        {
            public float fadeIn = 0.5f;
            public float fadeOut = 0.5f;
            public float duration = 2f;
            public float DurationWithFade => duration + fadeIn + fadeOut;
            public Color32 color = ColorHelper.White;

            public Vector2 a01 = new(0.3f, 0.5f);
            public Vector2 b01 = new(0.7f, 0.7f);

            public float fontSize = 28f;

            public HorizontalAlignmentOptions horizontalAlignment = HorizontalAlignmentOptions.Center;
            public VerticalAlignmentOptions verticalAlignment = VerticalAlignmentOptions.Bottom;
            public textPacket()
            {
                this.fadeIn = 0.5f;
                this.fadeOut = 0.5f;
            }
        }
        #endregion

        private class ActiveTextTracker
        {
            public Coroutine RunningCoroutine;
            public TMP_Text CloneInstance;
        }

        [SerializeField] TMP_Text cloneable;
        [SerializeField] RectTransform textSpaceAnchor;

        static GameXYTextDisplay instance;

        private readonly Dictionary<string, ActiveTextTracker> activeTrackers = new();

        private void Awake()
        {
            instance = this;
            cloneable.gameObject.SetActive(false);
        }

        public static void CreateText(string text, textPacket packet, string key = "")
        {
            if (!RinHelper.ValidGameObjects(instance))
                return;

            if (!string.IsNullOrEmpty(key) && instance.activeTrackers.TryGetValue(key, out var activeTracker))
            {
                if (activeTracker.RunningCoroutine != null)
                {
                    instance.StopCoroutine(activeTracker.RunningCoroutine);
                }
                if (activeTracker.CloneInstance != null)
                {
                    Destroy(activeTracker.CloneInstance.gameObject);
                }
                instance.activeTrackers.Remove(key);
            }

            TMP_Text clone = Instantiate(instance.cloneable, instance.textSpaceAnchor);
            RectTransform rt = clone.rectTransform;
            Rect parentRect = instance.textSpaceAnchor.rect;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Vector2 min = Vector2.Min(packet.a01, packet.b01);
            Vector2 max = Vector2.Max(packet.a01, packet.b01);

            Vector2 center = (min + max) * 0.5f;
            Vector2 size = max - min;

            rt.anchoredPosition = new Vector2(
                (center.x - 0.5f) * parentRect.width,
                (center.y - 0.5f) * parentRect.height);

            rt.sizeDelta = new Vector2(
                size.x * parentRect.width,
                size.y * parentRect.height);

            clone.enableAutoSizing = true;
            clone.fontSizeMax = packet.fontSize;
            clone.fontSizeMin = packet.fontSize * 0.25f;

            clone.horizontalAlignment = packet.horizontalAlignment;
            clone.verticalAlignment = packet.verticalAlignment;

            clone.text = text;
            clone.color = packet.color.Opacity(0);
            clone.gameObject.SetActive(true);

            Coroutine newRoutine = instance.StartCoroutine(CO_Text(clone, packet, key));

            if (!string.IsNullOrEmpty(key))
            {
                instance.activeTrackers[key] = new ActiveTextTracker
                {
                    RunningCoroutine = newRoutine,
                    CloneInstance = clone
                };
            }
        }

        private static IEnumerator CO_Text(TMP_Text clone, textPacket packet, string key)
        {
            float entry = packet.fadeIn;
            while (entry > 0)
            {
                float lerp01 = entry.MapTo01(packet.fadeIn, 0f, true);
                clone.color = clone.color.Opacity(lerp01.MapFrom01(0f, 255f).ToByte());
                entry -= Time.deltaTime;
                yield return null;
            }

            clone.color = packet.color;

            yield return packet.duration.WaitForSeconds();

            float exit = packet.fadeOut;
            while (exit > 0)
            {
                float lerp01 = exit.MapTo01(packet.fadeOut, 0f, true);
                clone.color = clone.color.Opacity(lerp01.MapFrom01(255f, 0f).ToByte());
                exit -= Time.deltaTime;
                yield return null;
            }

            if (!string.IsNullOrEmpty(key) && instance.activeTrackers.TryGetValue(key, out var currentTracker))
            {
                if (currentTracker.CloneInstance == clone)
                {
                    instance.activeTrackers.Remove(key);
                }
            }

            Destroy(clone.gameObject);
        }
    }
}