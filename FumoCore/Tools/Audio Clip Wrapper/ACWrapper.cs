using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace RinCore
{
    #region Static Extension
    public static partial class ACWrapperExtensions
    {
        public static void Play(this ACWrapper a, Vector2 position)
        {
            if (a == null)
                return;
            AudioEngine.PlayWrapper(a, position);
        }
    }
    #endregion
    #region Audio clip Button

#if UNITY_EDITOR
    public partial class AudioClipMenuEntry
    {
        [MenuItem("Assets/Create AC Wrapper from AudioClip", true)]
        private static bool ValidateCreateACWrapper()
        {
            return Selection.activeObject is AudioClip;
        }

        [MenuItem("Assets/Create AC Wrapper from AudioClip")]
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

            var wrapper = ScriptableObject.CreateInstance<ACWrapper>();
            wrapper.name = filename;
            wrapper.CreateFrom(clip);

            string newAssetPath = Path.Combine(directory, $"{filename}_{Application.productName} ACWrapper.asset");
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
#if UNITY_EDITOR
    #region ACWrapper test button
    [CustomEditor(typeof(ACWrapper))]
    public class ACWrapperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ACWrapper wrapper = (ACWrapper)target;

#if UNITY_EDITOR
            EditorGUILayout.Space();
            wrapper.loopInEditor = EditorGUILayout.Toggle("Loop in Editor", wrapper.loopInEditor);

            if (GUILayout.Button("▶ Test Play"))
            {
                ACWrapper.EditorPlay(wrapper, wrapper.loopInEditor);
            }

            if (GUILayout.Button("⏹ Stop All"))
            {
                ACWrapper.EditorStopAll();
            }
            if (GUILayout.Button("Select Asset"))
            {
                wrapper.EditorPing();
            }
#endif
        }
    }
    #endregion
#endif
    [CreateAssetMenu(fileName = "New Sound", menuName = "Core/Audio Clip Wrapper")]
    public partial class ACWrapper : ScriptableObject, ISerializationCallbackReceiver
    {
        private static readonly List<ACWrapper> runtimeRegistry = new();
        private void OnEnable()
        {
            if (!runtimeRegistry.Contains(this))
                runtimeRegistry.Add(this);
        }
        private void OnDisable()
        {
            runtimeRegistry.Remove(this);
        }
        [Initialize(0)]
        private static void ResetAllNextPlayTimes()
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ACWrapper");
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var wrapper = UnityEditor.AssetDatabase.LoadAssetAtPath<ACWrapper>(path);
                wrapper?.SetNextPlayTime(-1f);
            }
#endif
            foreach (var wrapper in runtimeRegistry)
                wrapper.SetNextPlayTime(-1f);
        }
        public void OnBeforeSerialize()
        {
            if (SoundName == "Headhunter, Leather Belt")
            {
                SoundName = name;
                this.Dirty();
            }
        }
        public void OnAfterDeserialize()
        {

        }
        public string SoundName = "Headhunter, Leather Belt";
        [Range(0f, 1f)]
        [SerializeField] float soundVolume = 0.7f;
        [SerializeField] AudioMixerGroup audioMixerOverride;
#if UNITY_EDITOR
        [System.NonSerialized]
        public bool loopInEditor = false;
