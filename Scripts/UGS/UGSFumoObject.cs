using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

namespace rinCore.UGS
{
    public class UGSFumoObject : NetworkBehaviour
    {
        static string _cachedOwnerName;
        public static string OwnerName
        {
            get => _cachedOwnerName;
            set => _cachedOwnerName =
                BadWords.CleanReplaceFunny(value, BadWords.BadWordsList, out var clean, out _, 16)
                ? clean : value;
        }
        [SerializeField] List<GameObject> toggledOwnedObjects = new();
        [SerializeField] TMP_Text playerNameText;

        private NetworkVariable<FixedString32Bytes> networkedPlayerName =
            new("Beaf",
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);
        public override void OnNetworkSpawn()
        {
            networkedPlayerName.OnValueChanged += OnNameChanged;
            foreach (var obj in toggledOwnedObjects)
                obj.SetActive(IsOwner);
            if (IsOwner)
                SubmitNameServerRpc(OwnerName);
            OnNameChanged(default, networkedPlayerName.Value);
        }
        private void OnNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
        {
            if (playerNameText != null)
                playerNameText.text = newValue.ToString();
        }
        [ServerRpc]
        private void SubmitNameServerRpc(string name)
        {
            name = name.Substring(0, Mathf.Min(name.Length, 16));
            networkedPlayerName.Value = name;
        }
        protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
        {
            foreach (var obj in toggledOwnedObjects)
                obj.SetActive(false);
        }
    }
}