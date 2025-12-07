using FumoCore.Tools;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace Fumorin
{
    #region Editor Script
#if UNITY_EDITOR
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using UnityEditor.VersionControl;
    using System.Collections.Generic;

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
            newSO.SetDialogueTextAsset(selected);
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
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (dialogueTextFile == null)
            {
                return;
            }
            SetDialogueTextAsset(dialogueTextFile);
        }
        private void Awake()
        {
            if (dialogueTextFile == null)
            {
                return;
            }
            SetDialogueTextAsset(dialogueTextFile);
        }
        public void SetDialogueTextAsset(TextAsset asset)
        {
            dialogueTextFile = asset;
            LoadDialogueFromTextAsset();
            this.Dirty();
        }
        public void Editor_RefreshAndSave()
        {
            SetDialogueTextAsset(dialogueTextFile);
            this.SetDirtyAndSave();
        }
#endif
        [field: SerializeField] public TextAsset dialogueTextFile { get; private set; }
        [SerializeField] private DialogueCharacterCollectionSO characterLookup;
        [SerializeField] Dialogue.DialogueCollection containedDialogue;
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
            Dialogue.LoadDialogue(containedDialogue, WhenEndDialogue);
            wait = Dialogue.WaitUntilNoDialogue;
        }
        public void LoadDialogueFromTextAsset()
        {
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
                if (characterLookup != null)
                {
                    characterLookup.AddCharacter(character);
                }
                if (!string.IsNullOrEmpty(character) && !string.IsNullOrEmpty(message))
                {
                    newParts.Add(new Dialogue.DialoguePart
                    {
                        CharacterName = character,
                        ContainedMessage = message
                    });
                }
            }
            containedDialogue = new(newParts);
            Debug.Log("Dialogue loaded from TextAsset.");
            this.Dirty();
        }
    }
}
