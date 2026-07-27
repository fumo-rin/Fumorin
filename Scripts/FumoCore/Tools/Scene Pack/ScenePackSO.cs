using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
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
        private const string ActivePackPrefsKey = "ScenePackSO_LastSyncedGUID";

        [Header("Scene Folders")]
        [Tooltip("Folders containing ScenePairSO assets to scan and sync.")]
        public List<DefaultAsset> scenePairSOFolders = new();

        public void AutoPopulateSceneLists()
        {
            Undo.RecordObject(this, "Auto Populate Scene Lists");
            EditorUtility.SetDirty(this);
            Debug.Log("[ScenePackSO] Scene lists refreshed from selected folders.");
        }
        public void SetAsActiveAndSync(bool isActualBuildProcess = false)
        {
            if (!isActualBuildProcess)
            {
                string myGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
                if (!string.IsNullOrEmpty(myGuid))
                {
                    EditorPrefs.SetString(ActivePackPrefsKey, myGuid);
                }
            }

            SyncScenesToBuildSettings(isActualBuildProcess);
        }

        public static ScenePackSO GetLastSyncedPack()
        {
            string savedGuid = EditorPrefs.GetString(ActivePackPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(savedGuid)) return null;

            string path = AssetDatabase.GUIDToAssetPath(savedGuid);
            if (string.IsNullOrEmpty(path)) return null;

            return AssetDatabase.LoadAssetAtPath<ScenePackSO>(path);
        }

        public void SyncScenesToBuildSettings(bool isActualBuildProcess = false)
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

                if (isActualBuildProcess && !pair.IncludeInBuild) return;

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
            string modeText = isActualBuildProcess ? "Build Mode (Filtered)" : "Editor Mode (All Scenes)";
            Debug.Log($"[ScenePackSO] Successfully synced {buildScenes.Count} scene(s) to Build Settings from '{name}' ({modeText}).");
        }
#endif
    }

#if UNITY_EDITOR
    public class ScenePackBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var activePack = ScenePackSO.GetLastSyncedPack();
            if (activePack != null)
            {
                activePack.SyncScenesToBuildSettings(isActualBuildProcess: true);
            }
            else
            {
                Debug.LogWarning("[ScenePackSO] No active Scene Pack found in EditorPrefs! Building with current Build Settings.");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var activePack = ScenePackSO.GetLastSyncedPack();
            if (activePack != null)
            {
                activePack.SyncScenesToBuildSettings(isActualBuildProcess: false);
            }
        }
    }

    [CustomEditor(typeof(ScenePackSO))]
    public class ScenePackEditor : Editor
    {
        private readonly List<(string sceneName, string packName, SceneAsset asset, string path, ScenePairSO pairAsset)> _cachedScenes = new();
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

            var pack = (ScenePackSO)target;
            bool isActive = ScenePackSO.GetLastSyncedPack() == pack;

            if (isActive)
            {
                EditorGUILayout.HelpBox("This is currently the ACTIVE Scene Pack for Build Settings.", MessageType.Info);
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Auto-Populate Scene Lists from Folders", GUILayout.Height(25)))
            {
                pack.AutoPopulateSceneLists();
                RefreshSceneList();
            }

            GUI.backgroundColor = isActive ? new Color(0.4f, 0.85f, 0.4f) : Color.white;
            string buttonText = isActive ? "Sync Build Settings (Currently Active)" : "Set Active & Sync Build Settings";

            if (GUILayout.Button(buttonText, GUILayout.Height(28)))
            {
                pack.SetAsActiveAndSync(isActualBuildProcess: false);
            }
            GUI.backgroundColor = Color.white;

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
                            _cachedScenes.Add((sr.GetSceneName(), label, asset, path, null));
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
                        _cachedScenes.Add((sr.GetSceneName(), so.name, asset, path, so));
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

                if (entry.pairAsset != null)
                {
                    bool included = entry.pairAsset.IncludeInBuild;
                    GUI.backgroundColor = included ? new Color(0.4f, 0.85f, 0.4f) : new Color(0.85f, 0.4f, 0.4f);
                    string buttonLabel = included ? "In Build" : "Excluded";

                    if (GUILayout.Button(buttonLabel, GUILayout.Width(65)))
                    {
                        Undo.RecordObject(entry.pairAsset, "Toggle Include In Build");
                        entry.pairAsset.IncludeInBuild = !entry.pairAsset.IncludeInBuild;
                        EditorUtility.SetDirty(entry.pairAsset);
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUILayout.Space(69);
                }

                if (GUILayout.Button("Ping", GUILayout.Width(45)))
                {
                    EditorGUIUtility.PingObject(entry.asset);
                }

                string tagText = entry.packName switch
                {
                    "BOOTSTRAPPER" => "<color=#FFD700>(BOOTSTRAPPER)</color>",
                    "PERSISTENT" => "<color=#00FFFF>(PERSISTENT)</color>",
                    _ => $"<color=#7F7F7F>(from {entry.packName})</color>"
                };

                bool isExcluded = entry.pairAsset != null && !entry.pairAsset.IncludeInBuild;
                string displayName = isExcluded ? $"<color=#888888><s>{entry.sceneName}</s> [Build Excluded]</color>" : entry.sceneName;

                string labelText = $"• {displayName} {tagText}";

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