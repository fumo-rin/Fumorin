using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace rinCore
{
    [CreateAssetMenu(menuName = "Fumocore/Scene Pack")]
    public class ScenePackSO : ScriptableObject
    {
        [Header("System Scenes")]
        [Tooltip("The bootstrapper/loader scene that will always be placed at Index 0 in Build Settings.")]
        [SerializeField] private SceneReference bootstrapperScene;

        [Tooltip("The persistent EventSystem/UI scene that stays permanently loaded across scene loads.")]
        [SerializeField] private SceneReference unloadPreventionScene;

        [Header("Starting Scenes")]
        [SerializeField] private ScenePairSO editorStartingScene;
        [SerializeField] private ScenePairSO buildStartingScene;
        [SerializeField] private bool loadStartingSceneInBuild;

        public SceneReference BootstrapperScene => bootstrapperScene;
        public SceneReference UnloadPreventionScene => unloadPreventionScene;
        public ScenePairSO EditorStartingScene => editorStartingScene;
        public ScenePairSO BuildStartingScene => buildStartingScene;
        public bool LoadStartingSceneInBuild => loadStartingSceneInBuild;

#if UNITY_EDITOR
        [Header("Scene Folders")]
        [Tooltip("Folders containing ScenePairSO assets to scan and sync.")]
        public List<DefaultAsset> scenePairSOFolders = new();

        public void AutoPopulateSceneLists()
        {
            Undo.RecordObject(this, "Auto Populate Scene Lists");
            EditorUtility.SetDirty(this);
            Debug.Log("[ScenePackSO] Scene lists refreshed from selected folders.");
        }

        public void SyncScenesToBuildSettings()
        {
            var buildScenes = new List<EditorBuildSettingsScene>();
            var addedPaths = new HashSet<string>();

            void AddSceneToBuild(SceneReference sr)
            {
                if (sr != null && sr.IsValid)
                {
                    string path = sr.ScenePath;
                    if (!string.IsNullOrEmpty(path) && !addedPaths.Contains(path))
                    {
                        buildScenes.Add(new EditorBuildSettingsScene(path, true));
                        addedPaths.Add(path);
                    }
                }
            }

            void AddPairToBuild(ScenePairSO pair)
            {
                if (pair == null) return;
                AddSceneToBuild(pair.MainScene);
                if (pair.AdditiveScenes != null)
                {
                    foreach (var add in pair.AdditiveScenes)
                        AddSceneToBuild(add);
                }
            }

            if (bootstrapperScene != null && bootstrapperScene.IsValid)
            {
                AddSceneToBuild(bootstrapperScene);
            }
            else
            {
                Debug.LogWarning("[ScenePackSO] No Bootstrapper Scene assigned! Index 0 will be the first scene found.");
            }

            AddSceneToBuild(unloadPreventionScene);
            AddPairToBuild(editorStartingScene);
            AddPairToBuild(buildStartingScene);
            foreach (var folder in scenePairSOFolders)
            {
                if (folder == null) continue;
                string folderPath = AssetDatabase.GetAssetPath(folder);
                if (!AssetDatabase.IsValidFolder(folderPath)) continue;

                string[] guids = AssetDatabase.FindAssets("t:ScenePairSO", new[] { folderPath });
                foreach (var guid in guids)
                {
                    var so = AssetDatabase.LoadAssetAtPath<ScenePairSO>(AssetDatabase.GUIDToAssetPath(guid));
                    AddPairToBuild(so);
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"[ScenePackSO] Successfully synced {buildScenes.Count} scene(s) to Build Settings.");
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ScenePackSO))]
    public class ScenePackEditor : Editor
    {
        private readonly List<(string sceneName, string packName, SceneAsset asset, string path)> _cachedScenes = new();
        private double _lastScanTime = 0;
        private const double ScanInterval = 2.0;

        private void OnEnable()
        {
            RefreshSceneList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (Event.current.type == EventType.Layout && EditorApplication.timeSinceStartup - _lastScanTime > ScanInterval)
            {
                RefreshSceneList();
            }

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

            serializedObject.ApplyModifiedProperties();
        }

        private void RefreshSceneList()
        {
            _lastScanTime = EditorApplication.timeSinceStartup;
            _cachedScenes.Clear();

            var pack = (ScenePackSO)target;
            if (pack == null) return;

            HashSet<string> seenScenePaths = new();

            void AddSingleScene(SceneReference sr, string label)
            {
                if (sr != null && sr.IsValid)
                {
                    string path = sr.ScenePath;
                    if (!seenScenePaths.Contains(path))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                        if (asset != null)
                        {
                            seenScenePaths.Add(path);
                            _cachedScenes.Add((sr.GetSceneName(), label, asset, path));
                        }
                    }
                }
            }

            AddSingleScene(pack.BootstrapperScene, "BOOTSTRAPPER");
            AddSingleScene(pack.UnloadPreventionScene, "PERSISTENT");

            if (pack.scenePairSOFolders == null) return;

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
                        if (sr == null || !sr.IsValid) continue;

                        string path = sr.ScenePath;
                        if (string.IsNullOrEmpty(path) || seenScenePaths.Contains(path)) continue;

                        var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                        if (asset == null) continue;

                        seenScenePaths.Add(path);
                        _cachedScenes.Add((sr.GetSceneName(), so.name, asset, path));
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

            foreach (var entry in _cachedScenes.OrderBy(e => GetPriority(e.packName)).ThenBy(e => e.sceneName))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(entry.asset);
                }

                string tagText = entry.packName switch
                {
                    "BOOTSTRAPPER" => "<color=#FFD700>(BOOTSTRAPPER)</color>",
                    "PERSISTENT" => "<color=#00FFFF>(PERSISTENT)</color>",
                    _ => $"<color=#7F7F7F>(from {entry.packName})</color>"
                };

                string labelText = $"• {entry.sceneName} {tagText}";

                if (GUILayout.Button(labelText, linkStyle))
                {
                    if (!string.IsNullOrEmpty(entry.path) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                    }
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
        }

        private int GetPriority(string packName)
        {
            if (packName == "BOOTSTRAPPER") return 0;
            if (packName == "PERSISTENT") return 1;
            return 2;
        }
    }
#endif
}