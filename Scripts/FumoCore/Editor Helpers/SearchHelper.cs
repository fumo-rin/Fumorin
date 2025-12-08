#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

public class SearchHelper : EditorWindow
{
    private const string StorageKey = "SearchHelper_List";
    private List<string> searchList = new();
    private Vector2 scroll;
    private string newSearchText = "";

    [MenuItem("Fumorin/Search Helper")]
    public static void ShowWindow()
    {
        var window = GetWindow<SearchHelper>();
        window.titleContent = new GUIContent("Search Helper");
        window.Show();
    }

    private void OnEnable()
    {
        LoadList();
    }

    private void OnDisable()
    {
        SaveList();
    }

    private void OnGUI()
    {
        GUILayout.Space(5);

        // Add new entry field
        GUILayout.BeginHorizontal();
        newSearchText = EditorGUILayout.TextField(newSearchText);
        if (GUILayout.Button("Add", GUILayout.Width(50)))
        {
            if (!string.IsNullOrWhiteSpace(newSearchText))
            {
                if (!searchList.Contains(newSearchText))
                {
                    searchList.Add(newSearchText);
                    SaveList();
                }
                newSearchText = "";
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // List display
        EditorGUILayout.LabelField("Saved Searches", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < searchList.Count; i++)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(searchList[i], EditorStyles.linkLabel))
            {
                OpenSearch(searchList[i]);
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                searchList.RemoveAt(i);
                SaveList();
                GUI.FocusControl(null);
                break;
            }

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        GUILayout.FlexibleSpace();

        // Clear all
        if (GUILayout.Button("Clear All"))
        {
            if (EditorUtility.DisplayDialog("Clear Search List",
                "Are you sure you want to clear all saved search shortcuts?", "Yes", "No"))
            {
                searchList.Clear();
                SaveList();
            }
        }
    }
    private void OpenSearch(string query)
    {
        var context = SearchService.CreateContext(new[] { "asset", "scene" }, query);

        if (!context.providers.Any())
            context = SearchService.CreateContext(new[] { "asset" }, query);

        SearchService.ShowWindow(context, reuseExisting: true);
    }

    private void LoadList()
    {
        string projectKey = Application.dataPath.GetHashCode() + StorageKey;
        string saved = EditorPrefs.GetString(projectKey, "");
        searchList.Clear();
        if (!string.IsNullOrEmpty(saved))
        {
            searchList.AddRange(saved.Split('|'));
        }
    }

    private void SaveList()
    {
        string projectKey = Application.dataPath.GetHashCode() + StorageKey;
        string joined = string.Join("|", searchList);
        EditorPrefs.SetString(projectKey, joined);
    }
}
#endif
