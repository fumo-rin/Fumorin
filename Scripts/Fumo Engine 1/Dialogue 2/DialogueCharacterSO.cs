using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using RinCore;
#endif
namespace RinCore
{
    [CreateAssetMenu(fileName = "New Character", menuName = "RinCore/Dialogue 2/Character")]
    public class DialogueCharacterSO : ScriptableObject
    {
#if UNITY_EDITOR

        [CustomEditor(typeof(DialogueCharacterSO))]
        public class DialogueCharacterSOEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                EditorGUILayout.Space(10);
                if (GUILayout.Button("Ping & Select Asset"))
                {
                    DialogueCharacterSO so = (DialogueCharacterSO)target;
                    Selection.activeObject = so;
                    so.EditorPing();
                }
            }
        }
#endif
        public string characterName;
        public Sprite sprite;
        public Sprite talkSprite;
        [SerializeField] DialogueSpeechData words;
        public bool Speak(AudioSource player, int hashValue)
        {
            if (player == null)
                return false;

            bool success = GetSpeech(hashValue, ref player, out AudioClip result);
            if (success)
            {
                player.clip = result;
                player.Play();
            }
            return success;
        }
        private bool GetSpeech(int hashValue, ref AudioSource s, out AudioClip result)
        {
            result = null;
            if (words != null)
            {
                words.ApplySettings(hashValue, ref s);
                words.GetWord(hashValue, out result);
            }
            return result != null;
        }
    }
}
