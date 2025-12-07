using UnityEngine;

namespace Fumorin
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
