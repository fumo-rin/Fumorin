using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FumoCore.Tools
{
    // --- Attribute for static fields ---
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class ExposedStaticAttribute : Attribute
    {
        public string Category { get; }
        public ExposedStaticAttribute(string category = "Default") => Category = category;
    }

    // --- Database ScriptableObject ---
    public class StaticValuesTool : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public string category;
            public UnityEngine.Object unityObject;
            public string jsonValue;
        }

        [SerializeField] private List<Entry> entries = new();

        public bool TryGet<T>(string id, out T val)
        {
            val = default;
            var e = entries.FirstOrDefault(x => x.id == id);
            if (e == null) return false;

            if (e.unityObject != null && e.unityObject is T obj)
            {
                val = obj;
                return true;
            }

            if (!string.IsNullOrEmpty(e.jsonValue))
            {
                try
                {
                    val = JsonUtility.FromJson<T>(e.jsonValue);
                    return true;
                }
                catch { return false; }
            }

            return false;
        }

        public void Set(string id, string category, object val)
        {
            var e = entries.FirstOrDefault(x => x.id == id);
            if (e == null)
            {
                e = new Entry { id = id, category = category };
                entries.Add(e);
            }

            e.category = category;

            if (val is UnityEngine.Object obj)
            {
                e.unityObject = obj;
                e.jsonValue = null;
            }
            else
            {
                e.unityObject = null;
                e.jsonValue = JsonUtility.ToJson(val);
            }
        }

        public IEnumerable<Entry> GetAll() => entries;
    }

    // --- Runtime accessor ---
    public static class StaticValueRuntime
    {
        private const string ResourcesPath = "StaticValueDatabase";
        private static StaticValuesTool _database;

        public static StaticValuesTool Database
        {
            get
            {
                if (_database == null)
                {
                    _database = Resources.Load<StaticValuesTool>(ResourcesPath);
                    if (_database == null)
                        Debug.LogError($"[StaticValues] Database missing at Resources/{ResourcesPath}.asset");
                }
                return _database;
            }
        }

        public static bool TryGet<T>(string id, out T val)
        {
            val = default;
            return Database != null && Database.TryGet(id, out val);
        }

        public static void Set(string id, string category, object val)
        {
            Database?.Set(id, category, val);
        }

        // Restore all statics at runtime
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RestoreStatics()
        {
            RestoreStaticsInternal();
        }

#if UNITY_EDITOR
        // Restore statics after domain reload
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            RestoreStaticsInternal();
        }
#endif

        private static void RestoreStaticsInternal()
        {
            var db = Database;
            if (db == null) return;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in asm.GetTypes())
                {
                    foreach (var f in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        var attr = f.GetCustomAttribute<ExposedStaticAttribute>();
                        if (attr == null) continue;

                        string id = t.FullName + "." + f.Name;
                        if (db.TryGet<object>(id, out var val))
                            f.SetValue(null, val);
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    // --- Editor: ensure database exists ---
    [InitializeOnLoad]
    public static class StaticValueEditorLoader
    {
        private const string AssetPath = "Assets/Resources/StaticValueDatabase.asset";

        static StaticValueEditorLoader()
        {
            if (StaticValueRuntime.Database != null && AssetDatabase.LoadAssetAtPath<StaticValuesTool>(AssetPath) != null)
                return;

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var db = ScriptableObject.CreateInstance<StaticValuesTool>();
            AssetDatabase.CreateAsset(db, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StaticValues] Created database at: " + AssetPath);
        }
    }

    // --- Editor Window ---
    public class StaticValuesEditor : EditorWindow
    {
        private class FieldEntry
        {
            public FieldInfo field;
            public Type type;
            public ExposedStaticAttribute attribute;
        }

        private List<FieldEntry> fields = new();
        private Vector2 scroll;
        private string search = "";
        private int categoryIndex = 0;
        private string[] categories;

        private StaticValuesTool database => StaticValueRuntime.Database;

        [MenuItem("Fumorin/Static Values Editor")]
        private static void Open() => GetWindow<StaticValuesEditor>("Static Values");

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            fields.Clear();
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                });

            foreach (var t in types)
            {
                foreach (var f in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attr = f.GetCustomAttribute<ExposedStaticAttribute>();
                    if (attr != null)
                        fields.Add(new FieldEntry { field = f, type = t, attribute = attr });
                }
            }

            categories = fields.Select(f => f.attribute.Category).Distinct().OrderBy(c => c).ToArray();
            categoryIndex = 0;
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh")) Refresh();
            GUILayout.Label("Search:");
            search = GUILayout.TextField(search);
            GUILayout.EndHorizontal();

            if (categories.Length > 0)
                categoryIndex = EditorGUILayout.Popup("Category", categoryIndex, categories);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var entry in fields)
            {
                if (!string.IsNullOrEmpty(search) &&
                    !(entry.field.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                      entry.type.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

                if (categories.Length > 0 && entry.attribute.Category != categories[categoryIndex])
                    continue;

                DrawField(entry);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawField(FieldEntry entry)
        {
            EditorGUILayout.BeginVertical("box");

            string id = entry.type.FullName + "." + entry.field.Name;
            EditorGUILayout.LabelField($"{entry.type.Name}.{entry.field.Name} [{entry.attribute.Category}]",
                EditorStyles.boldLabel);

            object value = entry.field.GetValue(null);
            Type fieldType = entry.field.FieldType;

            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                UnityEngine.Object obj = value as UnityEngine.Object;
                var newObj = EditorGUILayout.ObjectField(fieldType.Name, obj, fieldType, false);
                if (newObj != obj) value = newObj;
            }
            else if (value == null)
            {
                try { value = Activator.CreateInstance(fieldType); } catch { value = null; }
            }
            else
            {
                var wrapperType = typeof(TempSO<>).MakeGenericType(fieldType);
                ScriptableObject wrapper = ScriptableObject.CreateInstance(wrapperType);
                wrapperType.GetField("value").SetValue(wrapper, value);
                SerializedObject so = new SerializedObject(wrapper);
                SerializedProperty sp = so.FindProperty("value");
                if (sp != null)
                {
                    so.Update();
                    EditorGUILayout.PropertyField(sp, new GUIContent(fieldType.Name), true);
                    if (so.ApplyModifiedProperties())
                        value = wrapperType.GetField("value").GetValue(wrapper);
                }
                DestroyImmediate(wrapper);
            }

            entry.field.SetValue(null, value);
            database.Set(id, entry.attribute.Category, value);
            EditorUtility.SetDirty(database);

            EditorGUILayout.EndVertical();
        }

        private class TempSO<T> : ScriptableObject { public T value; }
    }
#endif
}
