using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
namespace Rensenware
{
    #region Utils
    internal static class MinesweeperExtensions
    {
        public static int clamp(this int i, int min, int max)
        {
            return Mathf.Clamp(i, min, max);
        }
        public static string SpaceByCapitals(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;
            var result = new System.Text.StringBuilder();
            result.Append(input[0]);
            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]) && !char.IsWhiteSpace(input[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(input[i]);
            }
            return result.ToString();
        }
        public static string ToSpacedString(this Enum key)
        {
            return key.ToString().SpaceByCapitals();
        }
        public static Color MinesweeperDigitColor(this int digit)
        {
            switch (digit)
            {
                case 1: return Color.blue;
                case 2: return Color.green;
                case 3: return Color.red;
                case 4: return new Color(0f, 0f, 0.5f);
                case 5: return new Color(0.5f, 0f, 0f);
                case 6: return new Color(0f, 0.5f, 0.5f);
                case 7: return Color.black;
                case 8: return Color.gray;
                default: return Color.clear;
            }
        }
    }
    public partial class Minesweeper
    {
        public static class MinesweeperUtils
        {
            public static bool IsBomb(MinesweeperTile s)
            {
                return (s.state & (MinesweeperTile.State.BombBrick | MinesweeperTile.State.BombVisible | MinesweeperTile.State.BombTriggered | MinesweeperTile.State.CorrectFlag)) != 0;
            }
            public static bool TryGetRandomTile(Dictionary<(int, int), MinesweeperTile> board, int sizeX, int sizeY, out MinesweeperTile result)
            {
                result = null;
                if (board == null || board.Count == 0)
                    return false;

                int x = UnityEngine.Random.Range(0, sizeX);
                int y = UnityEngine.Random.Range(0, sizeY);
                return board.TryGetValue((x, y), out result);
            }
        }
    }
    #endregion
    #region Base Tile
    public partial class Minesweeper
    {
        public class MinesweeperTile
        {
            [Flags]
            public enum State
            {
                None = 0,
                Cleared = 1 << 0,
                CorrectFlag = 1 << 1,
                FalseFlag = 1 << 2,
                BombBrick = 1 << 3,
                BombTriggered = 1 << 4,
                BombVisible = 1 << 5,
                Brick = 1 << 6
            }
            public State state { get; private set; } = State.None;
            public MinesweeperTile(State s)
            {
                state = s;
            }
            public (int, int) tileXY;
            public bool IsBomb => Minesweeper.MinesweeperUtils.IsBomb(this);
            public MinesweeperTile PlaceBomb()
            {
                return UpdateState(State.BombBrick);
            }
            public MinesweeperTile UpdateState(State s)
            {
                state = s;
                return this;
            }
            public int NearbyBombs
            {
                get
                {
                    int count = 0;
                    int x = tileXY.Item1;
                    int y = tileXY.Item2;

                    for (int i = x - 1; i <= x + 1; i++)
                    {
                        for (int j = y - 1; j <= y + 1; j++)
                        {
                            if (i == x && j == y) continue;
                            if (i < 0 || j < 0 || i >= BoardSize.Item1 || j >= BoardSize.Item2) continue;

                            if (playBoard.TryGetValue((i, j), out MinesweeperTile neighbor) && neighbor.IsBomb)
                                count++;
                        }
                    }

                    return count;
                }
            }
        }
    }
    #endregion
    #region Draw Board UI
    public partial class Minesweeper
    {
        private static float TileSize = 24f;
        private static bool IsSettings;
        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Repaint();
            }
            void DrawToolbar()
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);

                if (GUILayout.Button("Restart", EditorStyles.toolbarButton))
                {
                    ResetBoard();
                    GUI.FocusControl(null);
                }
                if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
                {
                    IsSettings = !IsSettings;
                    GUI.FocusControl(null);
                }

                GUILayout.FlexibleSpace();
                if (GameWin) GUILayout.Label("Y'oure Winner!", EditorStyles.toolbarButton);
                GUILayout.Label(GameStateText, EditorStyles.toolbarButton);
                GUILayout.EndHorizontal();
            }

            DrawToolbar();
            if (playBoard == null || playBoard.Count == 0)
            {
                if (GUILayout.Button("Build Board"))
                {
                    ResetBoard();
                }
                return;
            }
            if (!DrawSettings())
            {
                DrawBoard();
            }
        }
        private void DrawBoard()
        {
            for (int y = 0; y < BoardSize.Item2; y++)
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < BoardSize.Item1; x++)
                {
                    if (!playBoard.TryGetValue((x, y), out MinesweeperTile tile))
                        continue;

                    Rect rect = GUILayoutUtility.GetRect(TileSize, TileSize, GUILayout.Width(TileSize), GUILayout.Height(TileSize));
                    DrawTile(rect, tile);
                }
                GUILayout.EndHorizontal();
            }

            if (GUI.changed)
                Repaint();
        }
    }
    #endregion
    #region Draw Settings
    public partial class Minesweeper
    {
        private static int QuadraticTileSize = 10;
        private static int TargetBombCount = 10;

        private bool DrawSettings()
        {
            if (!IsSettings)
                return false;

            GUILayout.BeginVertical("box");
            GUILayout.Label("Board Settings", EditorStyles.boldLabel);

            int newSize = EditorGUILayout.IntSlider("Board Size", QuadraticTileSize, 10, 32);
            if (newSize != QuadraticTileSize)
            {
                QuadraticTileSize = newSize;
            }

            int maxBombs = Mathf.Min((int)(QuadraticTileSize * QuadraticTileSize * 0.8f), 800);
            int newBombs = EditorGUILayout.IntSlider("Bombs", TargetBombCount, 1, maxBombs);
            if (newBombs != TargetBombCount)
            {
                TargetBombCount = newBombs;
            }

            if (GUILayout.Button("Apply"))
            {
                ResetBoard();
                IsSettings = false;
            }

            GUILayout.EndVertical();
            return true;
        }
    }
    #endregion
    #region Draw Tile & Click Tile
    public partial class Minesweeper
    {
        #region GUI Style
        Dictionary<int, GUIStyle> guiStyleLookup = new();
        private GUIStyle FetchGUIStyle(int stateHash, Color bgColor, Color? textColor = null)
        {
            int colorHash = bgColor.GetHashCode();
            int textColorHash = (textColor ?? Color.black).GetHashCode();
            int cacheKey = stateHash ^ colorHash ^ textColorHash;

            if (guiStyleLookup.TryGetValue(cacheKey, out GUIStyle style))
                return style;

            style = new GUIStyle(GUI.skin.button);

            int texSize = 3;
            Texture2D tex = new Texture2D(texSize, texSize);
            Color borderColor = Color.black;

            for (int x = 0; x < texSize; x++)
            {
                for (int y = 0; y < texSize; y++)
                {
                    if (x == 0 || y == 0 || x == texSize - 1 || y == texSize - 1)
                        tex.SetPixel(x, y, borderColor);
                    else
                        tex.SetPixel(x, y, bgColor);
                }
            }
            tex.Apply();

            style.normal.background = tex;
            style.hover.background = tex;
            style.active.background = tex;
            style.focused.background = tex;

            style.border = new RectOffset(1, 1, 1, 1);

            Color finalTextColor = textColor ?? Color.black;
            style.normal.textColor = finalTextColor;
            style.hover.textColor = finalTextColor;
            style.active.textColor = finalTextColor;
            style.focused.textColor = finalTextColor;

            guiStyleLookup[cacheKey] = style;
            return style;
        }
        #endregion

        private void DrawTile(Rect rect, MinesweeperTile tile)
        {
            string label = "";
            int nearbyBombs = tile.NearbyBombs;
            switch (tile.state)
            {
                case MinesweeperTile.State.None: break;
                case MinesweeperTile.State.Cleared:
                    label = nearbyBombs > 0 ? nearbyBombs.ToString() : "";
                    break;
                case MinesweeperTile.State.CorrectFlag: label = "F"; break;
                case MinesweeperTile.State.FalseFlag: label = "F"; break;
                case MinesweeperTile.State.BombBrick: break;
                case MinesweeperTile.State.BombTriggered: label = "X"; break;
                case MinesweeperTile.State.BombVisible: label = "B"; break;
                case MinesweeperTile.State.Brick: break;
                default: break;
            }

            GUIStyle fetchedStyle = GUIStyle.none;

            switch (tile.state)
            {
                case MinesweeperTile.State.None:
                    fetchedStyle = FetchGUIStyle(0, Color.gray4);
                    break;
                case MinesweeperTile.State.Cleared:
                    fetchedStyle = FetchGUIStyle(0, Color.gray4, nearbyBombs.MinesweeperDigitColor());
                    break;
                case MinesweeperTile.State.CorrectFlag:
                    fetchedStyle = FetchGUIStyle(2, Color.yellow);
                    break;
                case MinesweeperTile.State.FalseFlag:
                    fetchedStyle = FetchGUIStyle(2, Color.yellow);
                    break;
                case MinesweeperTile.State.BombBrick:
                    fetchedStyle = FetchGUIStyle(1, Color.white);
                    break;
                case MinesweeperTile.State.BombTriggered:
                    fetchedStyle = FetchGUIStyle(3, Color.paleVioletRed);
                    break;
                case MinesweeperTile.State.BombVisible:
                    fetchedStyle = GameWin ? FetchGUIStyle(4, Color.lightGreen) : FetchGUIStyle(3, Color.red);
                    break;
                case MinesweeperTile.State.Brick:
                    fetchedStyle = FetchGUIStyle(1, Color.white);
                    break;
                default:
                    break;
            }

            if (GUI.Button(rect, label, fetchedStyle))
            {
                int mouseButton = Event.current.button;
                if (mouseButton == 0) OnLeftClick(tile);
                if (mouseButton == 1) OnRightClick(tile);
                GUI.changed = true;
            }
        }
    }
    #endregion
    #region Click Tile Actions
    public partial class Minesweeper
    {
        private void OnLeftClick(MinesweeperTile tile)
        {
            if (!GameStarted)
            {
                GameStarted = true;
                IsPlaying = true;
                StartTime = EditorApplication.timeSinceStartup;
            }
            if (!IsPlaying)
            {
                return;
            }

            if (MovesCount == 0 && tile.IsBomb)
            {
                TryMoveBombFromTileToRandomTile(tile);
            }

            if (tile.state.HasFlag(MinesweeperTile.State.CorrectFlag) || tile.state.HasFlag(MinesweeperTile.State.FalseFlag))
                return;

            if (tile.state.HasFlag(MinesweeperTile.State.BombBrick))
            {
                MovesCount++;
                tile.UpdateState(MinesweeperTile.State.BombTriggered);
                RevealEntireBoardWithBomb(tile);
                IsPlaying = false;
                return;
            }

            if (tile.state.HasFlag(MinesweeperTile.State.Brick))
            {
                tile.UpdateState(MinesweeperTile.State.Cleared);
                MovesCount++;

                if (tile.NearbyBombs == 0)
                {
                    RevealNeighbors(tile);
                }
                else if (MovesCount == 1)
                {
                    RevealFirstEmptyNeighbor(tile);
                }
                if (RemainingClickableTiles <= 0)
                {
                    RevealEntireBoardWin();
                }
            }
        }
        private void OnRightClick(MinesweeperTile tile)
        {
            if (!IsPlaying)
                return;
            if (tile.state.HasFlag(MinesweeperTile.State.Cleared))
                return;

            if (tile.state.HasFlag(MinesweeperTile.State.CorrectFlag))
            {
                tile.UpdateState(MinesweeperTile.State.BombBrick);
                return;
            }

            if (tile.state.HasFlag(MinesweeperTile.State.FalseFlag))
            {
                tile.UpdateState(MinesweeperTile.State.Brick);
                return;
            }

            if (tile.IsBomb)
            {
                tile.UpdateState(MinesweeperTile.State.CorrectFlag);
            }
            else
            {
                tile.UpdateState(MinesweeperTile.State.FalseFlag);
            }
        }
        private void RevealNeighbors(MinesweeperTile tile)
        {
            int x = tile.tileXY.Item1;
            int y = tile.tileXY.Item2;

            for (int i = (x - 1).clamp(0, BoardSize.Item1 - 1); i <= (x + 1).clamp(0, BoardSize.Item1 - 1); i++)
            {
                for (int j = (y - 1).clamp(0, BoardSize.Item2 - 1); j <= (y + 1).clamp(0, BoardSize.Item2 - 1); j++)
                {
                    if ((i, j) == tile.tileXY)
                        continue;

                    if (playBoard.TryGetValue((i, j), out MinesweeperTile neighbor))
                    {
                        if (neighbor.state.HasFlag(MinesweeperTile.State.Brick) &&
                            !neighbor.state.HasFlag(MinesweeperTile.State.Cleared))
                        {
                            neighbor.UpdateState(MinesweeperTile.State.Cleared);
                            if (neighbor.NearbyBombs == 0)
                                RevealNeighbors(neighbor);
                        }
                    }
                }
            }
        }
        private void RevealFirstEmptyNeighbor(MinesweeperTile tile)
        {
            int x = tile.tileXY.Item1;
            int y = tile.tileXY.Item2;

            for (int i = (x - 1).clamp(0, BoardSize.Item1 - 1); i <= (x + 1).clamp(0, BoardSize.Item1 - 1); i++)
            {
                for (int j = (y - 1).clamp(0, BoardSize.Item2 - 1); j <= (y + 1).clamp(0, BoardSize.Item2 - 1); j++)
                {
                    if ((i, j) == tile.tileXY) continue;

                    if (playBoard.TryGetValue((i, j), out MinesweeperTile neighbor))
                    {
                        if (neighbor.state.HasFlag(MinesweeperTile.State.Brick) && neighbor.NearbyBombs == 0)
                        {
                            neighbor.UpdateState(MinesweeperTile.State.Cleared);
                            RevealNeighbors(neighbor);
                            return;
                        }
                    }
                }
            }
        }
        private void RevealEntireBoardWin()
        {
            if (!IsPlaying)
                return;

            foreach (var kvp in playBoard)
            {
                var tile = kvp.Value;
                if (tile.IsBomb)
                {
                    tile.UpdateState(MinesweeperTile.State.BombVisible);
                }
                else if (tile.state.HasFlag(MinesweeperTile.State.Brick))
                {
                    tile.UpdateState(MinesweeperTile.State.Cleared);
                }
            }
            IsPlaying = false;
            GameWin = true;
        }
        private void RevealEntireBoardWithBomb(MinesweeperTile bombedTile)
        {
            foreach (var kvp in playBoard)
            {
                var tile = kvp.Value;

                if (tile.IsBomb)
                {
                    tile.UpdateState(MinesweeperTile.State.BombVisible);
                }
                else if (tile.state.HasFlag(MinesweeperTile.State.Brick))
                {
                    tile.UpdateState(MinesweeperTile.State.Cleared);
                }
                bombedTile.UpdateState(MinesweeperTile.State.BombTriggered);
            }

            double duration = EditorApplication.timeSinceStartup - StartTime;
            cachedGameText = $"You're Loser! - Time : {duration:F2} - Moves : {MovesCount}";
        }
        private void TryMoveBombFromTileToRandomTile(MinesweeperTile tile)
        {
            if (!tile.IsBomb)
                return;

            tile.UpdateState(MinesweeperTile.State.Brick);

            int attempts = 50000;
            while (attempts > 0)
            {
                attempts--;

                if (!MinesweeperUtils.TryGetRandomTile(playBoard, BoardSize.Item1, BoardSize.Item2, out MinesweeperTile randomTile))
                    continue;

                if (randomTile.IsBomb || randomTile == tile)
                    continue;

                randomTile.PlaceBomb();
                break;
            }
            if (attempts <= 0)
            {
                Debug.LogWarning("Failed to move bomb to a random tile after 5000 attempts.");
            }
        }
    }
    #endregion
    public partial class Minesweeper : EditorWindow
    {
        #region Board Building Methods
        private void ResetBoard()
        {
            BuildBoard(QuadraticTileSize, TargetBombCount);
        }
        private void BuildBoard(int quadraticSize, int bombsToPlace)
        {
            TileSize = 640f / (float)quadraticSize;
            if (playBoard == null)
            {
                playBoard = new();
            }
            playBoard.Clear();
            MovesCount = 0;
            BoardSize = new(quadraticSize, quadraticSize);
            bombsToPlace = bombsToPlace.clamp(1, ((int)((quadraticSize * quadraticSize) * 0.8f)));
            for (int i = 0; i < quadraticSize; i++)
            {
                for (int j = 0; j < quadraticSize; j++)
                {
                    MinesweeperTile tile = new(MinesweeperTile.State.Brick);
                    tile.tileXY = new(i, j);
                    playBoard.Add((i, j), tile);
                }
            }
            int attempts = 50000;
            while (attempts > 0 && BombCount < bombsToPlace && MinesweeperUtils.TryGetRandomTile(playBoard, quadraticSize, quadraticSize, out MinesweeperTile result))
            {
                attempts--;
                if (!result.state.HasFlag(MinesweeperTile.State.BombBrick))
                {
                    result.PlaceBomb();
                }
            }
            if (attempts <= 0 && BombCount < bombsToPlace)
            {
                Debug.LogError("Failed to create board fully in 50000 moves.");
            }
            GameStarted = false;
            IsPlaying = true;
            StartTime = EditorApplication.timeSinceStartup;
            GameWin = false;
        }
        #endregion

        #region Window Open
        [MenuItem("Rensenware/Minesweeper")]
        public static void OpenWindow()
        {
            Minesweeper window = GetWindow<Minesweeper>();
            window.titleContent = new GUIContent("Minesweeper");
            window.minSize = new Vector2(640, 660);
            IsSettings = false;
            window.Show();
        }
        private void OnEnable()
        {
            ResetBoard();
        }
        #endregion

        static string cachedGameText;
        public static string GameStateText
        {
            get
            {
                if (!GameStarted)
                {
                    cachedGameText = "Built board. Happy playing! UˬU";
                    return cachedGameText;
                }
                if (IsPlaying)
                {
                    double duration = EditorApplication.timeSinceStartup - StartTime;
                    cachedGameText = $"Time : {duration:F2} - Moves : {MovesCount}";
                }
                return cachedGameText;
            }
        }

        public static bool GameStarted;
        public static bool GameWin;
        public static double StartTime;
        public static bool IsPlaying;
        public static int MovesCount = 0;
        public static int BombCount
        {
            get
            {
                if (playBoard == null || playBoard.Count == 0)
                {
                    return 0;
                }
                int bombCount = 0;
                foreach (var kvp in playBoard)
                {
                    if (kvp.Value.IsBomb)
                    {
                        bombCount += 1;
                    }
                }
                return bombCount;
            }
        }
        public static int RemainingClickableTiles
        {
            get
            {
                if (playBoard == null || playBoard.Count == 0)
                    return 0;

                int count = 0;
                foreach (var kvp in playBoard)
                {
                    var tile = kvp.Value;
                    if (tile.state.HasFlag(MinesweeperTile.State.Brick) ||
                        tile.state.HasFlag(MinesweeperTile.State.FalseFlag))
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public static Dictionary<(int, int), MinesweeperTile> playBoard = new();
        public static (int, int) BoardSize { get; private set; } = new(10, 10);
    }
}
#endif