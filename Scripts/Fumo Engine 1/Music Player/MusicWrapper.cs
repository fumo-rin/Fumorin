using RinCore;
using UnityEngine;
using UnityEditor;
using System.IO;


namespace RinCore
{
    #region Music Clip Create
    using UnityEditor;
    using System.IO;
    using System.Collections;

#if UNITY_EDITOR
    public class MusicClipMenuEntry
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
    #region nullable Play Extension
    public static class MusicWrapperNullablePlay
    {
        public static void Play(this MusicWrapper p)
        {
            if (p == null)
                return;
            MusicWrapper.PlayMusic(p);
        }
    }
    #endregion
    [CreateAssetMenu(menuName = "Bremsengine/MusicWrapper")]
    [System.Serializable]
    public class MusicWrapper : ScriptableObject
    {
        public float bpm = 120f;
        public float BeatLength => 60f / bpm.Max(1f);
        public static implicit operator AudioClip(MusicWrapper mw) => mw == null ? null : mw.musicClip;
        public static implicit operator float(MusicWrapper mw) => mw == null ? 0f : mw.musicVolume;
        public string TrackName = RinHelper.DefaultName;
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
        [Initialize(-999)]
        private static void ResetQueue()
        {
            musicPlayNextRoutine = null;
        }
        static Coroutine musicPlayNextRoutine;
        internal static void PlayMusic(MusicWrapper p)
        {
            IEnumerator CO_PlayWhenReady()
            {
                Debug.Log("Playing : " + p.TrackName);
                while (!MusicPlayer.IsReady)
                {
                    Debug.Log("Stalling...");
                    yield return null;
                }
                MusicPlayer.PlayMusicWrapper(p);
                Debug.Log("Now Playing");
                musicPlayNextRoutine = null;
            }
            if (musicPlayNextRoutine != null)
            {
                GlobalCoroutineRunner.StopAllOfKey("Music Play Queue");
            }
            musicPlayNextRoutine = CO_PlayWhenReady().RunRoutine("Music Play Queue");
        }
    }
}