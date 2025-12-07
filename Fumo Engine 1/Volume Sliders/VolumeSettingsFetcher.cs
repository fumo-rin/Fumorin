using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Fumorin
{
    public class VolumeSettingsFetcher : MonoBehaviour
    {
        [SerializeField] List<AudioMixer> musicMixers = new(), effectsMixers = new(), dialogueMixers = new();
        private void Start()
        {
            VolumeSliders.FetchAndApplySettings(effectsMixers.ToArray(), musicMixers.ToArray(), dialogueMixers.ToArray());
        }
    }
}
