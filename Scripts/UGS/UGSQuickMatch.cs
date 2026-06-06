using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;

namespace rinCore.UGS
{
    public static class UGSQuickMatch
    {
        const int MaxPlayers = 16;

        const string RelayKey = "relayCode";
        const string GameModeKey = "gameMode";

        static Lobby currentLobby;

        static bool isHost;
        static bool heartbeatRunning;

        static bool isConnecting;
        static bool isInSession;
        [Initialize(-99999)]
        private static void Reinitialize()
        {
            currentLobby = null;

            isHost = false;
            heartbeatRunning = false;

            isConnecting = false;
            isInSession = false;
        }
        public static bool HasSessionOrConnecting => isConnecting || isInSession;

        public static Action OnStartGame;
        static UGSQuickMatch()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnect;
            }
        }
        public static async Task Join(string gameMode)
        {
            if (HasSessionOrConnecting)
            {
                Debug.LogWarning("[UGSQuickMatch] Already in session or connecting.");
                return;
            }

            if (!await UGSInitializer.IsReadyAsync())
            {
                Debug.LogWarning("[UGSQuickMatch] UGS not ready.");
                return;
            }

            isConnecting = true;

            try
            {
                await TryJoinOrCreate(gameMode);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                isConnecting = false;
            }
        }
        public static async Task Leave()
        {
            await LeaveInternal();
        }
        static async Task TryJoinOrCreate(string gameMode)
        {
            try
            {
                var joinedLobbies = await LobbyService.Instance.GetJoinedLobbiesAsync();
                if (joinedLobbies != null && joinedLobbies.Count > 0)
                {
                    Debug.LogWarning($"[UGSQuickMatch] Found {joinedLobbies.Count} hanging backend lobby connections. Cleaning up in background...");

                    _ = BackgroundLobbyCleanup(joinedLobbies);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UGSQuickMatch] Failed checking lingering lobbies: {ex.Message}");
            }

            Debug.Log("[UGSQuickMatch] Proceeding with lobby query...");
            var lobbies = await LobbyService.Instance.QueryLobbiesAsync();
            Lobby target = null;

            foreach (var l in lobbies.Results)
            {
                if (l.Players.Count >= MaxPlayers)
                    continue;

                if (!l.Data.ContainsKey(GameModeKey))
                    continue;

                if (l.Data[GameModeKey].Value != gameMode)
                    continue;

                target = l;
                break;
            }

            if (target != null)
                await JoinLobby(target);
            else
                await CreateLobby(gameMode);
        }
        private static async Task BackgroundLobbyCleanup(System.Collections.Generic.List<string> lobbyIds)
        {
            foreach (var lobbyId in lobbyIds)
            {
                try
                {
                    Debug.Log($"[UGSQuickMatch] Background worker removing player from lobby: {lobbyId}");
                    await LobbyService.Instance.RemovePlayerAsync(lobbyId, AuthenticationService.Instance.PlayerId);
                    Debug.Log($"[UGSQuickMatch] Background worker successfully left lobby: {lobbyId}");
                }
                catch (Exception ex)
                {
                    Debug.Log($"[UGSQuickMatch] Background cleanup note (Safe to ignore): {ex.Message}");
                }
            }
        }
        static async Task CreateLobby(string gameMode)
        {
            isHost = true;

            Allocation alloc =
                await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);

            string joinCode =
                await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            currentLobby =
                await LobbyService.Instance.CreateLobbyAsync(
                    "QuickMatch",
                    MaxPlayers,
                    new CreateLobbyOptions
                    {
                        Data = new System.Collections.Generic.Dictionary<string, DataObject>
                        {
                            {
                                RelayKey,
                                new DataObject(DataObject.VisibilityOptions.Member, joinCode)
                            },
                            {
                                GameModeKey,
                                new DataObject(DataObject.VisibilityOptions.Public, gameMode)
                            }
                        }
                    });

            SetupHostRelay(alloc);

            NetworkManager.Singleton.StartHost();

            isConnecting = false;
            isInSession = true;

            StartHeartbeat();
        }
        static void SetupHostRelay(Allocation alloc)
        {
            var transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetHostRelayData(
                alloc.RelayServer.IpV4,
                (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes,
                alloc.Key,
                alloc.ConnectionData
            );
        }
        static async Task JoinLobby(Lobby lobby)
        {
            try
            {
                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);

                string code = currentLobby.Data[RelayKey].Value;
                JoinAllocation alloc = await RelayService.Instance.JoinAllocationAsync(code);

                SetupClientRelay(alloc);
                NetworkManager.Singleton.StartClient();

                isConnecting = false;
                isInSession = true;
            }
            catch (RelayServiceException relayEx) when (relayEx.Message.Contains("not found") || relayEx.Message.Contains("404"))
            {
                Debug.LogWarning($"[UGSQuickMatch] Joined lobby {lobby.Id} but its Relay code was dead (Zombie Lobby). Self-healing...");

                try
                {
                    await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
                }
                catch (Exception) { /* Player might not even be fully registered yet, ignore */ }

                currentLobby = null;

                string failedGameMode = lobby.Data.ContainsKey(GameModeKey) ? lobby.Data[GameModeKey].Value : "Default";

                Debug.Log($"[UGSQuickMatch] Spawning a fresh lobby for game mode: {failedGameMode}");
                await CreateLobby(failedGameMode);
            }
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.AlreadySubscribedToLobby)
            {
                Debug.LogWarning("[UGSQuickMatch] 409 Conflict: Already in this lobby. Attempting to force resolve state.");

                currentLobby = lobby;
                string code = lobby.Data[RelayKey].Value;
                JoinAllocation alloc = await RelayService.Instance.JoinAllocationAsync(code);

                SetupClientRelay(alloc);
                NetworkManager.Singleton.StartClient();

                isConnecting = false;
                isInSession = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[UGSQuickMatch] JoinLobby failed catastrophically: {e.Message}");
                isConnecting = false;
                isInSession = false;
                currentLobby = null;
                throw;
            }
        }
        static void SetupClientRelay(JoinAllocation alloc)
        {
            var transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetClientRelayData(
                alloc.RelayServer.IpV4,
                (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes,
                alloc.Key,
                alloc.ConnectionData,
                alloc.HostConnectionData
            );
        }
        static void StartHeartbeat()
        {
            if (heartbeatRunning) return;

            heartbeatRunning = true;
            HeartbeatLoop().RunRoutine("UGS Heartbeat", false);
        }
        static IEnumerator HeartbeatLoop()
        {
            var wait = new WaitForSeconds(15f);

            while (true)
            {
                if (currentLobby != null)
                    _ = LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);

                yield return wait;
            }
        }
        static void OnDisconnect(ulong clientId)
        {

            if (NetworkManager.Singleton == null)
                return;

            if (clientId != NetworkManager.Singleton.LocalClientId)
                return;
            _ = LeaveInternal();

            isConnecting = false;
            isInSession = false;
            currentLobby = null;
            isHost = false;
            Debug.Log("[UGSQuickMatch] Local disconnect detected.");
        }
        static async Task LeaveInternal()
        {
            try
            {
                isConnecting = false;
                isInSession = false;

                NetworkManager.Singleton?.Shutdown();

                if (currentLobby != null)
                {
                    if (isHost)
                    {
                        await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                    }
                    else
                    {
                        await LobbyService.Instance.RemovePlayerAsync(
                            currentLobby.Id,
                            AuthenticationService.Instance.PlayerId
                        );
                    }
                }

                currentLobby = null;
                isHost = false;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
}