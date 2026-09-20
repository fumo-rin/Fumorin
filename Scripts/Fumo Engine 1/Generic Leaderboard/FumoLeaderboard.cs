using rinCore.UGS;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    [Serializable]
    public struct MetadataPair
    {
        public string key;
        public string value;

        public MetadataPair(string key, object value)
        {
            this.key = key;
            this.value = value?.ToString() ?? string.Empty;
        }
    }

    [Serializable]
    public class LeaderboardMetadata
    {
        public List<MetadataPair> entries = new();

        public LeaderboardMetadata() { }

        public LeaderboardMetadata(IEnumerable<KeyValuePair<string, object>> pairs)
        {
            if (pairs == null) return;
            foreach (var kvp in pairs)
            {
                Add(kvp.Key, kvp.Value);
            }
        }

        public void Add(string key, object val)
        {
            entries.Add(new MetadataPair(key, val));
        }

        public string Get(string key, string fallback = "")
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key) return entries[i].value;
            }
            return fallback;
        }

        public bool TryGet(string key, out string result)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key)
                {
                    result = entries[i].value;
                    return true;
                }
            }
            result = null;
            return false;
        }
    }

    public struct LeaderboardCacheEntry
    {
        public long score;
        public string player;
        public LeaderboardMetadata metadata;

        public LeaderboardCacheEntry(long score, string player, LeaderboardMetadata metadata)
        {
            this.score = score;
            this.player = player;
            this.metadata = metadata;
        }
    }

    public class LeaderboardPageCache
    {
        private readonly Dictionary<int, List<LeaderboardCacheEntry>> pages = new();

        public bool TryGetPage(int page, out List<LeaderboardCacheEntry> entries)
        {
            return pages.TryGetValue(page, out entries);
        }

        public void SetPage(int page, List<LeaderboardCacheEntry> entries)
        {
            pages[page] = entries;
        }

        public void Clear()
        {
            pages.Clear();
        }
    }

    public class FumoLeaderboard : MonoBehaviour, IUINestRunable
    {
        private static FumoLeaderboard instance;

        private static string _currentLeaderboardKey;
        public static string CurrentLeaderboardKey
        {
            get => _currentLeaderboardKey;
            set => _currentLeaderboardKey = value;
        }

        [Header("Leaderboard Settings")]
        [SerializeField] private List<string> leaderBoardKeys = new();
        [SerializeField] private FumoLeaderboardEntry copyableEntry;
        [SerializeField] private int count = 20;

        [Header("UI Controls")]
        [SerializeField] private Button incrementIndex;
        [SerializeField] private Button decrementIndex;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button prevPageButton;
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private TMP_Text pageText;

        private int currentIndex = 0;
        private int currentPage = 0;
        private bool isSelectorInitialized = false;

        private long activeFetchId = 0;
        private CancellationTokenSource fetchCts;

        private readonly List<FumoLeaderboardEntry> board = new();
        private readonly Dictionary<string, LeaderboardPageCache> cachedLeaderboards = new();

        public int RunnerPriority => -500;

        private void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            CancelPendingFetch();
        }

        public void RunNestComponent(UINest nest)
        {
            EnsureInitialized();
            RefreshStateFromStorage();
            EnsureBoardEntriesCreated();

            string keyToUse = GetOrFallbackKey(leaderBoardKeys);
            if (!string.IsNullOrEmpty(keyToUse))
            {
                CurrentLeaderboardKey = keyToUse;
                UpdateLeaderboardLabel();
                UpdatePageLabel();
                TriggerFetch(keyToUse, currentPage);
            }
        }

        public void EnsureInitialized()
        {
            if (isSelectorInitialized) return;
            isSelectorInitialized = true;

            if (incrementIndex != null) incrementIndex.BindSingleAction(() => CycleLeaderboard(1));
            if (decrementIndex != null) decrementIndex.BindSingleAction(() => CycleLeaderboard(-1));
            if (nextPageButton != null) nextPageButton.BindSingleAction(() => CyclePage(1));
            if (prevPageButton != null) prevPageButton.BindSingleAction(() => CyclePage(-1));
        }

        public void RefreshStateFromStorage()
        {
            if (leaderBoardKeys != null && leaderBoardKeys.Count > 0)
            {
                PersistentJSON.TryLoad(out currentIndex, "Leaderboard Index");
                PersistentJSON.TryLoad(out currentPage, "Leaderboard Page");

                if (leaderBoardKeys.TryGetIndex(currentIndex, out var savedKey))
                    CurrentLeaderboardKey = savedKey;
                else
                    CurrentLeaderboardKey = leaderBoardKeys[0];
            }
        }

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

            TriggerFetch(CurrentLeaderboardKey, currentPage);
        }

        private void CyclePage(int delta)
        {
            currentPage = Mathf.Max(0, (currentPage + delta).Min(9));
            PersistentJSON.TrySave(currentPage, "Leaderboard Page");
            UpdatePageLabel();

            string keyToUse = GetOrFallbackKey(leaderBoardKeys);
            if (!string.IsNullOrEmpty(keyToUse))
                TriggerFetch(keyToUse, currentPage);
        }

        private void TriggerFetch(string key, int page)
        {
            _ = BuildAsync(key, page);
        }

        private void UpdateLeaderboardLabel()
        {
            if (keyText != null)
            {
                string keyToDisplay = GetOrFallbackKey(leaderBoardKeys);
                if (!string.IsNullOrEmpty(keyToDisplay))
                {
                    keyText.text = keyToDisplay.SafeRemoveWords(Application.productName);
                }
            }
        }

        private void UpdatePageLabel()
        {
            if (pageText != null)
                pageText.text = $"Page {currentPage + 1}";
        }

        public static string GetOrFallbackKey(List<string> keys)
        {
            if (!string.IsNullOrEmpty(CurrentLeaderboardKey))
                return CurrentLeaderboardKey;

            return keys != null && keys.Count > 0 ? keys[0] : string.Empty;
        }

        private void EnsureBoardEntriesCreated()
        {
            if (copyableEntry == null) return;

            board.RemoveAll(x => x == null);

            if (board.Count == 0)
            {
                copyableEntry.gameObject.SetActive(false);
                copyableEntry.Clear();
                for (int i = 0; i < count; i++)
                {
                    var clone = copyableEntry.Spawn2D(Vector2.zero, copyableEntry.transform.parent);
                    board.Add(clone);
                    clone.Clear();
                    clone.gameObject.SetActive(true);
                    clone.transform.localScale = Vector3.one;
                }
            }
        }

        private void CancelPendingFetch()
        {
            activeFetchId++;
            if (fetchCts != null)
            {
                fetchCts.Cancel();
                fetchCts.Dispose();
                fetchCts = null;
            }
        }

        public async Task BuildAsync(string key, int page = 0)
        {
            if (string.IsNullOrEmpty(key)) return;

            CurrentLeaderboardKey = key;
            EnsureBoardEntriesCreated();

            if (cachedLeaderboards.TryGetValue(key, out var cache) && cache.TryGetPage(page, out var cachedData))
            {
                ApplyCachedEntries(cachedData);
                return;
            }

            CancelPendingFetch();

            long thisFetchId = ++activeFetchId;
            fetchCts = new CancellationTokenSource();
            var token = fetchCts.Token;

            bool ready = await UGSInitializer.IsReadyAsync();
            if (!ready || token.IsCancellationRequested || thisFetchId != activeFetchId) return;

            try
            {
                int offset = page * count;

                // Step 1: Call UGS on Main Thread so RateLimiter can safely read Time.unscaledTime
                var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(
                    key,
                    new GetScoresOptions
                    {
                        Limit = count,
                        Offset = offset,
                        IncludeMetadata = true
                    }
                );

                if (token.IsCancellationRequested || thisFetchId != activeFetchId) return;

                // Step 2: Pass UGS payload into Task.Run to do parsing & profanity checks on Background Thread!
                var cacheList = await Task.Run(() =>
                {
                    var parsedList = new List<LeaderboardCacheEntry>(count);
                    if (scoresResponse?.Results == null) return parsedList;

                    int resultsCount = scoresResponse.Results.Count;

                    for (int i = 0; i < resultsCount && i < count; i++)
                    {
                        var data = scoresResponse.Results[i];
                        string playerName = string.IsNullOrEmpty(data.PlayerName) ? data.PlayerId : data.PlayerName;

                        if (!string.IsNullOrEmpty(playerName) && BadWords.CleanReplaceFunny(playerName.RemoveAfter("#").Letterize(), BadWords.BadWordsList, out string clean, out string badWord, 16))
                        {
                            playerName = clean;
                        }
                        long score = data.Score.ToLong();

                        string rawMeta = SanitizeInputString(data.Metadata, 1024);
                        LeaderboardMetadata parsedMeta = null;

                        if (!string.IsNullOrEmpty(rawMeta))
                        {
                            try
                            {
                                parsedMeta = JsonUtility.FromJson<LeaderboardMetadata>(rawMeta);
                            }
                            catch (Exception) { }
                        }

                        parsedList.Add(new LeaderboardCacheEntry(score, playerName, parsedMeta));
                    }

                    return parsedList;
                }, token);

                if (token.IsCancellationRequested || thisFetchId != activeFetchId) return;

                // Step 3: Apply UI updates on Main Thread cleanly!
                for (int i = 0; i < board.Count; i++)
                {
                    if (board[i] == null) continue;

                    if (i < cacheList.Count)
                    {
                        var cacheEntry = cacheList[i];
                        board[i].Set(cacheEntry.score, cacheEntry.player, offset + i + 1, cacheEntry.metadata);
                        board[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        board[i].Clear();
                    }
                }

                if (!cachedLeaderboards.TryGetValue(key, out var pageCache))
                {
                    pageCache = new LeaderboardPageCache();
                    cachedLeaderboards[key] = pageCache;
                }

                pageCache.SetPage(page, cacheList);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                if (!token.IsCancellationRequested && thisFetchId == activeFetchId)
                {
                    Debug.LogError($"[FumoLeaderboard] Exception fetching scores: {e}");
                }
            }
        }

        private void ApplyCachedEntries(List<LeaderboardCacheEntry> cachedData)
        {
            EnsureBoardEntriesCreated();
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i] == null) continue;

                if (i < cachedData.Count)
                {
                    var entry = cachedData[i];
                    board[i].Set(entry, i + 1);
                    board[i].gameObject.SetActive(true);
                }
                else
                {
                    board[i].Clear();
                }
            }
        }

        public static void InvalidateCache(string key = null)
        {
            if (instance == null) return;

            if (string.IsNullOrEmpty(key))
            {
                instance.cachedLeaderboards.Clear();
            }
            else
            {
                instance.cachedLeaderboards.Remove(key);
            }
        }

        public static async Task SubmitScoreAsync(long score, LeaderboardMetadata metadata = null)
        {
            string key = CurrentLeaderboardKey;

            if (score <= 0 || score == long.MaxValue || string.IsNullOrEmpty(key)) return;

            bool ready = await UGSInitializer.IsReadyAsync();
            if (!ready) return;

            try
            {
                try
                {
                    var existingScoreEntry = await LeaderboardsService.Instance.GetPlayerScoreAsync(key);
                    if (existingScoreEntry != null && existingScoreEntry.Score >= score) return;
                }
                catch (LeaderboardsException ex) when (ex.Reason == LeaderboardsExceptionReason.EntryNotFound) { }

                var options = new AddPlayerScoreOptions();
                if (metadata != null)
                {
                    string sanitizedJson = await Task.Run(() =>
                    {
                        string rawJson = JsonUtility.ToJson(metadata);
                        string cleanJson = SanitizeInputString(rawJson, 1024);
                        return (!string.IsNullOrEmpty(cleanJson) && Encoding.UTF8.GetByteCount(cleanJson) <= 1024) ? cleanJson : null;
                    });

                    if (!string.IsNullOrEmpty(sanitizedJson))
                    {
                        options.Metadata = sanitizedJson;
                    }
                }

                await LeaderboardsService.Instance.AddPlayerScoreAsync(key, score, options);
                InvalidateCache(key);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FumoLeaderboard] Error submitting score: {e}");
            }
        }

        private static string SanitizeInputString(string input, int maxCharLimit)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            if (input.Length > maxCharLimit)
                input = input.Substring(0, maxCharLimit);

            var sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (!char.IsControl(c) || c == '\r' || c == '\n' || c == '\t')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}