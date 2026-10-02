using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    public class DialogueOnScreen : MonoBehaviour
    {
        [System.Serializable]
        public struct EntryStruct
        {
            public DialogueCharacterSO character;
            public string dialogue;
            public float expireDuration;
            public Entry AsEntry => new(character, dialogue, expireDuration);
        }
        public record Entry(DialogueCharacterSO character, string dialogue, float expireDuration = 3f) : IRinEvent;

        [Header("UI References")]
        [SerializeField] CanvasGroup dialogueGroup;
        [SerializeField] Image characterDisplay;
        [SerializeField] TMP_Text dialogueText;
        [SerializeField] TMP_Text characterNameText;
        [SerializeField] AudioSource speechPlayer;

        [Header("Text Scrolling & Speech Settings")]
        [SerializeField] float charactersPerSecond = 30f;
        [SerializeField, Range(1, 6)] int charactersPerSpeechChunk = 3;

        [Header("Group Fade & Scale Animations")]
        [SerializeField] float fadeDuration = 0.25f;
        [SerializeField] Vector3 collapsedScale = new Vector3(0.85f, 0.85f, 1f);

        Queue<Entry> entries = new();
        Coroutine currentDialogueRoutine;
        Coroutine activeJiggleRoutine;
        Vector3 initialImagePosition;

        bool originalBlocksRaycasts = true;

        static readonly HashSet<char> ExcludedPunctuation = new() { '\'', '"', '‘', '’', '“', '”', ',' };

        private void Awake()
        {
            if (characterDisplay != null)
            {
                initialImagePosition = characterDisplay.rectTransform.anchoredPosition;
            }

            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
            }

            if (dialogueGroup != null)
            {
                originalBlocksRaycasts = dialogueGroup.blocksRaycasts;

                dialogueGroup.alpha = 0f;
                dialogueGroup.transform.localScale = collapsedScale;
                dialogueGroup.interactable = false;
                dialogueGroup.blocksRaycasts = false;
            }
        }

        private void OnEnable()
        {
            EventBus.Bind<Entry>(QueueEntry);
        }

        private void OnDisable()
        {
            EventBus.Release<Entry>(QueueEntry);
            if (currentDialogueRoutine != null)
            {
                StopCoroutine(currentDialogueRoutine);
                currentDialogueRoutine = null;
            }
            if (activeJiggleRoutine != null)
            {
                StopCoroutine(activeJiggleRoutine);
                activeJiggleRoutine = null;
            }
        }

        void QueueEntry(Entry e)
        {
            entries.Enqueue(e);
            if (currentDialogueRoutine == null)
            {
                currentDialogueRoutine = StartCoroutine(ProcessQueue());
            }
        }

        IEnumerator ProcessQueue()
        {
            if (entries.Count > 0)
            {
                Entry firstEntry = entries.Peek();
                SetupCharacterUI(firstEntry);
            }

            yield return AnimateGroupVisibility(true);

            while (entries.Count > 0)
            {
                Entry current = entries.Dequeue();
                yield return RunEntry(current);
            }

            yield return AnimateGroupVisibility(false);

            ClearUI();

            currentDialogueRoutine = null;
        }

        private void SetupCharacterUI(Entry e)
        {
            if (e.character == null) return;

            if (characterNameText != null)
                characterNameText.text = e.character.characterName;

            if (characterDisplay != null)
            {
                characterDisplay.enabled = true;
                characterDisplay.sprite = e.character.sprite;
            }
        }

        private void ClearUI()
        {
            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
            }

            if (characterNameText != null)
            {
                characterNameText.text = string.Empty;
            }

            if (characterDisplay != null)
            {
                characterDisplay.sprite = null;
                characterDisplay.enabled = false;
            }
        }

        private IEnumerator AnimateGroupVisibility(bool show)
        {
            if (dialogueGroup == null) yield break;

            float startAlpha = dialogueGroup.alpha;
            float targetAlpha = show ? 1f : 0f;

            Vector3 startScale = dialogueGroup.transform.localScale;
            Vector3 targetScale = show ? Vector3.one : collapsedScale;

            if (show)
            {
                dialogueGroup.interactable = true;
                dialogueGroup.blocksRaycasts = originalBlocksRaycasts;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration);

                dialogueGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                dialogueGroup.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

                yield return null;
            }

            dialogueGroup.alpha = targetAlpha;
            dialogueGroup.transform.localScale = targetScale;

            if (!show)
            {
                dialogueGroup.interactable = false;
                dialogueGroup.blocksRaycasts = false;
            }
        }

        private IEnumerator RunEntry(Entry e)
        {
            SetupCharacterUI(e);

            if (e.character != null && characterDisplay != null)
            {
                Jiggle(e.character);
            }

            if (dialogueText != null)
            {
                yield return AnimateTypewriterText(e);
            }

            yield return new WaitForSeconds(e.expireDuration);
        }

        private IEnumerator AnimateTypewriterText(Entry e)
        {
            const float CHAR_DELAY = 1f / 35f;
            const float PUNCTUATION_DELAY = 0.25f;
            const float PAUSE_DELAY = 0.05f;

            int speakValue = 0;
            int wordCharCount = 0;

            void EndWord(bool fastForward = false)
            {
                if (e.character == null) return;

                if (!fastForward && speakValue > 0)
                {
                    if (e.character.Speak(speechPlayer, speakValue))
                    {
                        Jiggle(e.character);
                    }
                }

                speakValue = 0;
                wordCharCount = 0;
            }

            void IncrementSpeak(char c, bool fastForward)
            {
                if (char.IsLetterOrDigit(c) && !fastForward)
                    speakValue += c.GetHashCode();

                wordCharCount++;

                if (wordCharCount >= charactersPerSpeechChunk)
                    EndWord(fastForward);
            }

            bool IsPauseChar(char c) => char.IsSymbol(c) || char.IsWhiteSpace(c);

            string fullText = e.dialogue;
            dialogueText.text = fullText;
            dialogueText.maxVisibleCharacters = 0;

            int charIndex = 0;
            float charTimer = 0f;

            while (charIndex < fullText.Length)
            {
                char currentChar = fullText[charIndex];
                bool isExcluded = ExcludedPunctuation.Contains(currentChar);

                if (charTimer == 0f)
                {
                    dialogueText.maxVisibleCharacters = charIndex + 1;

                    if (isExcluded || char.IsLetterOrDigit(currentChar))
                        IncrementSpeak(currentChar, false);
                    else
                        EndWord(false);
                }

                float delay = CHAR_DELAY;

                if (isExcluded)
                {
                    delay = CHAR_DELAY;
                }
                else if (char.IsPunctuation(currentChar))
                {
                    delay = PUNCTUATION_DELAY;
                }
                else if (IsPauseChar(currentChar))
                {
                    delay = PAUSE_DELAY;
                }

                charTimer += Mathf.Min(Time.unscaledDeltaTime, 0.033f);
                if (charTimer < delay)
                {
                    yield return null;
                    continue;
                }

                charTimer = 0f;
                charIndex++;
                yield return null;
            }

            EndWord(false);
            dialogueText.maxVisibleCharacters = fullText.Length;
        }

        private void Jiggle(DialogueCharacterSO character)
        {
            if (characterDisplay == null) return;

            if (activeJiggleRoutine != null)
            {
                StopCoroutine(activeJiggleRoutine);
                characterDisplay.rectTransform.anchoredPosition = initialImagePosition;
            }

            activeJiggleRoutine = StartCoroutine(CO_JiggleSprite(character));
        }

        private IEnumerator CO_JiggleSprite(DialogueCharacterSO c)
        {
            float duration = 0.2f;
            float elapsed = 0f;
            characterDisplay.sprite = c.talkSprite != null ? c.talkSprite : c.sprite;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                if (characterDisplay.sprite != c.sprite && elapsed >= 0.015f)
                    characterDisplay.sprite = c.sprite;

                float wave = Mathf.Sin((elapsed / duration) * Mathf.PI);
                float x = 1f - (wave * 0.2f);
                float y = 1f + (wave * 0.15f);
                characterDisplay.rectTransform.localScale = new Vector3(x, y, 1f);
                yield return null;
            }

            characterDisplay.sprite = c.sprite;
            characterDisplay.rectTransform.localScale = Vector3.one;
            activeJiggleRoutine = null;
        }
    }
}