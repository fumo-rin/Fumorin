using rinCore;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace rinCore
{
    public class VolumeSliders : MonoBehaviour
    {
        [SerializeField] Slider effectsSlider, musicSlider, dialogueSlider;
        private void OnEnable()
        {
            effectsSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.RemoveAllListeners();
            dialogueSlider.onValueChanged.RemoveAllListeners();

            effectsSlider.SetValueWithoutNotify(AudioEngine.EffectsVolume * 10f);
            musicSlider.SetValueWithoutNotify(AudioEngine.MusicVolume * 10f);
            dialogueSlider.SetValueWithoutNotify(AudioEngine.DialogueVolume * 10f);

            effectsSlider.onValueChanged.AddListener(v =>
                AudioEngine.MixerInstance.SetEffectsVolume(v / 10f));

            musicSlider.onValueChanged.AddListener(v =>
                AudioEngine.MixerInstance.SetMusicVolume(v / 10f));

            dialogueSlider.onValueChanged.AddListener(v =>
                AudioEngine.MixerInstance.SetDialogueVolume(v / 10f));
        }
        private void OnDisable()
        {
            effectsSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.RemoveAllListeners();
            dialogueSlider.onValueChanged.RemoveAllListeners();
        }
    }
}