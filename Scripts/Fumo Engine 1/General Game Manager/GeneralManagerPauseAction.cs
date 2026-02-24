using UnityEngine;

namespace rinCore
{
    public class GeneralManagerPauseAction : MonoBehaviour
    {
        public void SetPause(bool state)
        {
            GeneralManager.SetPause(state);
        }
        public void TogglePause()
        {
            SetPause(!GeneralManager.IsPaused);
        }
    }
}
