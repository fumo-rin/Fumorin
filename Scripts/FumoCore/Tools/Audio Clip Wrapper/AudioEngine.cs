using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace rinCore
{
    internal static class AudioEngineExtensionsInternal
    {
        public static void CopySettingsFrom(this AudioSource target, AudioSource template)
        {
            if (target == null || template == null)
            {
                Debug.LogWarning("AudioSource CopySettingsFrom failed: Target or Template is null.");
                return;
            }

            target.clip = template.clip;
            target.outputAudioMixerGroup = template.outputAudioMixerGroup;
            target.mute = template.mute;
            target.bypassEffects = template.bypassEffects;
            target.bypassListenerEffects = template.bypassListenerEffects;
            target.bypassReverbZones = template.bypassReverbZones;
            target.playOnAwake = template.playOnAwake;
            target.loop = template.loop;

            target.priority = template.priority;
            target.volume = template.volume;
            target.pitch = template.pitch;
            target.panStereo = template.panStereo;
            target.spatialBlend = template.spatialBlend;
            target.reverbZoneMix = template.reverbZoneMix;

            target.dopplerLevel = template.dopplerLevel;
            target.spread = template.spread;
            target.rolloffMode = template.rolloffMode;
            target.minDistance = template.minDistance;
            target.maxDistance = template.maxDistance;

            target.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CopyCurve(template.GetCustomCurve(AudioSourceCurveType.CustomRolloff)));
            target.SetCustomCurve(AudioSourceCurveType.SpatialBlend, CopyCurve(template.GetCustomCurve(AudioSourceCurveType.SpatialBlend)));
            target.SetCustomCurve(AudioSourceCurveType.Spread, CopyCurve(template.GetCustomCurve(AudioSourceCurveType.Spread)));
            target.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, CopyCurve(template.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix)));
        }

        private static AnimationCurve CopyCurve(AnimationCurve sourceCurve)
        {
            if (sourceCurve == null) return null;

            // Creates a distinct copy of the curve data and its keys
            return new AnimationCurve(sourceCurve.keys)
            {
                postWrapMode = sourceCurve.postWrapMode,
                preWrapMode = sourceCurve.preWrapMode
            };
        }
    }
    #region Single Channel
    public partial class AudioEngine
    {
        Dictionary<ACWrapperEntry, AudioSource> singleChannels;
        private bool TrySingleChannel(ACWrapperEntry entry, ACWrapper wrapper, out AudioSource source)
        {
            source = null;

            if (singleChannels == null)
                singleChannels = new();

            if (singleChannels.TryGetValue(entry, out source))
            {
                if (source == null)
                {
                    singleChannels.Remove(entry);
                    source = null;
                }
            }

            if (source == null && instance != null && RinHelper.ValidGameObjects(instance))
            {
                AudioSource copy = Source3DSingle;
                switch (wrapper.soundMode)
                {
                    case ACWrapper.SoundPlayMode.Single3D:
                        break;
                    case ACWrapper.SoundPlayMode.Single2D:
                        copy = Source2DSingle;
                        break;
                    case ACWrapper.SoundPlayMode.Single2DNonDirectional:
                        copy = Source2DSingleNondirectional;
                        break;
                    case ACWrapper.SoundPlayMode.Dynamic3D:
                        copy = Source3DDynamic;
                        break;
                    case ACWrapper.SoundPlayMode.Dynamic2D:
                        copy = Source2DDynamic;
                        break;
                    default:
                        break;
                }
                source = RequestChannel(entry.ToString(), SceneRoot, copy);
                singleChannels[entry] = source;
            }

            return source != null;
        }
    }
    #endregion
    #region Play Sound
    public partial class AudioEngine
    {
        internal static void PlayWrapper(ACWrapper a, Vector3 position)
        {
            if (instance is not AudioEngine engine)
            {
                return;
            }
            if (a.singleRepeatLockoutTime > 0f)
            {
                if (!a.ReplayTimeAllowed())
                    return;

                a.SetNextPlayTime(Time.unscaledTime + a.singleRepeatLockoutTime);
            }

            for (int i = 0; i < a.soundClips.Count; i++)
            {
                if (!a.IsDynamic && engine.TrySingleChannel(a.Entries[i], a, out AudioSource s))
                {
                    s.transform.position = position;
                    s.PlayWrapper(a, i);
                }
                else
                {
                    switch (a.soundMode)
                    {
                        case ACWrapper.SoundPlayMode.Dynamic3D:
                            SoundIteration = engine.Dynamic3DQueue.Dequeue();
                            engine.Dynamic3DQueue.Enqueue(SoundIteration);

                            SoundIteration.transform.position = position;
                            SoundIteration.PlayWrapper(a, i);
                            break;
                        default:
                            SoundIteration = engine.Dynamic2DQueue.Dequeue();
                            engine.Dynamic2DQueue.Enqueue(SoundIteration);

                            SoundIteration.transform.position = position;
                            SoundIteration.PlayWrapper(a, i);
                            break;
                    }
                }
            }
        }
    }

    #endregion
    [DefaultExecutionOrder(5)]
    public partial class AudioEngine : MonoBehaviour
    {
        static AudioEngine instance;
        public AudioSource Source3DSingle;
        public AudioSource Source2DSingle;
        public AudioSource Source2DSingleNondirectional;
        public AudioSource Source3DDynamic;
        public AudioSource Source2DDynamic;

        public const int SoundChannels = 32;
        static Transform cachedRoot;
        static Transform cachedDynamicStack;
        static Transform SceneRoot
        {
            get
            {
                if (cachedRoot == null)
                {
                    cachedRoot = new GameObject("Audio Engine").transform;
                }
                return cachedRoot;
            }
        }
        static Transform DynamicStack
        {
            get
            {
                if (cachedDynamicStack == null)
                {
                    cachedDynamicStack = new GameObject("Audio Dynamic Stack").transform;
                    Object.DontDestroyOnLoad(cachedDynamicStack.gameObject);
                }
                return cachedDynamicStack;
            }
        }
        [Initialize(-9999999)]
        static void ReinitializeACW()
        {
            instance = null;
        }
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
                return;
            }
            Destroy(gameObject);
        }
        Queue<AudioSource> Dynamic2DQueue = new();
        Queue<AudioSource> Dynamic3DQueue = new();
        List<AudioSource> SoundStack = new();
        static AudioSource SoundIteration = new();
        private AudioSource RequestChannel(string name, Transform parent, AudioSource m)
        {
            GameObject g = new GameObject("Channel " + name);
            g.transform.SetParent(parent, false);

            var source = g.AddComponent<AudioSource>();
            source.CopySettingsFrom(m);
            source.outputAudioMixerGroup = m.outputAudioMixerGroup;
            source.playOnAwake = false;
            source.loop = false;



            return source;
        }
        public void Initialize()
        {
            GameObject staticObject = new("Dynamic ACPlayer");
            DontDestroyOnLoad(staticObject);
            AudioSource iteration;
            for (int i = 0; i < SoundChannels; i++)
            {
                iteration = RequestChannel(i.ToString(), DynamicStack, Source2DDynamic);
                iteration.transform.SetParent(staticObject.transform);
                Dynamic2DQueue.Enqueue(iteration);
                SoundStack.Add(iteration);
            }
            for (int i = 0; i < SoundChannels; i++)
            {
                iteration = RequestChannel(i.ToString(), DynamicStack, Source3DDynamic);
                iteration.transform.SetParent(staticObject.transform);
                Dynamic3DQueue.Enqueue(iteration);
                SoundStack.Add(iteration);
            }
        }
    }
}