using FumoCore.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Fumorin
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
