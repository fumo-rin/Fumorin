using UnityEngine;

namespace rinCore
{
    public class StageMusic : MonoBehaviour
    {
        [SerializeField] MusicWrapper music;
        private void Start()
        {
            music.Play();
        }
    }
}
