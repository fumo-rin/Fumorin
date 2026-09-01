using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;

namespace rinCore
{
    [CustomEditor(typeof(DualGridLogicMap))]
    [CanEditMultipleObjects]
    public class DualGridLogicMapEditor : Editor
    {
        private SerializedProperty dualSpritesProp;
        private SerializedProperty visualTilemapProp;
        private bool showMatrix = true;

        private static readonly int[,] GridIndexMatrix = new int[4, 4]
        {
            {  0,  1,  2,  3 },
            {  4,  5,  6,  7 },
            {  8,  9, 10, 11 },
            { 12, 13, 14, 15 }
        };

        private void OnEnable()
        {
            dualSpritesProp = serializedObject.FindProperty("dualSprites");
            visualTilemapProp = serializedObject.FindProperty("visualTilemap");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(visualTilemapProp, new GUIContent("Visual Tilemap"));
            EditorGUILayout.Space(10);

            if (dualSpritesProp != null)
            {
                if (dualSpritesProp.arraySize != 16)
                    dualSpritesProp.arraySize = 16;

                showMatrix = EditorGUILayout.BeginFoldoutHeaderGroup(showMatrix, "Dual Grid 4x4 Sprite Matrix");
                if (showMatrix)
                {
                    EditorGUI.indentLevel++;
                    DrawSpriteMatrixGUI();
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            EditorGUILayout.Space(10);

            DualGridLogicMap script = (DualGridLogicMap)target;
            if (GUILayout.Button("Force Rebuild Visual Grid", GUILayout.Height(30)))
            {
                script.ForceRebuildAll();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSpriteMatrixGUI()
        {
            const float boxSize = 58f;
            const float padding = 4f;

            EditorGUILayout.HelpBox("Assign ripped tile_0 through 15 in sequential order:", MessageType.Info);
            EditorGUILayout.Space(6);

            for (int row = 0; row < 4; row++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int col = 0; col < 4; col++)
                {
                    int spriteIndex = GridIndexMatrix[row, col];
                    SerializedProperty elementProp = dualSpritesProp.GetArrayElementAtIndex(spriteIndex);

                    Rect cellRect = GUILayoutUtility.GetRect(boxSize, boxSize + 16f, GUILayout.Width(boxSize), GUILayout.Height(boxSize + 16f));
                    GUI.Box(cellRect, GUIContent.none, EditorStyles.helpBox);

                    Rect spriteRect = new Rect(cellRect.x + padding, cellRect.y + padding, boxSize - (padding * 2), boxSize - (padding * 2));

                    EditorGUI.BeginChangeCheck();
                    UnityEngine.Object newSprite = EditorGUI.ObjectField(spriteRect, elementProp.objectReferenceValue, typeof(Sprite), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        elementProp.objectReferenceValue = newSprite;
                    }

                    Rect labelRect = new Rect(cellRect.x, cellRect.y + boxSize - 2f, boxSize, 16f);
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 9
                    };

                    GUI.Label(labelRect, $"[{spriteIndex}]", labelStyle);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(padding);
            }
        }
    }
}
#endif

namespace rinCore
{
    [ExecuteAlways]
    [RequireComponent(typeof(Tilemap))]
    public class DualGridLogicMap : MonoBehaviour
    {
        public Tilemap visualTilemap;
        public Sprite[] dualSprites = new Sprite[16];

        private Tilemap logicTilemap;

        // Bitmask Bit Values:
        // TL = 1 (bit 0), TR = 2 (bit 1), BL = 4 (bit 2), BR = 8 (bit 3)
        private static readonly int[] BitmaskToSprite = new int[16]
        {
            12, // 0000 (0)  : None           -> [12] Empty
             0, // 0001 (1)  : TL             -> [0]  Outer TL
             1, // 0010 (2)  : TR             -> [1]  Outer TR
             2, // 0011 (3)  : TL + TR        -> [2]  Top Edge
             3, // 0100 (4)  : BL             -> [3]  Outer BL
             4, // 0101 (5)  : TL + BL        -> [4]  Left Edge
            14, // 0110 (6)  : TR + BL        -> [14] Diag TR/BL
            11, // 0111 (7)  : TL + TR + BL   -> [11] Inner BL (missing BR)
             8, // 1000 (8)  : BR             -> [8]  Outer BR
            15, // 1001 (9)  : TL + BR        -> [15] Diag TL/BR
             9, // 1010 (10) : TR + BR        -> [9]  Right Edge
             5, // 1011 (11) : TL + TR + BR   -> [5]  Inner TL (missing BL)
            10, // 1100 (12) : BL + BR        -> [10] Bottom Edge
            13, // 1101 (13) : TL + BL + BR   -> [13] Inner BR (missing TR)
             6, // 1110 (14) : TR + BL + BR   -> [6]  Inner TR (missing TL)
             7  // 1111 (15) : All            -> [7]  Solid
        };

        private void OnEnable()
        {
            logicTilemap = GetComponent<Tilemap>();
            Tilemap.tilemapTileChanged += OnTilemapChanged;
        }

        private void OnDisable()
        {
            Tilemap.tilemapTileChanged -= OnTilemapChanged;
        }

        private void OnTilemapChanged(Tilemap changedMap, Tilemap.SyncTile[] syncTiles)
        {
            if (changedMap != logicTilemap || visualTilemap == null) return;

            foreach (var syncTile in syncTiles)
            {
                Vector3Int pos = syncTile.position;

                for (int x = 0; x <= 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                    {
                        UpdateVisualVertex(pos + new Vector3Int(x, y, 0));
                    }
                }
            }
        }

        public void UpdateVisualVertex(Vector3Int vertexPos)
        {
            if (dualSprites == null || dualSprites.Length < 16) return;

            // Sample the 4 grid cells sharing this vertex
            int tl = logicTilemap.HasTile(vertexPos + new Vector3Int(-1, 0, 0)) ? 1 : 0;
            int tr = logicTilemap.HasTile(vertexPos + new Vector3Int(0, 0, 0)) ? 2 : 0;
            int bl = logicTilemap.HasTile(vertexPos + new Vector3Int(-1, -1, 0)) ? 4 : 0;
            int br = logicTilemap.HasTile(vertexPos + new Vector3Int(0, -1, 0)) ? 8 : 0;

            int bitmask = tl | tr | bl | br;
            int spriteIndex = BitmaskToSprite[bitmask];

            if (bitmask == 0)
            {
                visualTilemap.SetTile(vertexPos, null);
            }
            else
            {
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = dualSprites[spriteIndex];
                visualTilemap.SetTile(vertexPos, tile);
            }
        }

        public void ForceRebuildAll()
        {
            if (logicTilemap == null || visualTilemap == null) return;
            visualTilemap.ClearAllTiles();

            BoundsInt bounds = logicTilemap.cellBounds;
            for (int x = bounds.xMin - 1; x <= bounds.xMax + 1; x++)
            {
                for (int y = bounds.yMin - 1; y <= bounds.yMax + 1; y++)
                {
                    UpdateVisualVertex(new Vector3Int(x, y, 0));
                }
            }
        }
    }
}