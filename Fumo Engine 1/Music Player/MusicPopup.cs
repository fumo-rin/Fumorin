using FumoCore.Tools;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Fumorin
{
    public class MusicPopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text musicText;

        [Header("Settings")]
        [Tooltip("Speed of fade in/out in alpha units per second (0-255)")]
        [Range(1f, 500f)]
        [SerializeField] private float fadeSpeed = 500f;

        private static MusicPopup instance;
        private static readonly Queue<string> songQueue = new Queue<string>();
        private static Coroutine activeRoutine;

        private void Awake()
        {
            if (activeRoutine != null && instance != this && instance != null)
            {
                instance.StopCoroutine(activeRoutine);
            }
            instance = this;
            activeRoutine = null;
            if (musicText != null)
                musicText.color = musicText.color.Opacity(0);
        }
        public static void QueuePopup(string songText)
        {
            if (string.IsNullOrEmpty(songText) || instance == null)
                return;

            songQueue.Enqueue(songText);

            if (activeRoutine == null)
                activeRoutine = instance.StartCoroutine(instance.CO_PlayQueue());
        }
        private IEnumerator CO_PlayQueue()
        {
            while (songQueue.Count > 0)
            {
                string song = songQueue.Dequeue();
                musicText.text = "BGM: " + song;

                yield return FadeToAlpha(255);
                float displayDuration = 3.5f;
                float elapsed = 0f;
                while (elapsed < displayDuration)
                {
                    elapsed += Time.unscaledDeltaTime;

                    if (songQueue.Count > 0) break;

                    yield return null;
                }
                yield return FadeToAlpha(0);
            }

            musicText.text = "";
            activeRoutine = null;
        }
        private IEnumerator FadeToAlpha(byte target)
        {
            byte current = (byte)(musicText.color.a * 255f);

            while (current != target)
            {
                current = (byte)Mathf.MoveTowards(current, target, fadeSpeed * Time.unscaledDeltaTime);
                musicText.color = musicText.color.Opacity(current);
                yield return null;
            }
        }
    }
}
