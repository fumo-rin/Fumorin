using RinCore;
using UnityEngine;

namespace rinCore
{
    public class PauseMenuPopupUI : MonoBehaviour
    {
        [SerializeField] GameObject pauseNest;
        private void Start()
        {
            GeneralManager.WhenPauseToggle += SetPauseVisibility;
            bool show = GeneralManager.IsPaused;
            SetPauseVisibility(show);
        }
        private void OnDestroy()
        {
            GeneralManager.WhenPauseToggle -= SetPauseVisibility;
        }
        private void SetPauseVisibility(bool state)
        {
            pauseNest.SetActive(state);
        }
    }
}
