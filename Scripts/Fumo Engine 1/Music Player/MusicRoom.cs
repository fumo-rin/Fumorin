using RinCore;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace RinCore
{
    public class MusicRoom : MonoBehaviour
    {
        [SerializeField] Button musicButton;
        [SerializeField] RectTransform musicContainerTransform;
        [SerializeField] MusicRoomTracklist trackList;
        protected virtual List<MusicWrapper> Tracklist()
        {
            List<MusicWrapper> result = new();
            foreach (var track in trackList.TrackList)
            {
                result.AddIfDoesntExist(track);
            }
            return result;
        }
        private void Awake()
        {
            bool selected = false;
            foreach (var track in Tracklist())
            {
                if (!selected)
                {
                    selected = CreateButton(musicButton, track).gameObject.Select_WithEventSystem();
                    continue;
                }
                CreateButton(musicButton, track);
            }
            musicButton.gameObject.SetActive(false);
        }
        private Button CreateButton(Button prefab, MusicWrapper music)
        {
            Button b = Instantiate(prefab, musicContainerTransform);
            b.BindSingleAction(() => music.Play());
            TMP_Text t = b.GetComponentInChildren<TMP_Text>();
            if (t != null)
            {
                t.text = music.TrackName;
            }
            return b;
        }
    }
}
