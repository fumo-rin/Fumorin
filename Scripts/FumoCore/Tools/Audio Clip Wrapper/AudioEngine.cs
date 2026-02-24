using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace rinCore
{
    #region Single Channel

    public static partial class AudioEngine
    {
        static Dictionary<ACWrapperEntry, AudioSource> singleChannels;

        private static bool TrySingleChannel(ACWrapperEntry entry, out AudioSource source)
        {
            source = null;

            if (singleChannels == null)
                singleChannels = new();

            // Remove destroyed references automatically
            if (singleChannels.TryGetValue(entry, out source))
            {
                if (source == null)
                {
                    singleChannels.Remove(entry);
                    source = null;
                }
            }

            if (source == null)
            {
                source = RequestChannel(entry.ToString(), SceneRoot);
                source.outputAudioMixerGroup = SingleChannelsMixer;
                singleChannels[entry] = source;
            }

            return source != null;
        }
    }

    #endregion
    #region Play Sound

    public static partial class AudioEngine
    {
        internal static void PlayWrapper(ACWrapper a, Vector2 position)
        {
            if (a.singleRepeatLockoutTime > 0f)
            {
                if (!a.ReplayTimeAllowed())
                    return;

                a.SetNextPlayTime(Time.unscaledTime + a.singleRepeatLockoutTime);
            }

            for (int i = 0; i < a.soundClips.Count; i++)
            {
                if (a.singleChannel && TrySingleChannel(a.Entries[i], out AudioSource s))
                {
                    s.transform.position = position;
                    s.PlayWrapper(a, i);
                }
                else
                {
                    SoundIteration = SoundQueue.Dequeue();
                    SoundQueue.Enqueue(SoundIteration);

                    SoundIteration.transform.position = position;
                    SoundIteration.PlayWrapper(a, i);
                }
            }
        }
    }

    #endregion
    [DefaultExecutionOrder(5)]
    public static partial class AudioEngine
    {
        public static AudioMixerGroup DynamicChannelsMixer { get; private set; }
        public static AudioMixerGroup SingleChannelsMixer { get; private set; }

        const string DynamicChannelsKey = "Dynamic Channels";
        const string SingleChannelsKey = "Single Channels";
        const string AudioEngineAddressableKey = "Audio Engine";

        const string AudioEngine3DPlayerName = "3D Audio Channel";
        const string AudioEngine2DPlayerName = "2D Audio Channel";

        public static AudioSource Source3D;
        public static AudioSource Source2D;

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

        static Queue<AudioSource> SoundQueue;
        static List<AudioSource> SoundStack;
        static AudioSource SoundIteration;

        private static AudioSource RequestChannel(string name, Transform parent)
        {
            GameObject g = new GameObject("Channel " + name);
            g.transform.SetParent(parent, false);

            var source = g.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;

            return source;
        }
        #region Initialization
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            singleChannels = new();
            SoundQueue = new();
            SoundStack = new();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            AudioSource iteration;
            for (int i = 0; i < SoundChannels; i++)
            {
                iteration = RequestChannel(i.ToString(), DynamicStack);
                SoundQueue.Enqueue(iteration);
                SoundStack.Add(iteration);
            }
        }
        static void OnSceneUnloaded(Scene scene)
        {
            singleChannels?.Clear();
            if (cachedRoot != null)
            {
                Object.Destroy(cachedRoot.gameObject);
                cachedRoot = null;
            }
        }
        #endregion
        #region After Scene Load
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AfterSceneLoad()
        {
            void DynamicsSetup(IList<AudioMixerGroup> mixers)
            {
                foreach (var group in mixers)
                {
                    if (group != null)
                        DynamicChannelsMixer = group;
                }

                if (DynamicChannelsMixer == null)
                {
                    Debug.LogWarning("Failed to find Mixer group for Dynamic Channels.");
                }

                foreach (var channel in SoundStack)
                {
                    if (channel != null)
                        channel.outputAudioMixerGroup = DynamicChannelsMixer;
                }
            }

            void SingleChannelsSetup(IList<AudioMixerGroup> mixers)
            {
                foreach (var group in mixers)
                {
                    if (group != null)
                        SingleChannelsMixer = group;
                }

                if (SingleChannelsMixer == null)
                {
                    Debug.LogWarning("Failed to find Mixer group for Single Channels.");
                }
            }

            void SetupSources(IList<GameObject> sourceObjects)
            {
                foreach (GameObject g in sourceObjects)
                {
                    if (g == null) continue;

                    if (g.TryGetComponent(out AudioSource source))
                    {
                        if (source.transform.name == AudioEngine3DPlayerName)
                            Source3D = source;

                        if (source.transform.name == AudioEngine2DPlayerName)
                            Source2D = source;
                    }
                }
            }

            rinCore.AddressablesTools.LoadKeys<AudioMixerGroup>(DynamicChannelsKey, DynamicsSetup);
            rinCore.AddressablesTools.LoadKeys<AudioMixerGroup>(SingleChannelsKey, SingleChannelsSetup);
            rinCore.AddressablesTools.LoadKeys<GameObject>(AudioEngineAddressableKey, SetupSources);
        }

        #endregion
    }
}