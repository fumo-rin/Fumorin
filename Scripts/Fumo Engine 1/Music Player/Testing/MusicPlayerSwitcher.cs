using System.Collections;
using UnityEngine;

namespace RinCore
{
    public class MusicPlayerSwitcher : MonoBehaviour
    {
        [SerializeField] MusicPlayer player;
        [SerializeField] MusicWrapper music1, music2;
        IEnumerator Start()
        {
            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForSeconds(1f);
                music1.Play();
                yield return new WaitForSeconds(1f);
                music2.Play();
            }
        }
    }
}
