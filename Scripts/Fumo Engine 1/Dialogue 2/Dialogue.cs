using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
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
                {//
                    return ContainedMessage.OwoSpeak();
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
            Debug.Log("TT:" + letterCount.ToString() + " : ");
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

            bool isPlayer = PlayerCharacter.characterName == character.characterName;
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
            yield return delay.WaitForSeconds(false);
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
        #region Run Dialogue
        static readonly HashSet<char> ExcludedPunctuation = new()
{
    '\'', '"', '‘', '’', '“', '”', ','
};
        static IEnumerator RunStack(DialogueStackSO stack)
        {
            float continueStallTime = Time.unscaledTime + 1f;
            bool StalledContinue()
            {
                return Time.unscaledTime < continueStallTime;
            }
            DialogueCharacterSO resultCharacter;
            void IncrementSpeak(char toAdd)
            {
                SpeakValue += toAdd.GetHashCode();
                wordCharCount++;
                if (wordCharCount >= CHAR_COUNT_TO_SPEAK)
                {
                    EndWord();
                }
            }
            void EndWord()
            {
                if (instance == null)
                {
                    return;
                }
                bool foundCharacter = false;
                foundCharacter = resultCharacter != null;
                if (!foundCharacter)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("Missing Character to jiggle");
#endif
                    return;
                }
                if (SpeakValue > 0f)
                {
                    resultCharacter.Speak(instance.speechPlayer, SpeakValue);
                    Jiggle(resultCharacter);
                }
                SpeakValue = 0;
                wordCharCount = 0;
            }
            bool isPauseChar(char c) => (char.IsSymbol(c) || char.IsWhiteSpace(c));
            foreach (var d in stack.DialogueParts)
            {
                continueStallTime = Time.unscaledTime + 0.1f;
                stack.TryGetCharacter(d.CharacterName, out resultCharacter);
                Jiggle(resultCharacter);
                if (!string.IsNullOrWhiteSpace(d.Command))
                {
                    if (ShmupCommands.TryRun(d.Command))
                    {
                        continue;
                    }
                }
                int currentLetterIndex = 0;
                string message = d.ProcessedMessage;
                bool isDone = false;
                string nameOverride = "";
                float speakingWait = Time.unscaledTime + 0.05f;
                Dialogue.SetTextMessage(d, message, nameOverride);
                instance.dialogueText.ForceMeshUpdate();
                ResetSpeech();




                while (!isDone && currentLetterIndex < message.Length)
                {
                    while (GeneralManager.IsPaused)
                    {
                        yield return null;
                    }
                    while (Time.unscaledTime < speakingWait)
                    {
                        if (!GeneralManager.IsPaused && ContinuePressedOrHeldForALongTime && !StalledContinue())
                        {
                            Dialogue.UpdateText(message.Length + 1, out isDone);
                            currentLetterIndex = message.Length - 1;
                            speakingWait = 0f;
                        }
                        yield return null;
                    }
                    Dialogue.UpdateText(currentLetterIndex + 1, out isDone);
                    bool isSpoken = true;
                    char currentChar = message[currentLetterIndex];

                    if (char.IsPunctuation(currentChar) && !currentChar.RegexChar(ExcludedPunctuation))
                    {
                        speakingWait = Time.unscaledTime + 0.25f;
                        isSpoken = false;
                        EndWord();
                    }
                    else if (isPauseChar(currentChar))
                    {
                        speakingWait = Time.unscaledTime + 0.05f;
                        isSpoken = false;
                        EndWord();
                    }
                    else
                    {
                        speakingWait = Time.unscaledTime + 0.015f;
                    }
                    if (isSpoken)
                    {
                        IncrementSpeak(currentChar);
                    }
                    currentLetterIndex++;
                }
                EndWord();
                speakingWait = Time.unscaledTime + 999999f;
                IEnumerator wait = new WaitForContinueOrTime(5f);
                while (wait.MoveNext())
                {
                    yield return null;
                }
            }
            yield return null;
            yield return null;
            if (instance != null) instance.activeDialogueRoutine = null;
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
        static bool ContinuePressedOrHeldForALongTime
        {
            get
            {
                return ShmupInput.SkipDialogueJustPressed || (ShmupInput.SkipDialoguePressedLongerThan(0.85f));
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
