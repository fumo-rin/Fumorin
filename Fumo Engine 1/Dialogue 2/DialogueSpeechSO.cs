using FumoCore.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Fumorin
{
#if UNITY_EDITOR
    [CustomEditor(typeof(DialogueSpeechSO))]
    public class DialogueSpeechEditor : Editor
    {
        private DialogueSpeechSO dialogueWordsSO;
        private SerializedProperty speechClipsProp;

        private void OnEnable()
        {
            dialogueWordsSO = (DialogueSpeechSO)target;
            speechClipsProp = serializedObject.FindProperty("speechClips");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw default inspector
            DrawDefaultInspector();

            GUILayout.Space(10);
            GUILayout.Label("Drag AudioClips Below to Add to Speech Clips", EditorStyles.boldLabel);
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 150.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop AudioClips Here", EditorStyles.helpBox);

            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropArea.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is AudioClip audioClip)
                            {
                                if (!dialogueWordsSO.SpeechClipsContains(audioClip))
                                {
                                    speechClipsProp.InsertArrayElementAtIndex(speechClipsProp.arraySize);
                                    speechClipsProp.GetArrayElementAtIndex(speechClipsProp.arraySize - 1).objectReferenceValue = audioClip;
                                }
                            }
                        }

                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(dialogueWordsSO);
                    }
                    Event.current.Use();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
    [CreateAssetMenu(fileName = "New Dialogue Words", menuName = "Eientei/Dialogue 2/Dialogue Audio Word Set")]
    public class DialogueSpeechSO : ScriptableObject
    {
        public bool SpeechClipsContains(AudioClip c) => speechClips != null && speechClips.Contains(c);
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
}
