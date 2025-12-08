using RinCore;
using UnityEngine;
using UnityEngine.UI;

namespace RinCore
{
    [RequireComponent(typeof(Button))]
    public class MusicModeButton : MonoBehaviour
    {
        [SerializeField] MusicPlayer.PlayMode mode;
        Button b;
        private void Awake()
        {
            b = GetComponent<Button>();
        }
        private void Start()
        {
            b.BindSingleAction(() => MusicPlayer.SetPlayMode(mode));
        }
        private void OnDestroy()
        {
            b.RemoveAllClickActions();
        }
    }
}
