using RinCore;
using UnityEngine;
using UnityEngine.UI;

namespace RinCore
{
    [RequireComponent(typeof(Button))]
    public class FullScreen : MonoBehaviour
    {
        public static bool Fullscreen => Screen.fullScreen;
        Button b;
        private void Awake()
        {
            b = GetComponent<Button>();
            if (GeneralManager.IsWebGL)
            {
                b.interactable = false;
            }
        }
        private void Start()
        {
            b.AddClickAction(ToggleFullscreen);
        }
        private void OnDestroy()
        {
            b.RemoveClickAction(ToggleFullscreen);
        }
        private static void ToggleFullscreen()
        {
            bool newFullscreen = !Fullscreen;
            if (newFullscreen)
            {
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, true);
            }
            else
            {
                Screen.SetResolution(1280, 960, false);
            }
        }
    }
}
