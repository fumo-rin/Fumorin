using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace rinCore
{
    using Unity.Netcode;
    using UnityEngine;
    public static class AudioRegistry
    {
        private static readonly Dictionary<int, ACWrapper> cache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeRegistry()
        {
            cache.Clear();
            ACWrapper[] wrappers = Resources.LoadAll<ACWrapper>("");
            foreach (var wrapper in wrappers)
            {
                if (wrapper == null) continue;
                if (cache.ContainsKey(wrapper.NetworkID))
                {
                    Debug.LogWarning($"Duplicate NetworkID detected! ID: {wrapper.NetworkID} on {wrapper.name} and {cache[wrapper.NetworkID].name}");
                    continue;
                }
                cache[wrapper.NetworkID] = wrapper;
            }
            Debug.Log($"AudioRegistry initialized with {cache.Count} audio wrappers.");
        }

        public static bool TryGet(int id, out ACWrapper wrapper)
        {
            return cache.TryGetValue(id, out wrapper);
        }
    }

    public class NetworkedAudioEngine : NetworkBehaviour
    {
        public static NetworkedAudioEngine Instance { get; private set; }

        [Initialize(-999)]
        static void Reinitialize()
        {
            Instance = null;
        }
        public static void PlayStatic(ACWrapper w, Vector3 pos)
        {
            if (Instance is not NetworkedAudioEngine e)
                return;

            e.PlayNetworked(w, pos);
        }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        [Rpc(SendTo.Everyone)]
        private void PlayAudioRpc(int soundId, Vector3 position)
        {
            if (!AudioRegistry.TryGet(soundId, out ACWrapper wrapper))
                return;

            AudioEngine.PlayWrapper(wrapper, position);
        }

        public void PlayNetworked(ACWrapper wrapper, Vector3 position)
        {
            if (wrapper == null)
                return;

            PlayAudioRpc(wrapper.NetworkID, position);
        }
    }
}
