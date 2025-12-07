using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace RinCore.UGS
{
    public static class UGSInitializer
    {
        private static bool _isInitialized;
        private static bool _isInitializing;
        private static bool _isChangingName;
        private static Task _initializationTask;

        private const int MaxRetries = 3;
        private const int RetryDelayMs = 2000;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            _ = InitializeAsync();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _isInitialized = false;
            _isInitializing = false;
            _initializationTask = null;
            _isChangingName = false;
        }
        private static async Task InitializeAsync()
        {
            if (_isInitialized) return;
            if (_isInitializing)
            {
                await _initializationTask;
                return;
            }

            _isInitializing = true;
            _initializationTask = InitializeInternal();
            await _initializationTask;
        }

        private static async Task InitializeInternal()
        {
            int attempt = 0;

            while (attempt < MaxRetries && !_isInitialized)
            {
                attempt++;

                try
                {
                    if (UnityServices.State != ServicesInitializationState.Initialized)
                    {
                        Debug.Log($"[UGS] Initializing Unity Services... (Attempt {attempt}/{MaxRetries})");
                        await UnityServices.InitializeAsync();
                        Debug.Log("[UGS] Unity Services initialized.");
                    }

                    if (AuthenticationService.Instance == null)
                    {
                        Debug.LogWarning("[UGS] AuthenticationService not available yet.");
                        await Task.Delay(RetryDelayMs);
                        continue;
                    }

                    if (!AuthenticationService.Instance.IsSignedIn)
                    {
                        Debug.Log("[UGS] Signing in anonymously...");
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();
                        Debug.Log($"[UGS] Signed in as Player ID: {AuthenticationService.Instance.PlayerId}");
                    }

                    _isInitialized = true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[UGS] Initialization failed (attempt {attempt}): {e.Message}");
                    await Task.Delay(RetryDelayMs);
                }
            }

            if (!_isInitialized)
                Debug.LogWarning("[UGS] Failed to initialize after retries. UGS unavailable (offline mode).");

            _isInitializing = false;
            _initializationTask = null;
        }

        public static async Task<bool> IsReadyAsync(float timeoutSeconds = 10f)
        {
            float start = Time.realtimeSinceStartup;
            await InitializeAsync();

            while ((!_isInitialized ||
                   UnityServices.State != ServicesInitializationState.Initialized ||
                   AuthenticationService.Instance == null ||
                   !AuthenticationService.Instance.IsSignedIn ||
                   _isChangingName) &&
                   Time.realtimeSinceStartup - start < timeoutSeconds)
            {
                await Task.Delay(100);
            }

            bool ready = _isInitialized &&
                         UnityServices.State == ServicesInitializationState.Initialized &&
                         AuthenticationService.Instance != null &&
                         AuthenticationService.Instance.IsSignedIn &&
                         !_isChangingName;

            if (!ready)
                Debug.LogWarning("[UGS] IsReadyAsync() timed out or failed.");

            return ready;
        }

        public static async Task SetPlayerNameAsync(string playerName)
        {
            _isChangingName = true;

            if (string.IsNullOrWhiteSpace(playerName))
            {
                Debug.LogWarning("[UGS] Player name is null or empty.");
                _isChangingName = false;
                return;
            }

            await InitializeAsync();

            if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("[UGS] Cannot set player name, not signed in.");
                _isChangingName = false;
                return;
            }

            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
                Debug.Log($"[UGS] Player name updated to: {playerName}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UGS] Failed to set player name: {e.Message}");
            }

            _isChangingName = false;
        }

        public static bool IsReadyFast()
        {
            return _isInitialized &&
                   UnityServices.State == ServicesInitializationState.Initialized &&
                   AuthenticationService.Instance != null &&
                   AuthenticationService.Instance.IsSignedIn;
        }
    }
}
