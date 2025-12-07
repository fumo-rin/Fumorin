using RinCore;
using RinCore;
using RinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RinCore
{
    #region Dialogue Part & Collection
    public partial class Dialogue
    {
        [System.Serializable]
        public struct DialoguePart
        {
            [TextArea(1, 10)]
            public string ContainedMessage;
            public string CharacterName;
            public string Command;
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
        [SerializeField] int charCountToSpeak = 5;
        private static void ResetSpeech()
        {
            SpeakValue = 0;
            wordCharCount = 0;
        }
        private static void IncrementSpeak(char toAdd, DialoguePart d)
        {
            SpeakValue += toAdd.GetHashCode();
            wordCharCount++;
            if (wordCharCount >= instance.charCountToSpeak)
            {
                EndWord(d);
            }
        }
        private static void EndWord(DialoguePart d)
        {
            if (instance == null)
            {
                return;
            }
            bool foundCharacter = false;
            if (instance.loadedCharacters.TryGetCharacter(d.CharacterName, out DialogueCharacterSO.Character character))
            {
                foundCharacter = true;
            }
            else if (instance.loadedCharacters.TryGetSecondaryCharacter(d.CharacterName, out character))
            {
                foundCharacter = true;
            }
            if (!foundCharacter)
            {
                return;
            }
            if (SpeakValue > 0f)
            {
                if (character.GetSpeech(SpeakValue, ref instance.speechPlayer, out AudioClip result))
                {
                    SpeakFunny(instance.speechPlayer, result);
                }
                Jiggle(character);
            }
            SpeakValue = 0;
            wordCharCount = 0;
        }
        private static void SpeakFunny(AudioSource s, AudioClip clip)
        {
            s.clip = clip;
            s.Play();
        }
    }
    #endregion
    #region Load & Add Dialogue
    public partial class Dialogue
    {
        public static void LoadDialogue(DialogueCollection newDialogueStack, Action whenDialogueEnd = null)
        {
            Stop();
            foreach (var item in newDialogueStack.parts)
            {
                AddDialogue(new(item));
            }
            Dialogue.SetContinuePressedStall(1f);
            instance.activeDialogue = instance.StartCoroutine(RunDialogue(0f, whenDialogueEnd));
        }
        static void AddDialogue(DialogueStackEntry entry)
        {
            DialogueStack.Add(entry);
        }
    }
    #endregion
    #region Set Text
    public partial class Dialogue
    {
        private static void SetTextMessage(DialoguePart p, string nameOverride = "")
        {
            instance.dialogueText.maxVisibleCharacters = 0;
            instance.dialogueText.text = p.ContainedMessage;
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
                    if (!GeneralManager.IsPaused && ContinuePressed)
                    {
                        yield break;
                    }
                    yield return null;
                }
            }
        }
    }
    #endregion
    #region Dialogue Stack Entry
    public partial class Dialogue
    {
        static readonly HashSet<char> ExcludedPunctuation = new()
{
    '\'', '"', '‘', '’', '“', '”', ','
};
        public class DialogueStackEntry : IEnumerator
        {
            public DialoguePart dialoguePart;
            int currentLetter;
            IEnumerator enumerator;
            public DialogueStackEntry(DialoguePart d)
            {
                dialoguePart = d;
                currentLetter = 0;
                enumerator = WaitForDialogueAndContinue(d);
            }
            public IEnumerator RunDialogue(DialoguePart d)
            {
                enumerator = WaitForDialogueAndContinue(d);
                return this;
            }
            IEnumerator WaitForDialogueAndContinue(DialoguePart d)
            {
                if (!string.IsNullOrWhiteSpace(d.Command))
                {
                    ShmupCommands.TryRun(d.Command);
                    yield break;
                }
                if (dialoguePart.ContainedMessage.Length == 0)
                {
                    yield break;
                }
                bool isPauseChar(char c) => (char.IsSymbol(c) || char.IsWhiteSpace(c));
                float messageWait = 0f;
                float continueSkipWait = 0f;
                bool isDone = false;
                string nameOverride = "";
                bool loadedChar = instance.loadedCharacters.TryGetCharacter(d.CharacterName, out var jiggleChar);
                if (!loadedChar)
                {
                    //Try Load From Secondary
                    loadedChar = instance.loadedCharacters.TryGetSecondaryCharacter(d.CharacterName, out jiggleChar);
                }
                if (loadedChar)
                {
                    nameOverride = jiggleChar.characterName;
                }
                else
                {
                    Debug.LogWarning("Bad");
                }
                Dialogue.SetTextMessage(dialoguePart, nameOverride);
                if (loadedChar)
                {
                    Jiggle(jiggleChar);
                }
                ResetSpeech();
                while (!isDone && currentLetter < dialoguePart.ContainedMessage.Length)
                {
                    while (GeneralManager.IsPaused)
                    {
                        yield return null;
                    }
                    while (Time.unscaledTime < messageWait)
                    {
                        if (!GeneralManager.IsPaused && ContinuePressed)
                        {
                            Dialogue.UpdateText(dialoguePart.ContainedMessage.Length + 1, out isDone);
                            currentLetter = dialoguePart.ContainedMessage.Length - 1;
                            messageWait = 0f;
                            continueSkipWait = Time.unscaledTime + 0.033f;
                            while (Time.unscaledTime < continueSkipWait)
                            {
                                yield return null;
                            }
                            yield return null;
                        }
                        yield return null;
                    }
                    Dialogue.UpdateText(currentLetter + 1, out isDone);
                    bool isSpoken = true;
                    char currentChar = dialoguePart.ContainedMessage[currentLetter];

                    if (char.IsPunctuation(currentChar) && !currentChar.RegexChar(ExcludedPunctuation))
                    {
                        messageWait = Time.unscaledTime + 0.25f;
                        isSpoken = false;
                        EndWord(d);
                    }
                    else if (isPauseChar(currentChar))
                    {
                        messageWait = Time.unscaledTime + 0.05f;
                        isSpoken = false;
                        EndWord(d);
                    }
                    else
                    {
                        messageWait = Time.unscaledTime + 0.015f;
                    }
                    if (isSpoken)
                    {
                        IncrementSpeak(currentChar, d);
                    }
                    currentLetter++;
                }
                EndWord(d);
                messageWait = Time.unscaledTime + 999999f;
                IEnumerator wait = new WaitForContinueOrTime(5f);
                while (wait.MoveNext())
                {
                    yield return null;
                }
                yield return null;
                yield return null;
            }
            public object Current => MoveNext();
            public bool MoveNext() => enumerator.MoveNext();
            public void Reset() => enumerator.Reset();
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
        private static void Jiggle(DialogueCharacterSO.Character character)
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

            if (PlayerCharacter.character.characterName == character.characterName)
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
            IEnumerator CO_JiggleSprite(Image image, DialogueCharacterSO.Character c)
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
    public partial class Dialogue : MonoBehaviour
    {
        [Initialize(100)]
        private static void Reinitialize()
        {
            ContinuePressedStallTimeEnd = 0f;
        }
        static float ContinuePressedStallTimeEnd;
        static DialogueCharacterSO PlayerCharacter;
        [SerializeField] TMP_Text dialogueText, characterNameText;
        [SerializeField] string animationChatStringKey = "CHAT";
        [SerializeField] Animator playerChatAnimator;
        [SerializeField] Animator otherChatAnimator;
        [SerializeField] Image playerSprite;
        [SerializeField] Image otherSprite;
        static Dialogue instance;
        static List<DialogueStackEntry> DialogueStack = new();
        [SerializeField] GameObject visibilityAnchor;
        [SerializeField] AudioSource speechPlayer;
        [SerializeField] DialogueCharacterCollectionSO loadedCharacters;
        Coroutine activeDialogue;
        public static void SetContinuePressedStall(float delay) => ContinuePressedStallTimeEnd = Time.unscaledTime + delay; 
        static bool ContinuePressed
        {
            get
            {
                return ShmupInput.SkipDialogueJustPressed || (ShmupInput.SkipDialoguePressedLongerThan(0.85f) && Time.unscaledTime >= ContinuePressedStallTimeEnd);
            }
        }
        public static bool IsRunning => DialogueStack != null && DialogueStack.Count > 0 && instance.visibilityAnchor.activeInHierarchy;
        public static WaitUntil WaitUntilNoDialogue => new(() => DialogueStack == null || DialogueStack.Count <= 0);
        private void Awake()
        {
            instance = this;
            if (DialogueStack == null)
            {
                DialogueStack = new List<DialogueStackEntry>();
            }
            SetBoxVisibility(false);
        }
        public static void RunThisWhenPlayerRespawns()
        {
            Dialogue.SetContinuePressedStall(0f);
        }
        private static void SetBoxVisibility(bool state)
        {
            instance.visibilityAnchor.SetActive(state);
        }
        private static IEnumerator RunDialogue(float delay, Action whenDialogueEnd)
        {
            yield return delay.WaitForSeconds(false);
            SetBoxVisibility(true);
            instance.playerSprite.enabled = false;
            instance.otherSprite.enabled = false;
            if (PlayerCharacter != null) Jiggle(PlayerCharacter.character);
            foreach (DialogueStackEntry entry in DialogueStack)
            {
                yield return entry.RunDialogue(entry.dialoguePart);
            }
            whenDialogueEnd?.Invoke();
            SetBoxVisibility(false);
            DialogueStack.Clear();
        }
        public static void Stop()
        {
            if (instance == null)
            {
                return;
            }
            if (instance.activeDialogue != null)
            {
                instance.StopCoroutine(instance.activeDialogue);
            }
            SetBoxVisibility(false);
            DialogueStack.Clear();
        }
    }
}
