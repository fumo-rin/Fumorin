using rinCore;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace rinCore
{
    public class MusicRoom : MonoBehaviour
    {
        [SerializeField] Button buttonTemplate;
        [SerializeField] MusicRoomTracklist trackList;
        protected virtual List<MusicWrapper> Tracklist()
        {
            List<MusicWrapper> result = new();
            if (trackList != null && trackList.TrackList != null && trackList.TrackList.Count > 0)
            {
                foreach (var track in trackList.TrackList)
                {
                    result.AddIfDoesntExist(track);
                }
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
                    selected = CreateButton(buttonTemplate, track).gameObject.Select_WithEventSystem();
                    continue;
                }
                CreateButton(buttonTemplate, track);
            }

            buttonTemplate.gameObject.SetActive(false);
        }
        private Button CreateButton(Button prefab, MusicWrapper music)
        {
            Button b = Instantiate(prefab, buttonTemplate.transform.parent);
            b.BindSingleAction(() => music.Play());
            TMP_Text t = b.GetComponentInChildren<TMP_Text>();
            if (t != null)
            {
                t.text = music.TrackName;
            }
            b.gameObject.SetActive(true);
            return b;
        }
    }
}
