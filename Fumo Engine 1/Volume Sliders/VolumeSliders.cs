using FumoCore.Tools;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Fumorin
{
    public class VolumeSliders : MonoBehaviour
    {
        [ContextMenu("Reset Player Prefs for music")]
        public void ResetPrefs()
        {
            PlayerPrefs.DeleteKey("EffectsSFX");
            PlayerPrefs.DeleteKey("MusicSFX");
            PlayerPrefs.DeleteKey("DialogueSFX");
            StartSliders();
        }
        [SerializeField] Slider effectsSlider, musicSlider, dialogueSlider;
        [SerializeField] AudioMixer[] effectsMixers, musicMixers, dialogueMixers;
        static float StoredVolume = -7f;
        public static void FetchAndApplySettings(AudioMixer[] effects, AudioMixer[] music, AudioMixer[] dialogue)
        {
            VolumeSliders v = new GameObject("Volume Fetcher").AddComponent<VolumeSliders>();
            v.TryGetSavedValue("EffectsSFX", out float effectsVolume);
            v.TryGetSavedValue("MusicSFX", out float musicVolume);
            v.TryGetSavedValue("DialogueSFX", out float dialogueVolume);
            v.SetMixers(music, musicVolume);
            v.SetMixers(effects, effectsVolume);
            v.SetMixers(dialogue, dialogueVolume);
            Destroy(v.gameObject);
        }
        private bool TryGetSavedValue(string key, out float value)
        {
            value = StoredVolume;
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetFloat(key);
                return true;
            }
            return false;
        }
        private void OnEnable()
        {
            if (effectsSlider) effectsSlider.onValueChanged.AddListener(delegate { ReadEffectsSlider(); });
            if (musicSlider) musicSlider.onValueChanged.AddListener(delegate { ReadMusicSlider(); });
            if (dialogueSlider) dialogueSlider.onValueChanged.AddListener(delegate { ReadDialogueSlider(); });
        }
        private void Awake()
        {
            StartSliders();
        }
        private void StartSliders()
        {
            TryGetSavedValue("EffectsSFX", out float effectsVolume);

            if (effectsSlider) effectsSlider.SetValues(effectsVolume * 0.25f, 0f, -40f * 0.25f);
            if (effectsMixers != null) SetMixers(effectsMixers, effectsVolume);

            TryGetSavedValue("MusicSFX", out float musicVolume);

            if (musicSlider) musicSlider.SetValues(musicVolume * 0.25f, 0f, -40f * 0.25f);
            if (musicMixers != null) SetMixers(musicMixers, musicVolume);

            TryGetSavedValue("DialogueSFX", out float dialogueVolume);

            if (dialogueSlider) dialogueSlider.SetValues(dialogueVolume * 0.25f, 0f, -40f * 0.25f);
            if (dialogueMixers != null) SetMixers(dialogueMixers, dialogueVolume);
        }
        private void OnDisable()
        {
            if (effectsSlider) effectsSlider.onValueChanged.RemoveListener(delegate { ReadEffectsSlider(); });
            if (musicSlider) musicSlider.onValueChanged.RemoveListener(delegate { ReadMusicSlider(); });
            if (dialogueSlider) dialogueSlider.onValueChanged.RemoveListener(delegate { ReadDialogueSlider(); });
        }
        private void StoreValue(string key, float value)
        {
            StoredVolume = value;
            PlayerPrefs.SetFloat(key, value);
        }
        private void SetMixers(AudioMixer[] mixers, float value)
        {
            if (value < -39.5f)
            {
                value = -80f;
            }
            value = (value - 5f).Clamp(-80f, 30f);
            foreach (var item in mixers)
            {
                item.SetFloat("Volume", value);
            }
        }
        public void ReadEffectsSlider()
        {
            float value = effectsSlider.value * 4f;
            StoreValue("EffectsSFX", value);
            SetMixers(effectsMixers, value);
        }
        public void ReadDialogueSlider()
        {
            float value = dialogueSlider.value * 4f;
            StoreValue("DialogueSFX", value);
            SetMixers(dialogueMixers, value);
        }
        public void ReadMusicSlider()
        {
            float value = musicSlider.value * 4f;
            StoreValue("MusicSFX", value);
            SetMixers(musicMixers, value);
        }
    }
}
