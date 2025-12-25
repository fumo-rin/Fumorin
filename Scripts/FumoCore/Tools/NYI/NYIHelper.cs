using UnityEngine;
using System;
using System.Reflection;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace RinCore
{
    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    public class NYIAttribute : Attribute
    {
        public virtual string Category { get; private set; } = "Default";
        public NYIAttribute() { }
        public NYIAttribute(string category)
        {
            Category = category;
        }
    }
    public class NYIUntested : NYIAttribute
    {
        public override string Category { get { return "Untested"; } }
    }
    public class NYITestingAttribute : NYIAttribute
    {
        public override string Category { get { return "Testing"; } }
    }
#if UNITY_EDITOR
    public static class NYIHelper
    {
        public class NYIDebugWindow : EditorWindow
        {
            private Vector2 scrollPos;
            private Dictionary<string, List<NYIEntry>> nyiEntriesByCategory = new Dictionary<string, List<NYIEntry>>();
            private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

            [MenuItem("Fumorin/NYI Debugger")]
            public static void ShowWindow() => GetWindow<NYIDebugWindow>("NYI Debugger");

            private void OnGUI()
            {
                GUILayout.Label("NYI Debug Logger", EditorStyles.boldLabel);

                if (GUILayout.Button("Scan All NYI Items"))
                {
                    ScanAllNYI();
                }

                GUILayout.Space(10);

                scrollPos = GUILayout.BeginScrollView(scrollPos);

                // Sort categories alphabetically but put "Default" at the end
                var categories = nyiEntriesByCategory.Keys
                    .OrderBy(c => c == "Default" ? "ZZZZZZ" : c)
                    .ToList();

                foreach (var category in categories)
                {
                    if (!foldouts.ContainsKey(category))
                        foldouts[category] = true;

                    foldouts[category] = EditorGUILayout.Foldout(foldouts[category], category, true);
                    if (foldouts[category])
                    {
                        foreach (var entry in nyiEntriesByCategory[category])
                        {
                            GUIStyle style = entry.Member != null ? EditorStyles.miniButton : EditorStyles.label;

                            if (GUILayout.Button(entry.DisplayName, style, GUILayout.ExpandWidth(true)))
                            {
                                OpenScriptAtMember(entry);
                            }
                        }
                        GUILayout.Space(5);
                    }
                }

                GUILayout.EndScrollView();
            }

            private void ScanAllNYI()
            {
                nyiEntriesByCategory.Clear();

                // --- Fast: get all class-level NYI ---
                var classTypes = TypeCache.GetTypesWithAttribute<NYIAttribute>();
                foreach (var type in classTypes)
                {
                    var attr = type.GetCustomAttribute<NYIAttribute>(true);
                    AddEntry(attr.Category, new NYIEntry
                    {
                        Type = type,
                        Member = null,
                        DisplayName = $"NYI Class: {type.FullName}"
                    });
                }

                // --- Method-level NYI: check all types in the project ---
                var allTypes = TypeCache.GetTypesDerivedFrom<object>();
                foreach (var type in allTypes)
                {
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        var attr = method.GetCustomAttribute<NYIAttribute>(true);
                        if (attr != null)
                        {
                            AddEntry(attr.Category, new NYIEntry
                            {
                                Type = type,
                                Member = method,
                                DisplayName = $"NYI Method: {type.FullName}.{method.Name}"
                            });
                        }
                    }
                }

                Debug.Log($"NYI Scan complete: {nyiEntriesByCategory.Sum(kv => kv.Value.Count)} items found.");
            }

            private void AddEntry(string category, NYIEntry entry)
            {
                if (!nyiEntriesByCategory.ContainsKey(category))
                    nyiEntriesByCategory[category] = new List<NYIEntry>();

                nyiEntriesByCategory[category].Add(entry);
            }

            private void OpenScriptAtMember(NYIEntry entry)
            {
                MonoScript script = null;
                string[] guids = AssetDatabase.FindAssets("t:MonoScript");

                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    MonoScript s = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (s.GetClass() == entry.Type)
                    {
                        script = s;
                        break;
                    }
                }

                if (script != null)
                {
                    int line = 0;
                    if (entry.Member != null)
                    {
                        string path = AssetDatabase.GetAssetPath(script);
                        if (!string.IsNullOrEmpty(path))
                        {
                            string[] lines = System.IO.File.ReadAllLines(path);
                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (lines[i].Contains(entry.Member.Name))
                                {
                                    line = i + 1;
                                    break;
                                }
                            }
                        }
                    }
                    AssetDatabase.OpenAsset(script, line);
                }
                else
                {
                    Debug.LogWarning($"Could not find script for {entry.DisplayName}");
                }
            }

            private class NYIEntry
            {
                public Type Type;
                public MethodInfo Member;
                public string DisplayName;
            }
        }
    }
#endif
}