#endif
        public void ApplyMixerOverride(AudioSource source)
        {
            if (audioMixerOverride != null)
            {
                source.outputAudioMixerGroup = audioMixerOverride;
            }
        }
        [field: SerializeField] public List<ACWrapperEntry> soundClips { get; private set; } = new();
        public void CreateFrom(AudioClip clip)
        {
            soundClips = new()
                {
                    new()
                    {
                        clip = clip,
                        Muted = false,
                        PitchOrigin = 1f,
                        PitchVariancePercent = 5f,
                        Volume = 0.7f,
                    }
                };
            singleChannel = true;
            Is3D = true;
            singleRepeatLockoutTime = 0.02f;
        }
        [field: SerializeField] public bool singleChannel { get; private set; } = false;
        [field: SerializeField] public float singleRepeatLockoutTime { get; private set; } = 0f;
        [field: SerializeField] public bool Is3D { get; private set; }
        private float nextPlayTime;
        public bool ReplayTimeAllowed() => (nextPlayTime - Time.unscaledTime).Absolute() > singleRepeatLockoutTime;
        public float GetNextPlayTime()
        {
            return nextPlayTime;
        }
        public void SetNextPlayTime(float value)
        {
            nextPlayTime = value;
        }
        public void EditorPlaySound()
        {
            EditorPlay(this);
        }
        internal static Dictionary<AudioClip, AudioSource> editorTestPlayers; private static ACWrapper currentLoopingWrapper;
        private static double loopRestartTime = 0;
        private static bool isLooping = false;

        public static void EditorPlay(ACWrapper a, bool loop = false)
        {
#if UNITY_EDITOR
            EditorStopAll();
            if (a == null || a.soundClips == null || a.soundClips.Count == 0)
                return;

            Vector3 position = Camera.main != null ? Camera.main.transform.position : Vector3.zero;

            if (editorTestPlayers == null)
                editorTestPlayers = new Dictionary<AudioClip, AudioSource>();

            double longestClip = 0;
            int index = 0;
            foreach (var item in a.soundClips)
            {
                if (item?.clip == null)
                    continue;

                GameObject player = new GameObject($"EditorTestPlayer_{item.clip.name}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                AudioSource source = player.AddComponent<AudioSource>();
                source.clip = item.clip;
                source.volume = item.Muted ? 0f : a.GetVolume(index);
                source.pitch = item.PitchOrigin.Spread(item.PitchVariancePercent);
                source.loop = false;

                a.ApplyMixerOverride(source);
                player.transform.position = position;
                source.Play();

                editorTestPlayers[item.clip] = source;

                if (item.clip.length > longestClip)
                    longestClip = item.clip.length;

                index++;
            }

            if (loop)
            {
                currentLoopingWrapper = a;
                loopRestartTime = EditorApplication.timeSinceStartup + longestClip;
                isLooping = true;
                EditorApplication.update -= EditorLoopHandler;
                EditorApplication.update += EditorLoopHandler;
            }
#endif
        }
        private static void EditorLoopHandler()
        {
#if UNITY_EDITOR
            if (!isLooping || currentLoopingWrapper == null)
            {
                EditorApplication.update -= EditorLoopHandler;
                return;
            }

            if (EditorApplication.timeSinceStartup >= loopRestartTime)
            {
                EditorPlay(currentLoopingWrapper, true);
            }
#endif
        }
        public static void EditorStopAll()
        {
#if UNITY_EDITOR
            if (editorTestPlayers != null)
            {
                foreach (var src in editorTestPlayers.Values)
                {
                    if (src != null && src.gameObject != null)
                        Object.DestroyImmediate(src.gameObject);
                }
                editorTestPlayers.Clear();
            }

            isLooping = false;
            currentLoopingWrapper = null;
            EditorApplication.update -= EditorLoopHandler;
#endif
        }
        public ACWrapperEntry[] Entries => soundClips.ToArray();
        public float GetVolume(int index)
        {
            if (soundClips[index] == null)
                return soundVolume * 0.7f;
            return soundClips[index].Volume * soundVolume;
        }
        private void OnValidate()
        {
            this.FindEnumerableError(nameof(soundClips), soundClips);
            this.FindStringError(nameof(SoundName), SoundName);
        }
        private void Awake()
        {
            this.FindEnumerableError(nameof(soundClips), soundClips);
            this.FindStringError(nameof(SoundName), SoundName);
            nextPlayTime = 0f;
        }
    }
    #region ACWrapper Entry
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ACWrapperEntry))]
    public class ACWrapperEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;

            Rect clipRect = new Rect(position.x, position.y, position.width, lineHeight);
            Rect pitchVarianceRect = new Rect(position.x, position.y + (lineHeight + spacing) * 1, position.width, lineHeight);
            Rect pitchOriginRect = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);
            Rect volumeRect = new Rect(position.x, position.y + (lineHeight + spacing) * 3, position.width, lineHeight);
            Rect mutedRect = new Rect(position.x, position.y + (lineHeight + spacing) * 4, position.width, lineHeight);
            Rect nameRect = new Rect(position.x, position.y + (lineHeight + spacing) * 5, position.width, lineHeight);

            EditorGUI.PropertyField(clipRect, property.FindPropertyRelative("clip"));
            EditorGUI.Slider(pitchVarianceRect, property.FindPropertyRelative("PitchVariancePercent"), 0f, 80f, new GUIContent("Pitch Variance %"));
            EditorGUI.Slider(pitchOriginRect, property.FindPropertyRelative("PitchOrigin"), 0.2f, 6f, new GUIContent("Pitch Origin"));
            EditorGUI.Slider(volumeRect, property.FindPropertyRelative("Volume"), 0.01f, 1f, new GUIContent("Volume"));
            EditorGUI.PropertyField(mutedRect, property.FindPropertyRelative("Muted"));

            SerializedProperty clipProp = property.FindPropertyRelative("clip");
            string clipName = clipProp.objectReferenceValue != null ? ((AudioClip)clipProp.objectReferenceValue).name : "None";
            EditorGUI.LabelField(nameRect, "Name", clipName);

            EditorGUI.EndProperty();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;
            return (lineHeight + spacing) * 6;
        }
    }
