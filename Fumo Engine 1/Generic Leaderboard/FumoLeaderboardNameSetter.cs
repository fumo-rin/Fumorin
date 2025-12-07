using UnityEngine;
using TMPro;
using RinCore.UGS;
using RinCore;
namespace RinCore
{
    [RequireComponent(typeof(TMP_InputField))]
    public class FumoLeaderboardNameSetter : MonoBehaviour
    {
        TMP_InputField field;
        const string saveKey = "Leaderboard Player Name";
        static string storedName;
        bool wasChanged;
        private void Awake()
        {
            field = GetComponent<TMP_InputField>();
        }
        private void Start()
        {
            field.onEndEdit.RemoveAllListeners();
            field.onEndEdit.AddListener(WhenUpdateField);
            string previousName = storedName;
            bool loadedName = PersistentJSON.TryLoad(out storedName, saveKey);
            field.text = loadedName ? storedName : "Nanashi";
            WhenUpdateField(field.text);
            wasChanged = loadedName && storedName != previousName;
        }
        private void OnDestroy()
        {
            string nameToSend = storedName;

            if (BadWords.CleanReplaceFunny(storedName.Letterize(), BadWords.BadWordsList, out string clean, field.characterLimit))
            {
                nameToSend = clean;
            }
            _ = UGSInitializer.SetPlayerNameAsync(nameToSend);
        }
        async void SetName(string n)
        {
            if (!wasChanged)
            {
                return;
            }
            wasChanged = false;
            await UGSInitializer.SetPlayerNameAsync(n);
        }
        private void WhenUpdateField(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                s = "Nanashi";
                field.text = s;
            }
            wasChanged = storedName != s;
            storedName = s;
            PersistentJSON.TrySave(s, saveKey);
            Helper.EventSystem_Deselect();
        }
    }
}
