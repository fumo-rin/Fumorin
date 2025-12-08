using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityAddressables = UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using UnityEngine.AddressableAssets;
using System.Security.Cryptography;

namespace RinCore
{
    public static class AddressablesTools
    {
        #region Keys
        public static string KeyAny => "Any";
        public static string KeyItem => "Items";
        public static string KeyNameGenerationSettings => "Name Generation Settings";
        #endregion
        private static async void LoadKeysToAction<T>(List<string> keys, Action<IList<T>> callback, bool async = true)
        {
            if (keys == null || keys.Count == 0)
            {
                Debug.LogWarning($"[AddressablesTools] No keys provided for loading {typeof(T).Name} assets.");
                return;
            }

            async Task LoadAsync()
            {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(keys, Addressables.MergeMode.Union, typeof(T));
                await locationsHandle.Task;

                if (locationsHandle.Status != AsyncOperationStatus.Succeeded || locationsHandle.Result == null || locationsHandle.Result.Count == 0)
                {
                    Debug.LogWarning($"[AddressablesTools] No resource locations found for keys: {string.Join(", ", keys)} (Type: {typeof(T).Name})");
                    return;
                }

                var loadHandle = Addressables.LoadAssetsAsync<T>(keys, null, Addressables.MergeMode.Union);
                await loadHandle.Task;

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogWarning($"[AddressablesTools] Asset load failed for keys: {string.Join(", ", keys)} (Type: {typeof(T).Name})");
                    return;
                }

                if (loadHandle.Result == null || loadHandle.Result.Count == 0)
                {
                    Debug.LogWarning($"[AddressablesTools] Asset load succeeded but returned no results for keys: {string.Join(", ", keys)} (Type: {typeof(T).Name})");
                    return;
                }

                callback?.Invoke(loadHandle.Result);
            }

            void LoadSync()
            {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(keys, Addressables.MergeMode.Union, typeof(T));
                locationsHandle.WaitForCompletion();

                if (locationsHandle.Status != AsyncOperationStatus.Succeeded || locationsHandle.Result == null || locationsHandle.Result.Count == 0)
                {
                    Debug.LogWarning($"[AddressablesTools] No resource locations found for keys: {string.Join(", ", keys)} (Type: {typeof(T).Name})");
                    return;
                }

                var loadHandle = Addressables.LoadAssetsAsync<T>(keys, null, Addressables.MergeMode.Union);
                loadHandle.WaitForCompletion();

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogWarning($"[AddressablesTools] Asset load failed for keys: {string.Join(", ", keys)} (Type: {typeof(T).Name})");
                    return;
                }

                if (loadHandle.Result == null || loadHandle.Result.Count == 0)
                {
                    Debug.LogWarning($"[AddressablesTools] Asset load succeeded but returned no results for keys: {string.Join(", ", keys)} (Type: {typeof(T).Name})");
                    return;
                }

                callback?.Invoke(loadHandle.Result);
            }

            if (async)
                await LoadAsync();
            else
                LoadSync();
        }

        public static void LoadKeys<T>(string key, Action<IList<T>> callback, bool async = true) => LoadKeysToAction<T>(new List<string> { key }, callback, async);
        public static void LoadKeys<T>(string key1, string key2, Action<IList<T>> callback, bool async = true) => LoadKeysToAction<T>(new List<string> { key1, key2 }, callback, async);
        public static void LoadKeys<T>(string key1, string key2, string key3, Action<IList<T>> callback, bool async = true) => LoadKeysToAction<T>(new List<string> { key1, key2, key3 }, callback, async);
        public static void LoadKeys<T>(string key1, string key2, string key3, string key4, Action<IList<T>> callback, bool async = true) => LoadKeysToAction<T>(new List<string> { key1, key2, key3, key4 }, callback, async);
    }
}
