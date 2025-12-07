using UnityEngine;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace FumoCore.Tools
{
    public static class VersionManager
    {
        private static readonly string AssetsVersionPath = Path.Combine(Application.dataPath, "version.txt");

        public struct GameVersion
        {
            public string name;
            public string version;

            public GameVersion(string name, string version)
            {
                this.name = name;
                this.version = version;
            }

            public static implicit operator string(GameVersion v) => $"{v.name} v{v.version}";
        }
        public static GameVersion GetCurrentVersion()
        {
            return new GameVersion(Application.productName, Application.version);
        }

#if UNITY_EDITOR
        class AutoIncrementVersion : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report)
            {
                string gameName = PlayerSettings.productName;
                string version = LoadVersionFromFile(gameName);

                if (string.IsNullOrEmpty(version))
                {
                    Debug.LogWarning($"No version found for '{gameName}', defaulting to 1.0.0");
                    version = "1.0.0";
                }

                string newVersion = IncrementVersion(version);
                SaveVersionToFile(gameName, newVersion);

                PlayerSettings.bundleVersion = newVersion;

#if UNITY_ANDROID
            PlayerSettings.Android.bundleVersionCode += 1;
#endif

#if UNITY_IOS
            if (int.TryParse(PlayerSettings.iOS.buildNumber, out int buildNum))
                PlayerSettings.iOS.buildNumber = (buildNum + 1).ToString();
#endif

                Debug.Log($"[VersionManager] Updated version for '{gameName}' to {newVersion}");
                AssetDatabase.Refresh();
            }

            private string LoadVersionFromFile(string gameName)
            {
                if (!File.Exists(AssetsVersionPath)) return null;

                var lines = File.ReadAllLines(AssetsVersionPath);
                foreach (string line in lines)
                {
                    if (line.StartsWith(gameName + "="))
                        return line.Substring(gameName.Length + 1).Trim();
                }

                return null;
            }

            private void SaveVersionToFile(string gameName, string version)
            {
                var versions = new System.Collections.Generic.Dictionary<string, string>();

                if (File.Exists(AssetsVersionPath))
                {
                    foreach (string line in File.ReadAllLines(AssetsVersionPath))
                    {
                        if (line.Contains("="))
                        {
                            var split = line.Split('=');
                            if (split.Length == 2)
                                versions[split[0].Trim()] = split[1].Trim();
                        }
                    }
                }

                versions[gameName] = version;
                var linesOut = versions.Select(kv => $"{kv.Key}={kv.Value}").ToArray();
                File.WriteAllLines(AssetsVersionPath, linesOut);
            }

            private string IncrementVersion(string version)
            {
                var parts = version.Split('.');
                if (parts.Length == 0) return "1.0.0";

                if (int.TryParse(parts[^1], out int patch))
                {
                    patch++;
                    parts[^1] = patch.ToString();
                    return string.Join(".", parts);
                }

                return "1.0.0";
            }
        }
#endif
    }
}