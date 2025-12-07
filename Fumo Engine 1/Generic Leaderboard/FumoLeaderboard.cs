using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Authentication;
using FumoCore.Tools;
using Unity.Services.Core;
using FumoCore.UGS;

namespace Fumorin
{
    #region UI Controls
    public partial class FumoLeaderboard
    {
        [SerializeField] List<string> leaderBoardKeys = new();
        [SerializeField] Button incrementIndex, decrementIndex;
        [SerializeField] Button nextPageButton, prevPageButton;
        [SerializeField] private TMP_Text leaderboardTitleText;
        [SerializeField] private TMP_Text pageText;
        private int currentIndex = 0;
        private int currentPage = 0;

        private void CycleLeaderboard(int delta)
        {
            if (leaderBoardKeys == null || leaderBoardKeys.Count == 0) return;

            leaderBoardKeys.WrapIndex(currentIndex + delta, out currentIndex);
            PersistentJSON.TrySave(currentIndex, "Leaderboard Index");
            currentPage = 0;
            PersistentJSON.TrySave(currentPage, "Leaderboard Page");

            CurrentLeaderboardKey = leaderBoardKeys[currentIndex];
            UpdateLeaderboardLabel();
            UpdatePageLabel();
        }

        private void CyclePage(int delta)
        {
            currentPage = Mathf.Max(0, (currentPage + delta).Min(9));
            PersistentJSON.TrySave(currentPage, "Leaderboard Page");
            UpdatePageLabel();
            if (!string.IsNullOrEmpty(CurrentLeaderboardKey))
                Build(CurrentLeaderboardKey, currentPage);
        }

        private void UpdateLeaderboardLabel()
        {
            if (leaderboardTitleText != null && leaderBoardKeys.TryGetIndex(currentIndex, out var key))
            {
                leaderboardTitleText.text = key.SafeRemoveWords(Application.productName);
            }
        }

        private void UpdatePageLabel()
        {
            if (pageText != null)
                pageText.text = $"Page {currentPage + 1}";
        }

        private void StartLeaderboardSelector()
        {
            if (incrementIndex != null) incrementIndex.BindSingleAction(() => CycleLeaderboard(1));
            if (decrementIndex != null) decrementIndex.BindSingleAction(() => CycleLeaderboard(-1));
            if (nextPageButton != null) nextPageButton.BindSingleAction(() => CyclePage(1));
            if (prevPageButton != null) prevPageButton.BindSingleAction(() => CyclePage(-1));

            if (leaderBoardKeys.Count > 0)
            {
                currentIndex = 0;
                currentPage = 0;

                PersistentJSON.TryLoad(out currentIndex, "Leaderboard Index");
                PersistentJSON.TryLoad(out currentPage, "Leaderboard Page");

                CurrentLeaderboardKey = leaderBoardKeys[currentIndex];
                UpdateLeaderboardLabel();
                UpdatePageLabel();
            }
        }
    }
    #endregion

    public partial class FumoLeaderboard : MonoBehaviour
    {
        private static string _currentLeaderboardKey;
        public static string CurrentLeaderboardKey
        {
            get => _currentLeaderboardKey;
            set
            {
                _currentLeaderboardKey = value;
                if (instance != null)
                    instance.Build(value, instance.currentPage);
            }
        }

        static FumoLeaderboard instance;

        [SerializeField] FumoLeaderboardEntry copyableEntry;
        [SerializeField] int count = 20;

        private List<FumoLeaderboardEntry> board = new();
        private Dictionary<string, Dictionary<int, List<(long score, string player)>>> cachedLeaderboards = new();

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            copyableEntry.Clear();
            for (int i = 0; i < count; i++)
            {
                var clone = copyableEntry.Spawn2D(Vector2.zero, copyableEntry.transform.parent);
                board.Add(clone);
                clone.Clear();
                clone.transform.localScale = Vector3.one;
            }
            copyableEntry.gameObject.SetActive(false);
            StartLeaderboardSelector();
        }

        private async void Build(string key, int page = 0)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("Leaderboard ID not set!");
                return;
            }
            bool ready = await UGSInitializer.IsReadyAsync();
            if (!ready)
            {
                Debug.LogWarning("[FumoLeaderboard] UGS not ready, cannot fetch leaderboard yet.");
                return;
            }
            if (cachedLeaderboards.TryGetValue(key, out var pageDict) && pageDict.TryGetValue(page, out var cachedData))
            {
                ApplyCachedEntries(cachedData);
                return;
            }
            foreach (var entry in board)
                entry.Clear();

            try
            {
                int offset = page * count;
                var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(
                    key,
                    new GetScoresOptions
                    {
                        Limit = count,
                        Offset = offset
                    }
                );

                var cacheList = new List<(long score, string player)>();

                for (int i = 0; i < board.Count; i++)
                {
                    if (i < scoresResponse.Results.Count)
                    {
                        var data = scoresResponse.Results[i];
                        string playerName = string.IsNullOrEmpty(data.PlayerName) ? data.PlayerId : data.PlayerName;
                        long score = data.Score.ToLong();
                        board[i].Set(score, playerName, offset + i + 1);
                        cacheList.Add((score, playerName));
                    }
                    else
                    {
                        board[i].Clear();
                    }
                }

                if (!cachedLeaderboards.ContainsKey(key))
                    cachedLeaderboards[key] = new();

                cachedLeaderboards[key][page] = cacheList;

                Debug.Log($"Fetched and cached {scoresResponse.Results.Count} leaderboard entries for {key}, page {page}.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error fetching leaderboard: {e}");
            }
        }
        private void ApplyCachedEntries(List<(long score, string player)> cachedData)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (i < cachedData.Count)
                {
                    var entry = cachedData[i];
                    board[i].Set(entry.score, entry.player, i + 1);
                }
                else
                {
                    board[i].Clear();
                }
            }
        }
        public static async Task SubmitScoreAsync(long score)
        {
            string key = CurrentLeaderboardKey;
            if (score <= 0d)
            {
                return;
            }
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("Leaderboard key is null or empty!");
                return;
            }
            bool ready = await UGSInitializer.IsReadyAsync();
            if (!ready)
            {
                Debug.LogWarning("[FumoLeaderboard] UGS not ready — cannot submit score.");
                return;
            }
            try
            {
                var result = await LeaderboardsService.Instance.AddPlayerScoreAsync(key, score);
                Debug.Log($"Score {result.Score} submitted successfully to {key} for player {AuthenticationService.Instance.PlayerId}.");

                if (instance != null && instance.cachedLeaderboards.ContainsKey(key))
                    instance.cachedLeaderboards.Remove(key);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error submitting score to {key}: {e}");
            }
        }
    }
}
