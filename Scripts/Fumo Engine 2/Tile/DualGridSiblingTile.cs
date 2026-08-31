using System;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;

namespace rinCore
{
    [CustomEditor(typeof(DualGridSiblingTile))]
    [CanEditMultipleObjects]
    public class DualGridSiblingTileEditor : Editor
    {
        private SerializedProperty dualSpritesProp;
        private SerializedProperty defaultSpriteProp;
        private bool showDualGridMatrix = true;

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
            defaultSpriteProp = serializedObject.FindProperty("m_DefaultSprite");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(defaultSpriteProp, new GUIContent("Default Sprite"));
            EditorGUILayout.Space(10);

            if (dualSpritesProp != null)
            {
                if (dualSpritesProp.arraySize != 16)
                {
                    dualSpritesProp.arraySize = 16;
                }

                showDualGridMatrix = EditorGUILayout.BeginFoldoutHeaderGroup(showDualGridMatrix, "Dual Grid 4x4 Sprite Matrix");
                if (showDualGridMatrix)
                {
                    EditorGUI.indentLevel++;
                    DrawSpriteMatrixGUI();
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSpriteMatrixGUI()
        {
            const float boxSize = 58f;
            const float padding = 4f;

            EditorGUILayout.Space(4);
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

            EditorGUILayout.Space(4);
        }
    }
}
#endif

namespace rinCore
{
    [CreateAssetMenu(menuName = "rinCore/Tile/Dual Grid Sibling Tile")]
    public class DualGridSiblingTile : TileBase
    {
        [Header("Dual Grid Settings")]
        public Sprite m_DefaultSprite;

        [Tooltip("Assign ripped tile 0 through 15 in sequential order")]
        public Sprite[] dualSprites = new Sprite[16];

        // Direct bitmask lookup mapping 4 corner states to your exact Inspector GridIndexMatrix:
        // Bit 0 (1) : Top-Left     ( 0,  0)
        // Bit 1 (2) : Top-Right    ( 1,  0)
        // Bit 2 (4) : Bottom-Left  ( 0, -1)
        // Bit 3 (8) : Bottom-Right ( 1, -1)
        private static readonly int[] DualBitmaskToSpriteIndex = new int[16]
        {
            12, // 0000 (0)  : None                -> [12] Empty
             0, // 0001 (1)  : TL                  -> [0]  Outer Corner TL
             3, // 0010 (2)  : TR                  -> [3]  Outer Corner TR
             2, // 0011 (3)  : TL + TR             -> [2]  Top Edge
            13, // 0100 (4)  : BL                  -> [13] Outer Corner BL
             1, // 0101 (5)  : TL + BL             -> [1]  Left Edge
            14, // 0110 (6)  : TR + BL             -> [14] Diagonal TR/BL
            15, // 0111 (7)  : TL + TR + BL        -> [15] Inner Corner missing BR
             8, // 1000 (8)  : BR                  -> [8]  Outer Corner BR
             4, // 1001 (9)  : TL + BR             -> [4]  Diagonal TL/BR
            11, // 1010 (10) : TR + BR             -> [11] Right Edge
             7, // 1011 (11) : TL + TR + BR        -> [7]  Inner Corner missing BL
             9, // 1100 (12) : BL + BR             -> [9]  Bottom Edge
            10, // 1101 (13) : TL + BL + BR        -> [10] Inner Corner missing TR
             5, // 1110 (14) : TR + BL + BR        -> [5]  Inner Corner missing TL
             6  // 1111 (15) : All 4               -> [6]  Full Solid
        };

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            base.GetTileData(position, tilemap, ref tileData);

            if (dualSprites != null && dualSprites.Length >= 16)
            {
                int spriteIdx = CalculateDualIndex(position, tilemap);
                tileData.sprite = dualSprites[spriteIdx] != null ? dualSprites[spriteIdx] : m_DefaultSprite;
            }
            else
            {
                tileData.sprite = m_DefaultSprite;
            }
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            // Refresh 3x3 local cluster so adjacent cells recalculate their corner bitmasks automatically
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int neighborPos = position + new Vector3Int(x, y, 0);
                    tilemap.RefreshTile(neighborPos);
                }
            }
        }

        private int CalculateDualIndex(Vector3Int pos, ITilemap tilemap)
        {
            // Sample forward 2x2 corner quad relative to current vertex cell position
            int tl = IsMatch(pos + new Vector3Int(0, 0, 0), tilemap) ? (1 << 0) : 0;
            int tr = IsMatch(pos + new Vector3Int(1, 0, 0), tilemap) ? (1 << 1) : 0;
            int bl = IsMatch(pos + new Vector3Int(0, -1, 0), tilemap) ? (1 << 2) : 0;
            int br = IsMatch(pos + new Vector3Int(1, -1, 0), tilemap) ? (1 << 3) : 0;

            int bitmask = tl | tr | bl | br;
            return DualBitmaskToSpriteIndex[bitmask];
        }

        private bool IsMatch(Vector3Int localPos, ITilemap tilemap)
        {
            TileBase other = tilemap.GetTile(localPos);
            return other != null;
        }
    }
}