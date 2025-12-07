using UnityEngine;

namespace Fumorin
{
    [CreateAssetMenu(fileName ="New Character", menuName = "Eientei/Dialogue 2/Character")]
    public class DialogueCharacterSO : ScriptableObject
    {
        [System.Serializable]
        public struct Character
        {
            public string characterName;
            public Sprite sprite;
            public Sprite talkSprite;
            [SerializeField] DialogueSpeechSO words;
            public bool GetSpeech(int hashValue, ref AudioSource s, out AudioClip result)
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
        public Character character;
    }
}
