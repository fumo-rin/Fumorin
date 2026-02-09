using RinCore;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEditor.VersionControl;
#endif
namespace RinCore
{
    #region Speech
    [System.Serializable]
    public class DialogueSpeechData
    {
        [SerializeField] float volume = 1f;
        [SerializeField] List<AudioClip> speechClips = new();
        public bool GetWord(int hashValue, out AudioClip result)
        {
            result = null;
            if (speechClips.Count <= 1)
            {
                result = speechClips[0];
            }
            else
            {
                result = speechClips[hashValue % speechClips.Count];
            }
            return result != null;
        }
        [SerializeField] Vector2 pitchRange;
        [Range(100, 300)]
        [SerializeField] int pitchSteps;
        public void ApplySettings(int hashValue, ref AudioSource s)
        {
            float pitch = pitchRange.x.LerpUnclamped(pitchRange.y, (hashValue % pitchRange.y));
            s.pitch = pitch;
            s.volume = volume;
        }
    }
    #endregion

    #region Editor Script
#if UNITY_EDITOR

    public static class CreateShmupDialogueFromText
    {
        [MenuItem("Assets/Create/Shmup Dialogue Asset from Text", true)]
        private static bool ValidateCreateDialogue()
        {
            if (!(Selection.activeObject is TextAsset textAsset))
                return false;

            string path = AssetDatabase.GetAssetPath(textAsset);
            string ext = Path.GetExtension(path).ToLowerInvariant();

            return ext == ".txt";
        }
        [MenuItem("Assets/Create/Shmup Dialogue Asset from Text")]
        private static void CreateDialogueAsset()
        {
            var selected = Selection.activeObject as TextAsset;
            if (selected == null)
            {
                Debug.LogWarning("Please select a valid text file.");
                return;
            }
            string selectedPath = AssetDatabase.GetAssetPath(selected);
            string directory = Path.GetDirectoryName(selectedPath);
            string dialogueName = Path.GetFileNameWithoutExtension(selectedPath) + ".asset";
            string dialoguePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, dialogueName));
            var newSO = ScriptableObject.CreateInstance<DialogueStackSO>();
            newSO.name = Path.GetFileNameWithoutExtension(dialoguePath);
            newSO.PopulateFromTextAsset(selected);
            AssetDatabase.CreateAsset(newSO, dialoguePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newSO;
        }
    }
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DialogueStackSO))]
    public class ShmupDialogueSOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Create and Assign New Dialogue Text File"))
            {
                CreateTextFileForDialogue((DialogueStackSO)target);
            }
            if (target is DialogueStackSO d && GUILayout.Button("Refresh And Save"))
            {
                d.Editor_RefreshAndSave();
            }
        }
        private void CreateTextFileForDialogue(DialogueStackSO dialogueSO)
        {
            var assetPath = AssetDatabase.GetAssetPath(dialogueSO);
            var directory = Path.GetDirectoryName(assetPath);
            var filename = Path.GetFileNameWithoutExtension(assetPath) + "_Dialogue.txt";
            var fullPath = Path.Combine(directory, filename);
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            File.WriteAllText(uniquePath, "Narrator: Hello, world!");
            AssetDatabase.Refresh();
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(uniquePath);
            Undo.RecordObject(dialogueSO, "Assign Dialogue TextAsset");
            EditorUtility.SetDirty(dialogueSO);
            AssetDatabase.SaveAssets();
        }
    }
