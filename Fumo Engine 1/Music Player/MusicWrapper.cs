using FumoCore.Tools;
using UnityEngine;
using UnityEditor;
using System.IO;


namespace Fumorin
{
    #region Music Clip Create
    using UnityEditor;
    using System.IO;

#if UNITY_EDITOR
    public class AudioClipTools
    {
        [MenuItem("Assets/Create Music Wrapper From AudioClip", true)]
        private static bool ValidateAudioClip()
        {
            return Selection.activeObject is AudioClip;
        }

        [MenuItem("Assets/Create Music Wrapper From AudioClip")]
        private static void CreateACWrapperFromSelected()
        {
            AudioClip clip = Selection.activeObject as AudioClip;
            if (clip == null)
            {
                Debug.LogWarning("Selected object is not an AudioClip.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(clip);
            string directory = Path.GetDirectoryName(path);
            string filename = Path.GetFileNameWithoutExtension(path);

            var wrapper = ScriptableObject.CreateInstance<MusicWrapper>();
            wrapper.CreateFrom(clip);

            string newAssetPath = Path.Combine(directory, $"{Application.productName} MusicWrapper_{filename}.asset");
            AssetDatabase.CreateAsset(wrapper, newAssetPath);
            wrapper.Dirty();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = wrapper;
        }
    }
#endif
    #endregion
    [CreateAssetMenu(menuName = "Bremsengine/MusicWrapper")]
    [System.Serializable]
    public class MusicWrapper : ScriptableObject
    {
        public float bpm = 120f;
        public float BeatLength => 60f / bpm.Max(1f);
        public static implicit operator AudioClip(MusicWrapper mw) => mw == null ? null : mw.musicClip;
        public static implicit operator float(MusicWrapper mw) => mw == null ? 0f : mw.musicVolume;
        public string TrackName = Helper.DefaultName;
        public AudioClip musicClip;
        public float musicVolume => clipVolume * MusicPlayer.GlobalVolume;
        public float musicLength => musicClip != null ? musicClip.length : 0f;
        [SerializeField] float clipVolume = 0.7f;
        [field: SerializeField] public bool dontReplaceSelf { get; private set; } = true;
        private void OnValidate()
        {
            this.FindStringError(nameof(TrackName), TrackName);
        }
        public void CreateFrom(AudioClip clip)
        {
            this.musicClip = clip;
            TrackName = clip.name;
            clipVolume = 0.7f;
            dontReplaceSelf = true;
        }
        public void Play()
        {
            PlayMusic(this);
        }
        private static void PlayMusic(MusicWrapper p)
        {
            if (p != null) MusicPlayer.PlayMusicWrapper(p);
        }
    }
}