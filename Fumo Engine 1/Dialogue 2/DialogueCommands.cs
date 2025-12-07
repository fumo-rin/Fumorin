using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;

namespace Fumorin
{
    using FumoCore.Tools;
    #region Validator
    using System.IO;
    using UnityEditor;
    public static class DialogueValidator
    {
#if UNITY_EDITOR
        [MenuItem("Fumorin/Validate All Shmup Dialogue Assets")]
        [Initialize(999)]
        public static void ValidateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:ShmupDialogueSO");
            int errorCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DialogueStackSO asset = AssetDatabase.LoadAssetAtPath<DialogueStackSO>(path);

                if (asset == null)
                    continue;
                if (asset.GetAllCommands(out HashSet<string> commands))
                {
                    foreach (var item in commands)
                    {
                        if (!Fumorin.ShmupCommands.HasCommand(item))
                        {
                            Debug.LogError($"Invalid command '{item}' in asset: {path}", asset);
                            if (asset != null && asset.dialogueTextFile) asset.dialogueTextFile.EditorPing();
                            errorCount++;
                        }
                    }
                }
            }

            if (errorCount == 0)
            {
                Debug.Log("All ShmupDialogueSO assets are valid.");
            }
            else
            {
                Debug.LogWarning($"Validation completed with {errorCount} invalid command(s).");
            }
        }
#endif
    }
    #endregion
    #region Attribute & Command Registry
    [AttributeUsage(AttributeTargets.Method)]
    public class DialogueCommandAttribute : Attribute
    {
        public string Name { get; }
        public DialogueCommandAttribute(string name) => Name = name;
    }
    public static class ShmupCommands
    {
        private static readonly Dictionary<string, Action> commandMap = new();
        [Initialize(-100)]
        public static void LoadAll()
        {
            commandMap.Clear();

            var methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                })
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.GetCustomAttribute<DialogueCommandAttribute>() != null);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<DialogueCommandAttribute>();

                if (method.GetParameters().Length == 0 && method.ReturnType == typeof(void))
                {
                    var del = (Action)Delegate.CreateDelegate(typeof(Action), method);
                    commandMap[attr.Name] = del;
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Invalid DialogueCommand method '{method.Name}' — must be 'static void Method()'");
                }
            }
        }
        public static bool TryRun(string commandName)
        {
            if (commandMap.TryGetValue(commandName, out var action))
            {
                try
                {
                    action.Invoke();
                    return true;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Error executing command '{commandName}': {e.Message}");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Unknown command: {commandName}");
            }

            return false;
        }
        public static bool HasCommand(string command)
        {
            if (!commandMap.TryGetValue(command, out var action))
            {
                Debug.LogWarning($"Unknown Command: {command}");
                return false;
            }
            return true;
        }
    }
    #endregion
    public static class TestCommands
    {
        [DialogueCommand("Test")]
        public static void Test()
        {
            GeneralManager.FunnyExplosion(Vector2.zero, 3f);
        }
    }
}
