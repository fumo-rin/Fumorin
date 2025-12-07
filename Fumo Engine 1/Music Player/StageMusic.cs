using UnityEngine;

namespace Fumorin
{
    public class StageMusic : MonoBehaviour
    {
        [SerializeField] MusicWrapper music;
        private void Start()
        {
            if (MusicPlayer.FetchPlaymode() == MusicPlayer.PlayMode.Loop)
            {
                music.Play();
            }
        }
    }
}
