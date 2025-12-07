#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FumoCore.Tools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;

[CustomEditor(typeof(ScenePackSO))]
public class ScenePackEditor : Editor
{
    private SerializedProperty scenePairFoldersProp;

    private readonly List<(string sceneName, string packName, SceneAsset asset)> cachedScenes = new();

    private double lastScanTime = 0;
    private const double scanInterval = 1.0;

    private void OnEnable()
    {
        scenePairFoldersProp = serializedObject.FindProperty("scenePairSOFolders");
        RefreshSceneList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        GUILayout.Space(5);
        if (GUILayout.Button("Auto-Populate Scene Lists from Folders"))
        {
            ((ScenePackSO)target).AutoPopulateSceneLists();
            RefreshSceneList();
        }
        if (GUILayout.Button("Sync Build Settings From Scene Lists"))
        {
            ((ScenePackSO)target).SyncScenesToBuildSettings();
        }
        GUILayout.Space(10);
        DrawSceneOverview();
        GUILayout.Space(20);

        base.OnInspectorGUI();
        if (EditorApplication.timeSinceStartup - lastScanTime > scanInterval)
            RefreshSceneList();
        serializedObject.ApplyModifiedProperties();
    }

    private void RefreshSceneList()
    {
        lastScanTime = EditorApplication.timeSinceStartup;

        cachedScenes.Clear();
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
                    if (sr == null || sr.sceneAsset == null) continue;

                    string path = AssetDatabase.GetAssetPath(sr.sceneAsset);
                    if (string.IsNullOrEmpty(path)) continue;

                    if (seenScenePaths.Contains(path)) continue;
                    seenScenePaths.Add(path);

                    cachedScenes.Add((sr.GetSceneName(), so.name, sr.sceneAsset));
                }
            }
        }
    }
    private void DrawSceneOverview()
    {
        GUILayout.Label("Scenes in Scene Packs", EditorStyles.boldLabel);

        if (cachedScenes.Count == 0)
        {
            EditorGUILayout.HelpBox("No scenes found in the selected ScenePairSO folders.", MessageType.Info);
            return;
        }

        foreach (var entry in cachedScenes.OrderBy(e => e.sceneName))
        {
            EditorGUILayout.BeginHorizontal();

            GUIStyle linkStyle = new(EditorStyles.linkLabel)
            {
                richText = true,
            };

            if (GUILayout.Button("Ping", GUILayout.Width(50)))
            {
                EditorGUIUtility.PingObject(entry.asset);
            }
            Rect labelRect = GUILayoutUtility.GetRect(
                new GUIContent($"• {entry.sceneName} (from {entry.packName})"),
                linkStyle
            );

            if (GUI.Button(labelRect, $"• {entry.sceneName} (from {entry.packName})", linkStyle))
            {
                string path = AssetDatabase.GetAssetPath(entry.asset);
                if (!string.IsNullOrEmpty(path))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
[CreateAssetMenu(menuName = "Fumocore/Scene Pack")]
public class ScenePackSO : ScriptableObject
{
#if UNITY_EDITOR
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
                    if (scene.sceneAsset == null) continue;
                    string path = AssetDatabase.GetAssetPath(scene.sceneAsset);
                    if (string.IsNullOrEmpty(path) || addedPaths.Contains(path)) continue;

                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                    addedPaths.Add(path);
                }
            }
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log($"[ScenePackSO] Synced {buildScenes.Count} scenes (including ScenePairSO scenes) to Build Settings.");
    }
    private List<SceneReference> FindScenes(string[] folders)
    {
        var guids = AssetDatabase.FindAssets("t:Scene", folders);
        var sceneRefs = new List<SceneReference>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".unity")) continue;

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (sceneAsset == null) continue;

            if (!sceneRefs.Any(sr => sr.sceneAsset == sceneAsset))
            {
                sceneRefs.Add(new SceneReference { sceneAsset = sceneAsset });
            }
        }

        return sceneRefs;
    }
#endif
}
