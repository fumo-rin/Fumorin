using UnityEngine;

namespace RinCore
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
