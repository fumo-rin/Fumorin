#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using rinCore;

#if UNITY_EDITOR
[CustomEditor(typeof(ScenePackSO))]
public class ScenePackEditor : Editor
{
    private readonly List<(string sceneName, string packName, SceneAsset asset)> _cachedScenes = new();
    private double _lastScanTime = 0;
    private const double ScanInterval = 1.0;

    private void OnEnable()
    {
        RefreshSceneList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.Space(5);
        if (GUILayout.Button("Auto-Populate Scene Lists from Folders", GUILayout.Height(25)))
        {
            ((ScenePackSO)target).AutoPopulateSceneLists();
            RefreshSceneList();
        }
        if (GUILayout.Button("Sync Build Settings From Scene Lists", GUILayout.Height(25)))
        {
            ((ScenePackSO)target).SyncScenesToBuildSettings();
        }

        GUILayout.Space(15);
        DrawSceneOverview();
        GUILayout.Space(15);

        DrawPropertiesExcluding(serializedObject, "m_Script");

        if (EditorApplication.timeSinceStartup - _lastScanTime > ScanInterval)
        {
            RefreshSceneList();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void RefreshSceneList()
    {
        _lastScanTime = EditorApplication.timeSinceStartup;
        _cachedScenes.Clear();

        var pack = (ScenePackSO)target;
        if (pack.scenePairSOFolders == null) return;

        HashSet<string> seenScenePaths = new();

        foreach (var folder in pack.scenePairSOFolders)
        {
            if (folder == null) continue;

            string folderPath = AssetDatabase.GetAssetPath(folder);
            if (!AssetDatabase.IsValidFolder(folderPath)) continue;

            string[] guids = AssetDatabase.FindAssets("t:ScenePairSO", new[] { folderPath });

            foreach (string guid in guids)
            {
                var so = AssetDatabase.LoadAssetAtPath<ScenePairSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (so == null || so.Scenes == null) continue;

                foreach (var sr in so.Scenes)
                {
                    if (sr == null || sr.SceneAsset == null) continue;

                    string path = AssetDatabase.GetAssetPath(sr.SceneAsset);
                    if (string.IsNullOrEmpty(path)) continue;

                    if (seenScenePaths.Contains(path)) continue;
                    seenScenePaths.Add(path);

                    _cachedScenes.Add((sr.GetEditorSceneName(), so.name, sr.SceneAsset));
                }
            }
        }
    }

    private void DrawSceneOverview()
    {
        GUILayout.Label("Scenes in Scene Packs", EditorStyles.boldLabel);

        if (_cachedScenes.Count == 0)
        {
            EditorGUILayout.HelpBox("No scenes found in the selected ScenePairSO folders.", MessageType.Info);
            return;
        }

        GUIStyle linkStyle = new(EditorStyles.linkLabel) { richText = true };

        foreach (var entry in _cachedScenes.OrderBy(e => e.sceneName))
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Ping", GUILayout.Width(50)))
            {
                EditorGUIUtility.PingObject(entry.asset);
            }

            string labelText = $"• {entry.sceneName} <color=#7F7F7F>(from {entry.packName})</color>";
            Rect labelRect = GUILayoutUtility.GetRect(new GUIContent($"• {entry.sceneName} (from {entry.packName})"), linkStyle);

            if (GUI.Button(labelRect, labelText, linkStyle))
            {
                string path = AssetDatabase.GetAssetPath(entry.asset);
                if (!string.IsNullOrEmpty(path) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                }
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }
    }
}
#endif

[CreateAssetMenu(menuName = "Fumocore/Scene Pack")]
public class ScenePackSO : ScriptableObject
{
#if UNITY_EDITOR
    [Tooltip("Folders containing ScenePairSO assets to scan and sync.")]
    public List<DefaultAsset> scenePairSOFolders = new();

    public void AutoPopulateSceneLists()
    {
        Undo.RecordObject(this, "Auto Populate Scene Lists");
        EditorUtility.SetDirty(this);
        Debug.Log("[ScenePackSO] Scene lists auto-populated from selected folders.");
    }

    public void SyncScenesToBuildSettings()
    {
        var buildScenes = new List<EditorBuildSettingsScene>();
        var addedPaths = new HashSet<string>();

        foreach (var folder in scenePairSOFolders)
        {
            if (folder == null) continue;
            string folderPath = AssetDatabase.GetAssetPath(folder);
            if (!AssetDatabase.IsValidFolder(folderPath)) continue;

            string[] guids = AssetDatabase.FindAssets("t:ScenePairSO", new[] { folderPath });
            foreach (var guid in guids)
            {
                var so = AssetDatabase.LoadAssetAtPath<ScenePairSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (so == null || so.Scenes == null) continue;

                foreach (var scene in so.Scenes)
                {
                    if (scene == null || scene.SceneAsset == null) continue;

                    string path = AssetDatabase.GetAssetPath(scene.SceneAsset);
                    if (string.IsNullOrEmpty(path) || addedPaths.Contains(path)) continue;

                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                    addedPaths.Add(path);
                }
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log($"[ScenePackSO] Synced {buildScenes.Count} scenes to Build Settings.");
    }
#endif
}