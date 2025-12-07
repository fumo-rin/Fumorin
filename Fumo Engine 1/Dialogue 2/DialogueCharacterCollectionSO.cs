using FumoCore.Tools;
using System.Collections.Generic;
using UnityEngine;

namespace Fumorin
{
    [CreateAssetMenu(fileName = "New Dialogue Characters", menuName = "Eientei/Dialogue 2/Character Lookup")]
    public class DialogueCharacterCollectionSO : ScriptableObject
    {
        [System.Serializable]
        public struct CharacterEntry
        {
            public string characterLookupName;
            public DialogueCharacterSO characterReference;
        }
        [SerializeField] List<DialogueCharacterCollectionSO> secondaryCharacterLists = new();
        [SerializeField] List<CharacterEntry> characters;
        public bool TryGetCharacter(string name, out DialogueCharacterSO.Character c, bool withSecondary = false)
        {
            if (Dialogue.TryGetCharacterOverride(name, out DialogueCharacterSO character))
            {
                c = character.character;
                return true;
            }
            if (HasCharacterName(in characters, name, out c) == FindCharacterResult.Success)
            {
                return true;
            }
            if (withSecondary)
            {
                return TryGetSecondaryCharacter(name, out c);
            }
            c = default;
            return false;
        }
        public bool TryGetSecondaryCharacter(string name, out DialogueCharacterSO.Character c)
        {
            foreach (var character in secondaryCharacterLists)
            {
                if (HasCharacterName(in character.characters, name, out c) == FindCharacterResult.Success)
                {
                    return true;
                }
            }
            c = default;
            return false;
        }
        public void AddCharacter(string c)
        {
            if (HasCharacterName(in characters, c, out _) != FindCharacterResult.NoCharacter)
            {
                return;
            }
            characters.Add(new()
            {
                characterLookupName = c,
                characterReference = null
            });
            this.Dirty();
        }
        public enum FindCharacterResult
        {
            NoCharacter,
            NoReference,
            Success
        }
        private FindCharacterResult HasCharacterName(in List<CharacterEntry> characters, string name, out DialogueCharacterSO.Character c)
        {
            c = default;
            FindCharacterResult result = FindCharacterResult.NoCharacter;
            foreach (CharacterEntry character in characters)
            {
                if (character.characterLookupName == name)
                {
                    if (character.characterReference != null)
                    {
                        c = character.characterReference.character;
                        return result = FindCharacterResult.Success;
                    }
                    return result = FindCharacterResult.NoReference;
                }
            }
            return result;
        }
        private FindCharacterResult TryFindInCharacters(List<DialogueCharacterCollectionSO> characters, string name, out DialogueCharacterSO.Character c)
        {
            c = default;
            FindCharacterResult result = FindCharacterResult.NoCharacter;
            if (result == FindCharacterResult.NoCharacter && secondaryCharacterLists != null && secondaryCharacterLists.Count > 0)
            {
                foreach (var item in characters)
                {
                    result = item.HasCharacterName(item.characters, name, out c);
                    if (result != FindCharacterResult.NoCharacter)
                    {
                        return result;
                    }
                }
            }
            return result;
        }
    }
}