#endif
    #endregion
    [CreateAssetMenu(fileName = "New Dialogue Stack", menuName = "Bremsengine/Dialogue 2/Dialogue Stack")]
    public class DialogueStackSO : ScriptableObject
    {
        #region Editor shiz
        private void EditorValidate()
        {
#if UNITY_EDITOR
            if (dialogueTextFile == null)
            {
                return;
            }
            PopulateFromTextAsset(dialogueTextFile);
#endif
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (dialogueTextFile == null)
            {
                return;
            }
            PopulateFromTextAsset(dialogueTextFile);
        }
        public void Editor_RefreshAndSave()
        {
            PopulateFromTextAsset(dialogueTextFile);
            this.SetDirtyAndSave();
        }
#endif
        #endregion
        private void Awake()
        {
            EditorValidate();
        }
        [System.Serializable]
        public class CharacterEntry
        {
            #region Prop Drawer
#if UNITY_EDITOR
            [CustomPropertyDrawer(typeof(DialogueStackSO.CharacterEntry))]
            public class CharacterEntryDrawer : PropertyDrawer
            {
                private const float Padding = 2f;
                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    EditorGUI.BeginProperty(position, label, property);
                    position = EditorGUI.IndentedRect(position);
                    var nameProp = property.FindPropertyRelative("characterName");
                    var characterProp = property.FindPropertyRelative("character");
                    float half = position.width * 0.45f;
                    float spacing = 6f;
                    var nameRect = new Rect(position.x, position.y, half, EditorGUIUtility.singleLineHeight);
                    var charRect = new Rect(position.x + half + spacing, position.y, position.width - half - spacing, EditorGUIUtility.singleLineHeight);

                    EditorGUI.PropertyField(nameRect, nameProp, GUIContent.none);
                    EditorGUI.PropertyField(charRect, characterProp, GUIContent.none);
                    EditorGUI.EndProperty();
                }
                public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
                {
                    return EditorGUIUtility.singleLineHeight + Padding;
                }
            }
#endif
            #endregion
            public string characterName;
            public DialogueCharacterSO character;
        }
        [SerializeField] List<CharacterEntry> characterList = new();
        Dictionary<string, DialogueCharacterSO> characterLookup;
        [field: SerializeField] public TextAsset dialogueTextFile { get; private set; }
        [SerializeField] Dialogue.DialogueCollection containedDialogue;
        public IEnumerable<Dialogue.DialoguePart> DialogueParts
        {
            get
            {
                foreach (var item in containedDialogue.parts)
                {
                    yield return item;
                }
            }
        }
        private void FillFromList(List<CharacterEntry> entries, Dictionary<string, DialogueCharacterSO> lookup)
        {
            foreach (var item in entries)
            {
                if (item.character == null)
                {
                    Debug.LogWarning("Missing Character");
                    continue;
                }
                if (lookup.ContainsKey(item.characterName))
                {
                    continue;
                }
                lookup[item.characterName] = item.character;
            }
        }
        public bool TryGetCharacter(string characterName, out DialogueCharacterSO result)
        {
            result = null;
            if (characterLookup == null || characterLookup.Count <= 0)
            {
                characterLookup = new();
            }
            FillFromList(characterList, characterLookup);

            if (characterLookup == null || characterLookup.Count <= 0)
            {
                Debug.LogError("Bad Character Lookup for : " + name);
                return false;
            }

            characterLookup.TryGetValue(characterName, out result);

            return result != null;
        }
        public bool GetAllCommands(out HashSet<string> commandNames)
        {
            commandNames = null;
            foreach (var item in containedDialogue.parts)
            {
                if (!string.IsNullOrEmpty(item.Command))
                {
                    if (commandNames == null) commandNames = new();
                    commandNames.Add(item.Command);
                }
            }
            return commandNames != null;
        }
        public void StartDialogue(out WaitUntil wait, Action WhenEndDialogue)
        {
            Dialogue.LoadDialogue(this, WhenEndDialogue);
            float minimumTimeWait = Time.time + 0.5f;
            wait = new(() => !Dialogue.IsRunning && Time.time > minimumTimeWait);
        }
        public void PopulateFromTextAsset(TextAsset asset)
        {
            dialogueTextFile = asset;
            List<Dialogue.DialoguePart> newParts = new();
            if (dialogueTextFile == null)
            {
                Debug.LogWarning("Dialogue TextAsset is null.");
                return;
            }

            var lines = dialogueTextFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("."))
                {
                    newParts.Add(new Dialogue.DialoguePart
                    {
                        Command = line.Substring(1).Trim()
                    });
                }
                if (string.IsNullOrWhiteSpace(line) || !line.Contains(":"))
                    continue;

                var split = line.Split(new[] { ':' }, 2);
                var character = split[0].Trim();
                var message = split[1].Trim().Capitalized();
                if (!string.IsNullOrEmpty(character) && !string.IsNullOrEmpty(message))
                {
                    newParts.Add(new Dialogue.DialoguePart(message)
                    {
                        CharacterName = character
                    });
                }
                bool characterExists = false;
                foreach (var item in characterList)
                {
                    if (item != null && item.characterName == character)
                    {
                        characterExists = true;
                        break;
                    }
                }
                if (!characterExists)
                {
                    characterList.Add(new()
                    {
                        characterName = character,
                        character = null
                    });
                }
            }
            containedDialogue = new(newParts);
            Debug.Log("Dialogue loaded from TextAsset.");
            this.Dirty();
        }
    }
}
