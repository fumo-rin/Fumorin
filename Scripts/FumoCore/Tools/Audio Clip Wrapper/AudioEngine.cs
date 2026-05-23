using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace rinCore
{
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
        internal static void PlayWrapper(ACWrapper a, Vector2 position)
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
                if (engine.TrySingleChannel(a.Entries[i], a, out AudioSource s))
                {
                    s.transform.position = position;
                    s.PlayWrapper(a, i);
                }
                else
                {
                    SoundIteration = engine.SoundQueue.Dequeue();
                    engine.SoundQueue.Enqueue(SoundIteration);

                    SoundIteration.transform.position = position;
                    SoundIteration.PlayWrapper(a, i);
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
                return;
            }
            Destroy(gameObject);
        }
        Queue<AudioSource> SoundQueue = new();
        List<AudioSource> SoundStack = new();
        static AudioSource SoundIteration = new();
        private AudioSource RequestChannel(string name, Transform parent, AudioSource m)
        {
            GameObject g = new GameObject("Channel " + name);
            g.transform.SetParent(parent, false);

            var source = g.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = m.outputAudioMixerGroup;
            source.playOnAwake = false;
            source.loop = false;

            return source;
        }
        public void Initialize()
        {
            AudioSource iteration;
            for (int i = 0; i < SoundChannels; i++)
            {
                iteration = RequestChannel(i.ToString(), DynamicStack, Source3DDynamic);
                SoundQueue.Enqueue(iteration);
                SoundStack.Add(iteration);
            }
        }
    }
}