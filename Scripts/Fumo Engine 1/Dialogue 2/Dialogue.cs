using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace rinCore
{
    #region Dialogue Part & Collection
    public partial class Dialogue
    {
        [System.Serializable]
        public struct DialoguePart
        {
            [TextArea(1, 10)]
            [SerializeField] string ContainedMessage;
            public string ProcessedMessage
            {
                get
                {
                    return ContainedMessage;
                }
            }
            public string CharacterName;
            public string Command;
            public DialoguePart(string message)
            {
                ContainedMessage = message;
                CharacterName = "";
                Command = "";
            }
        }
        [System.Serializable]
        public struct DialogueCollection
        {
            public List<DialoguePart> parts;
            public DialogueCollection(List<DialoguePart> newParts)
            {
                parts = new();
                foreach (var item in newParts)
                {
                    parts.Add(item);
                }
            }
        }
    }
    #endregion
    #region Speak
    public partial class Dialogue
    {
        static int SpeakValue;
        static int wordCharCount;
        const int CHAR_COUNT_TO_SPEAK = 3;
        private static void ResetSpeech()
        {
            SpeakValue = 0;
            wordCharCount = 0;
        }
    }
    #endregion
    #region Set Text
    public partial class Dialogue
    {
        private static void SetTextMessage(DialoguePart p, string message, string nameOverride = "")
        {
            instance.dialogueText.maxVisibleCharacters = 0;
            instance.dialogueText.text = message;
            instance.characterNameText.text = nameOverride == "" ? p.CharacterName : nameOverride;
        }
        private static void UpdateText(int letterCount, out bool IsMessageDone)
        {
            IsMessageDone = false;
            instance.dialogueText.maxVisibleCharacters = letterCount;
            if (instance.dialogueText.text.Length <= letterCount)
            {
                IsMessageDone = true;
            }
        }
    }
    #endregion
    #region Helper Classes
    public partial class Dialogue
    {
        private class WaitForContinueOrTime : IEnumerator
        {
            float endTime;
            IEnumerator enumerator;
            public WaitForContinueOrTime(float time)
            {
                endTime = time + Time.unscaledTime;
                enumerator = Wait();
            }
            public object Current => MoveNext();
            public bool MoveNext() => enumerator.MoveNext();
            public void Reset() => enumerator.Reset();
            IEnumerator Wait()
            {
                while (Time.unscaledTime < endTime)
                {
                    if (GeneralManager.IsPaused)
                    {
                        endTime += Time.unscaledDeltaTime;
                        yield return null;
                    }
                    if (!GeneralManager.IsPaused && ContinuePressedOrHeldForALongTime)
                    {
                        yield break;
                    }
                    yield return null;
                }
            }
        }
    }
    #endregion
    #region Jiggle
    public partial class Dialogue
    {
        static Dictionary<GameObject, Coroutine> activeJiggle;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ReinitializeJiggle()
        {
            activeJiggle = null;
        }
        private static void Jiggle(DialogueCharacterSO character)
        {
            if (activeJiggle == null)
            {
                activeJiggle = new();
            }
            if (instance == null)
            {
                return;
            }
            Image image = null;

            bool isPlayer = PlayerCharacter == null ? false : PlayerCharacter.characterName == character.characterName;
            if (isPlayer)
            {
                instance.playerChatAnimator.SetTrigger(instance.animationChatStringKey);
                instance.playerSprite.enabled = true;
                image = instance.playerSprite;
            }
            else
            {
                instance.otherChatAnimator.SetTrigger(instance.animationChatStringKey);
                image = instance.otherSprite;
                instance.otherSprite.enabled = true;
            }
            IEnumerator CO_JiggleSprite(Image image, DialogueCharacterSO c)
            {
                instance.SetSprite(image, c.talkSprite);
                yield return 0.015f.WaitForSeconds();
                instance.SetSprite(image, c.sprite);
                if (activeJiggle.TryGetValue(image.gameObject, out Coroutine r))
                {
                    instance.StopCoroutine(r);
                    activeJiggle.Remove(image.gameObject);
                }
            }
            if (activeJiggle != null)
            {
                if (activeJiggle.TryGetValue(image.gameObject, out Coroutine r))
                {
                    instance.StopCoroutine(r);
                    activeJiggle.Remove(image.gameObject);
                }
            }
            activeJiggle.Add(image.gameObject, instance.StartCoroutine(CO_JiggleSprite(image, character)));
        }
        private void SetSprite(Image sr, Sprite sprite)
        {
            sr.sprite = sprite;
        }
    }
    #endregion
    #region Set Player Character
    public partial class Dialogue
    {
        public static bool TrySetPlayerCharacter(DialogueCharacterSO c)
        {
            PlayerCharacter = c;
            return true;
        }
        [Initialize(50)]
        static void ReinitializeCharacterOverrides()
        {
            characterOverrides = new();
            SceneLoader.WhenFinishedLoadingAdditives += () => characterOverrides = new();
        }
        static Dictionary<string, DialogueCharacterSO> characterOverrides;
        public static void AddCharacterOverride(string charName, DialogueCharacterSO c)
        {
            characterOverrides[charName] = c;
        }
        public static bool TryGetCharacterOverride(string key, out DialogueCharacterSO c)
        {
            return characterOverrides.TryGetValue(key, out c);
        }
    }
    #endregion
    #region Run Dialogue
    public partial class Dialogue
    {
        public static void LoadDialogue(DialogueStackSO stack, Action whenDialogueEnd = null)
        {
            Stop();
            instance.activeDialogueRoutine = instance.StartCoroutine(RunDialogue(stack, 0f, whenDialogueEnd));
        }
        private static IEnumerator RunDialogue(DialogueStackSO stack, float delay, Action whenDialogueEnd)
        {
            yield return delay.WaitForSeconds(cached: false);
            unscaledDialogueStartTime = Time.unscaledTime;
            SetBoxVisibility(true);
            instance.playerSprite.enabled = false;
            instance.otherSprite.enabled = false;
            if (PlayerCharacter != null) Jiggle(PlayerCharacter);
            yield return RunStack(stack);
            whenDialogueEnd?.Invoke();
            SetBoxVisibility(false);
        }
        public static void Stop()
        {
            if (instance == null)
            {
                return;
            }
            if (instance.activeDialogueRoutine != null)
            {
                instance.StopCoroutine(instance.activeDialogueRoutine);
                instance.activeDialogueRoutine = null;
            }
            SetBoxVisibility(false);
        }
    }
    #endregion
    public partial class Dialogue : MonoBehaviour
    {
        const float HOLD_THRESHOLD = 0.85f;
        #region Run Dialogue
        static readonly HashSet<char> ExcludedPunctuation = new() { '\'', '"', '‘', '’', '“', '”', ',' };
        static IEnumerator RunStack(DialogueStackSO stack)
        {
            const float CHAR_DELAY = 0.015f;
            const float PUNCTUATION_DELAY = 0.25f;
            const float PAUSE_DELAY = 0.05f;
            const float NATURAL_WAIT = 5f;

            DialogueCharacterSO resultCharacter;

            void EndWord(bool fastForward = false)
            {
                if (instance == null || resultCharacter == null) return;

                if (!fastForward && SpeakValue > 0)
                {
                    resultCharacter.Speak(instance.speechPlayer, SpeakValue);
                    Jiggle(resultCharacter);
                }

                SpeakValue = 0;
                wordCharCount = 0;
            }

            void IncrementSpeak(char c, bool fastForward)
            {
                if (char.IsLetterOrDigit(c) && !fastForward)
                    SpeakValue += c.GetHashCode();

                wordCharCount++;

                if (wordCharCount >= CHAR_COUNT_TO_SPEAK)
                    EndWord(fastForward);
            }

            bool IsPauseChar(char c) => char.IsSymbol(c) || char.IsWhiteSpace(c);

            foreach (var d in stack.DialogueParts)
            {
                stack.TryGetCharacter(d.CharacterName, out resultCharacter);
                Jiggle(resultCharacter);

                if (!string.IsNullOrWhiteSpace(d.Command) && ShmupCommands.TryRun(d.Command))
                    continue;

                string message = d.ProcessedMessage;
                Dialogue.SetTextMessage(d, message);
                instance.dialogueText.ForceMeshUpdate();
                ResetSpeech();

                int charIndex = 0;
                bool messageDone = false;
                float charTimer = 0f;
                bool allowHoldSkip = false;

                while (!messageDone && charIndex < message.Length)
                {
                    while (GeneralManager.IsPaused)
                        yield return null;

                    char currentChar = message[charIndex];
                    bool isExcluded = ExcludedPunctuation.Contains(currentChar);

                    if (Dialogue.instance.SkipDialogueKey.JustPressed())
                    {
                        Dialogue.UpdateText(message.Length, out messageDone);
                        EndWord(false);
                        yield return null;
                        break;
                    }
                    if (charIndex > 0 && ContinuePressedOrHeldForALongTime)
                        allowHoldSkip = true;

                    if (charTimer == 0f)
                    {
                        Dialogue.UpdateText(charIndex + 1, out messageDone);

                        if (isExcluded || (char.IsLetterOrDigit(currentChar)))
                            IncrementSpeak(currentChar, allowHoldSkip);
                        else
                            EndWord(allowHoldSkip);
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

                    if (allowHoldSkip)
                        delay *= 0.05f;

                    // 3. The Waiting Room
                    charTimer += Mathf.Min(Time.unscaledDeltaTime, 0.033f);
                    if (charTimer < delay)
                    {
                        yield return null;
                        continue;
                    }

                    // 4. Reset for next character
                    charTimer = 0f;
                    charIndex++;
                    yield return null;
                }

                EndWord(allowHoldSkip);

                float waitEnd = Time.unscaledTime + NATURAL_WAIT;
                while (Time.unscaledTime < waitEnd)
                {
                    if (GeneralManager.IsPaused)
                        waitEnd += Time.unscaledDeltaTime;
                    else if (ContinuePressedOrHeldForALongTime)
                    {
                        yield return null;
                        break;
                    }
                    yield return null;
                }
            }

            if (instance != null)
                instance.activeDialogueRoutine = null;
        }
        #endregion
        static DialogueCharacterSO PlayerCharacter;
        [SerializeField] TMP_Text dialogueText, characterNameText;
        [SerializeField] string animationChatStringKey = "CHAT";
        [SerializeField] Animator playerChatAnimator;
        [SerializeField] Animator otherChatAnimator;
        [SerializeField] Image playerSprite;
        [SerializeField] Image otherSprite;
        static Dialogue instance;
        [SerializeField] GameObject visibilityAnchor;
        [SerializeField] AudioSource speechPlayer;
        Coroutine activeDialogueRoutine;
        [SerializeField] InputActionReference SkipDialogueKey;
        static float unscaledDialogueStartTime;
        static bool ContinuePressedOrHeldForALongTime
        {
            get
            {
                if (instance is Dialogue d)
                {
                    bool stalled = unscaledDialogueStartTime + HOLD_THRESHOLD >= Time.unscaledTime;
                    return d.SkipDialogueKey.JustPressed() ||
                         (!stalled && d.SkipDialogueKey.PressedLongerThan(HOLD_THRESHOLD));
                }
                return false;
            }
        }
        public static bool IsRunning
        {
            get
            {
                return RinHelper.ValidGameObjects(instance) && RinHelper.ValidGameObjects(instance.visibilityAnchor) && instance.visibilityAnchor.activeInHierarchy;
            }
        }
        private void Awake()
        {
            instance = this;
            SetBoxVisibility(false);
        }
        private static void SetBoxVisibility(bool state)
        {
            instance.visibilityAnchor.SetActive(state);
        }
    }
}
