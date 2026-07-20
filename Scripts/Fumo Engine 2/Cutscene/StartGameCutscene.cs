using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    public class StartGameCutscene : MonoBehaviour
    {
        [System.Serializable]
        public class CutsceneItem
        {
            public Sprite cutsceneImage;
            public string name => cutsceneImage.name;
            public string textDisplay;
            public GameXYTextDisplay.textPacket textPacket = new()
            {
                a01 = new(0.2f, 0.3f),
                b01 = new(0.8f, 0.5f),
                color = ColorHelper.White,
                duration = 3.25f,
                fadeIn = 0.5f,
                fadeOut = 0.5f,
                fontSize = 22f,
                horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center,
                verticalAlignment = TMPro.VerticalAlignmentOptions.Top,
            };
        }
        [SerializeField] MusicWrapper cutsceneMusic;
        [SerializeField] Image cutsceneViewer;
        [SerializeField] List<CutsceneItem> items = new();
        [SerializeField] ScenePairSO nextScene;
        [SerializeField] Button skipButton;
        bool skipped;
        [SerializeField] float initialDelay = 3f;
        private void Awake()
        {
            cutsceneViewer.sprite = null;
            skipped = false;
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() => skipped = true);
        }
        private void OnDestroy()
        {
            skipButton.onClick.RemoveAllListeners();
        }
        private void Start()
        {
            IEnumerator CO_Run()
            {
                bool first = true;
                yield return initialDelay;
                cutsceneMusic.Play();
                foreach (var item in items)
                {
                    skipped = false;
                    bool wasFirst = first;
                    first = false;
                    cutsceneViewer.sprite = item.cutsceneImage;
                    GameXYTextDisplay.CreateText(item.textDisplay, item.textPacket);

                    float endTime = Time.time + item.textPacket.DurationWithFade;

                    yield return new WaitUntil(() =>
                    skipped || Time.time >= endTime);
                }
                SceneLoader.LoadScenePair(nextScene);
            }
            StartCoroutine(CO_Run());
        }
    }
}