#endif
    [System.Serializable]
    public class ACWrapperEntry
    {
        public AudioClip clip;
        [Range(0f, 80f)]
        public float PitchVariancePercent = 5f;
        [Range(0.2f, 6f)]
        public float PitchOrigin = 1f;
        [Range(0.01f, 1f)]
        public float Volume = 0.7f;
        public bool Muted;
        public string name => clip.name;
    }
    #endregion
    public static class AudioExtensions
    {
        public static bool TryPlayClip(this AudioSource a, AudioClip clip)
        {
            ACWrapper.editorTestPlayers = null;
            if (clip == null)
                return false;

            a.clip = clip;
            a.Play();
            return true;
        }
        public static void PlayWrapper(this AudioSource a, ACWrapper sound, int index)
        {
            if (sound is ACWrapper audio && audio.soundClips != null)
            {
                ACWrapperEntry entry = audio.soundClips[index];
                if (entry.Muted)
                    return;
                a.clip = entry.clip;
                a.pitch = entry.PitchOrigin.Spread(entry.PitchVariancePercent);
                a.volume = sound.GetVolume(index);
                sound.ApplyMixerOverride(a);
                a.Set3D(sound);
                a.Play();
            }
        }
        private static void Set3D(this AudioSource a, ACWrapper w, float? maxDistance = null)
        {
            float _maxDistance;
            if (w.Is3D)
            {
                if (AudioEngine.Source3D is AudioSource a3D and not null)
                {
                    _maxDistance = (maxDistance ?? a3D.maxDistance).Max(20f);
                    a.rolloffMode = AudioRolloffMode.Custom;
                    a.SetCustomCurve(AudioSourceCurveType.CustomRolloff, a3D.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
                    a.SetCustomCurve(AudioSourceCurveType.SpatialBlend, a3D.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
                    a.SetCustomCurve(AudioSourceCurveType.Spread, a3D.GetCustomCurve(AudioSourceCurveType.Spread));
                    a.maxDistance = _maxDistance;
                    return;
                }
            }
            if (AudioEngine.Source2D is AudioSource a2D and not null)
            {
                a.rolloffMode = AudioRolloffMode.Custom;
                a.SetCustomCurve(AudioSourceCurveType.CustomRolloff, a2D.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
                a.SetCustomCurve(AudioSourceCurveType.SpatialBlend, a2D.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
                a.SetCustomCurve(AudioSourceCurveType.Spread, a2D.GetCustomCurve(AudioSourceCurveType.Spread));
            }
        }
    }
}
