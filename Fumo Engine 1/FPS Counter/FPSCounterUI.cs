using UnityEngine;
using TMPro;
using FumoCore.Tools;

namespace Fumorin
{
    public class FPSCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text fpsText;
        [SerializeField, Range(0.1f, 1f)] private float updateInterval = 0.5f; // UI update interval

        private float elapsedTime = 0f;
        private int frameCount = 0;
        private float fps;

        private void Update()
        {
            frameCount++;
            elapsedTime += Time.unscaledDeltaTime;

            if (elapsedTime >= updateInterval)
            {
                fps = frameCount / elapsedTime;
                int i = Mathf.RoundToInt(fps).Clamp(0, Application.targetFrameRate);
                fpsText.text = $"FPS: {i}";

                frameCount = 0;
                elapsedTime = 0f;
            }
        }
    }
}