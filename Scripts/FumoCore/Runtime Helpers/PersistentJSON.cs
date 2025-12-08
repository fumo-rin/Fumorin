using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Text;

#region JSON Playerprefs Alternate during WEBGL
public static partial class PersistentJSON
{
    private static bool IsWebGLBuild =>
        Application.platform == RuntimePlatform.WebGLPlayer;
    private static bool TrySaveWebGL<T>(T saveItem, string key, string json)
    {
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        if (DebugMode)
            Debug.Log($"[WebGL] Saved {typeof(T).Name} to PlayerPrefs key '{key}'");
        return true;
    }
    private static bool TryLoadWebGL(out string json, string key)
    {
        json = null;
        if (!PlayerPrefs.HasKey(key))
        {
            if (DebugMode)
                Debug.LogWarning($"[WebGL] No PlayerPrefs key found for '{key}'");
            return false;
        }
        json = PlayerPrefs.GetString(key);
        if (DebugMode)
            Debug.Log($"[WebGL] Loaded JSON string for '{key}'");
        return true;
    }
    public static bool TryDeleteWebGL(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return false;
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        if (DebugMode)
            Debug.Log($"[WebGL] Deleted PlayerPrefs key '{key}'");
        return true;
    }
}
#endregion
#region Safe (lmao) Score Storage
public static partial class PersistentJSON
{
    private const string EncryptionKey = "Fumo Fumo Fumo Fumo";
    private static long DoubleToLong(double value) =>
        BitConverter.DoubleToInt64Bits(value);
    private static double LongToDouble(long bits) =>
        BitConverter.Int64BitsToDouble(bits);
    private static string EncryptString(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            var key = new Rfc2898DeriveBytes(EncryptionKey, Encoding.UTF8.GetBytes("Mofumofumo"));
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                return Convert.ToBase64String(encrypted);
            }
        }
    }
    private static string DecryptString(string cipherText)
    {
        using (Aes aes = Aes.Create())
        {
            var key = new Rfc2898DeriveBytes(EncryptionKey, Encoding.UTF8.GetBytes("Mofumofumo"));
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                byte[] bytes = Convert.FromBase64String(cipherText);
                byte[] decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
        }
    }
    private static string ComputeHash(string value)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value + EncryptionKey));
            return Convert.ToBase64String(bytes);
        }
    }
    public static bool SaveScore(double score, string key)
    {
        long encoded = DoubleToLong(score);
        string data = encoded.ToString();
        string hash = ComputeHash(data);
        string combined = $"{data}:{hash}";
        string encrypted = EncryptString(combined);

        return PersistentJSON.TrySave(encrypted, key);
    }
    public static bool LoadScore(string key, out double score)
    {
        score = 0f;
        if (!PersistentJSON.TryLoad(out string encrypted, key))
            return false;
        try
        {
            string decrypted = DecryptString(encrypted);
            string[] parts = decrypted.Split(':');
            if (parts.Length != 2)
                throw new Exception("Corrupt score data");

            string data = parts[0];
            string hash = parts[1];
            if (hash != ComputeHash(data))
                throw new Exception("Score tampering detected!");

            long encoded = long.Parse(data);
            score = LongToDouble(encoded);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SecureScore] Failed to load score: {ex.Message}");
            return false;
        }
    }
}
#endregion
#region Core Persistent JSON System
public static partial class PersistentJSON
{
    public static bool DebugMode => false;
    [System.Serializable]
    private class ListWrapper<TItem>
    {
        public List<TItem> Items;
        public ListWrapper(List<TItem> items) => Items = items;
    }
    [System.Serializable]
    private class PrimitiveWrapper<T>
    {
        public T Value;
        public PrimitiveWrapper(T value) => Value = value;
    }
    private static string SaveFilePath<T>(string fileName)
    {
        string typeName = typeof(T).Name;
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            typeName = $"ListOf_{elementType.Name}";
        }
        string safeFileName = fileName.Replace(" ", "_");
        return Path.Combine(Application.persistentDataPath, $"Json Storage/{safeFileName}_{typeName}.json");
    }
    public static bool TrySave<T>(T saveItem, string key)
    {
        if (saveItem == null) return false;
        string json;
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            var wrapperType = typeof(ListWrapper<>).MakeGenericType(elementType);
            var wrapper = Activator.CreateInstance(wrapperType, saveItem);
            json = JsonUtility.ToJson(wrapper, true);
        }
        else if (IsPrimitiveOrString(typeof(T)))
        {
            var wrapper = new PrimitiveWrapper<T>(saveItem);
            json = JsonUtility.ToJson(wrapper, true);
        }
        else
        {
            json = JsonUtility.ToJson(saveItem, true);
        }
        string slotKey = GetSlotKey(key);
        if (IsWebGLBuild)
            return TrySaveWebGL(saveItem, slotKey, json);
        string path = GetSlotPath<T>(key);
        File.WriteAllText(path, json);
        if (DebugMode)
            Debug.Log($"Saved {typeof(T).Name} to {path}");
        return true;
    }
    public static bool TryLoad<T>(out T target, string key)
    {
        target = default(T);
        string json = null;
        string slotKey = GetSlotKey(key);
        if (IsWebGLBuild)
        {
            if (!TryLoadWebGL(out json, slotKey))
                return false;
        }
        else
        {
            string path = GetSlotPath<T>(key);
            if (!File.Exists(path))
            {
                if (DebugMode)
                    Debug.LogWarning($"No save found at {path}");
                return false;
            }
            json = File.ReadAllText(path);
        }
        T item;
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            var wrapperType = typeof(ListWrapper<>).MakeGenericType(elementType);
            var wrapper = JsonUtility.FromJson(json, wrapperType);
            var itemsField = wrapperType.GetField("Items");
            item = (T)itemsField.GetValue(wrapper);
        }
        else if (IsPrimitiveOrString(typeof(T)))
        {
            var wrapper = JsonUtility.FromJson<PrimitiveWrapper<T>>(json);
            item = wrapper.Value;
        }
        else
        {
            item = JsonUtility.FromJson<T>(json);
        }
        if (item == null)
        {
            Debug.LogWarning($"Failed to deserialize {typeof(T).Name} from {(IsWebGLBuild ? "PlayerPrefs" : "file")}");
            return false;
        }
        target = item;
        if (DebugMode)
        {
            if (IsWebGLBuild)
                Debug.Log($"Loaded {typeof(T).Name} from PlayerPrefs key '{slotKey}'");
            else
                Debug.Log($"Loaded {typeof(T).Name} from file");
        }
        return true;
    }
    private static bool IsPrimitiveOrString(Type t)
    {
        return t.IsPrimitive || t == typeof(string) ||
               t == typeof(decimal) || t == typeof(double) ||
               t == typeof(float);
    }
}
#endregion
#region Save Slot Management
public static partial class PersistentJSON
{
    private static int _currentSlot = 0;
    public static int CurrentSlot
    {
        get => _currentSlot;
        set
        {
            if (value < 0)
            {
                Debug.LogWarning("[PersistentJSON] Slot index cannot be negative. Defaulting to 0.");
                _currentSlot = 0;
            }
            else
            {
                _currentSlot = value;
                if (DebugMode)
                    Debug.Log($"[PersistentJSON] Switched to save slot {_currentSlot}");
            }
        }
    }
    private static string GetSlotKey(string baseKey)
    {
        return $"{baseKey}_slot{_currentSlot}";
    }
    private static string GetSlotPath<T>(string baseKey)
    {
        string slotFolder = Path.Combine(Application.persistentDataPath, "Json Storage", $"Slot_{_currentSlot}");
        Directory.CreateDirectory(slotFolder);
        string typeName = typeof(T).Name;
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            typeName = $"ListOf_{elementType.Name}";
        }
        string safeFileName = baseKey.Replace(" ", "_");
        return Path.Combine(slotFolder, $"{safeFileName}_{typeName}.json");
    }
    public static void ClearSlot()
    {
        if (IsWebGLBuild)
        {
            Debug.LogWarning("[PersistentJSON] ClearSlot() on WebGL only works for known keys you manually delete.");
            return;
        }
        string slotFolder = Path.Combine(Application.persistentDataPath, "Json Storage", $"Slot_{_currentSlot}");
        if (Directory.Exists(slotFolder))
        {
            Directory.Delete(slotFolder, true);
            if (DebugMode)
                Debug.Log($"[PersistentJSON] Cleared slot folder: {slotFolder}");
        }
        else if (DebugMode)
        {
            Debug.Log($"[PersistentJSON] No folder found for slot {_currentSlot}");
        }
    }
}
#endregion