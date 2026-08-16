using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Text;

#region JSON Playerprefs Alternate during WEBGL
public static partial class PersistentJSON
{
    private static bool IsWebGLBuild => Application.platform == RuntimePlatform.WebGLPlayer;

    private static bool TrySaveWebGL<T>(T saveItem, string key, string json)
    {
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        if (DebugMode) Debug.Log($"[WebGL] Saved {typeof(T).Name} to PlayerPrefs key '{key}'");
        return true;
    }

    private static bool TryLoadWebGL(out string json, string key)
    {
        json = null;
        if (!PlayerPrefs.HasKey(key))
        {
            if (DebugMode) Debug.LogWarning($"[WebGL] No PlayerPrefs key found for '{key}'");
            return false;
        }
        json = PlayerPrefs.GetString(key);
        if (DebugMode) Debug.Log($"[WebGL] Loaded JSON string for '{key}'");
        return true;
    }

    public static bool TryDeleteWebGL(string key)
    {
        if (!PlayerPrefs.HasKey(key)) return false;
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        if (DebugMode) Debug.Log($"[WebGL] Deleted PlayerPrefs key '{key}'");
        return true;
    }
}
#endregion

#region Safe (lmao) Score Storage
public static partial class PersistentJSON
{
    private const string EncryptionKey = "Fumo Fumo Fumo Fumo";

    public static long ToLong(this double value) => BitConverter.DoubleToInt64Bits(value);
    public static double ToDouble(this long bits) => BitConverter.Int64BitsToDouble(bits);

    public static string EncryptString(this string plainText, string salt = "Mofumofumo")
    {
        using (Aes aes = Aes.Create())
        {
            var key = new Rfc2898DeriveBytes(EncryptionKey, Encoding.UTF8.GetBytes(salt));
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(plainText);
                return Convert.ToBase64String(encryptor.TransformFinalBlock(bytes, 0, bytes.Length));
            }
        }
    }

    public static string DecryptString(this string cipherText, string salt = "Mofumofumo")
    {
        using (Aes aes = Aes.Create())
        {
            var key = new Rfc2898DeriveBytes(EncryptionKey, Encoding.UTF8.GetBytes(salt));
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                byte[] bytes = Convert.FromBase64String(cipherText);
                return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(bytes, 0, bytes.Length));
            }
        }
    }

    private static string ComputeHash(string value)
    {
        using (SHA256 sha = SHA256.Create())
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value + EncryptionKey)));
    }

    public static bool SaveScore(double score, string key)
    {
        string data = score.ToLong().ToString();
        string hash = ComputeHash(data);
        string encrypted = $"{data}:{hash}".EncryptString();
        Debug.Log($"Storing score: {score} : key: {key}");
        return PersistentJSON.TrySave(encrypted, key);
    }

    private static double TestFetchScore(string key)
    {
        LoadScore(key, out double score);
        return score;
    }

    public static bool LoadScore(string key, out double score)
    {
        score = 0d;
        if (!PersistentJSON.TryLoad(out string encrypted, key))
        {
            Debug.Log($"Failed to Fetch score. Fallback: {score} : key: {key}");
            return false;
        }
        try
        {
            string decrypted = encrypted.DecryptString();
            string[] parts = decrypted.Split(':');
            if (parts.Length != 2 || parts[1] != ComputeHash(parts[0]))
                throw new Exception("Corrupt or tampered score data");

            score = long.Parse(parts[0]).ToDouble();
            Debug.Log($"Fetching score: {score} : key: {key}");
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

    public static string ToJson<T>(T item, bool prettyPrint = true)
    {
        if (item == null) return string.Empty;

        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elemType = typeof(T).GetGenericArguments()[0];
            var wrapperType = typeof(ListWrapper<>).MakeGenericType(elemType);
            return JsonUtility.ToJson(Activator.CreateInstance(wrapperType, item), prettyPrint);
        }
        if (IsPrimitiveOrString(typeof(T)))
            return JsonUtility.ToJson(new PrimitiveWrapper<T>(item), prettyPrint);

        return JsonUtility.ToJson(item, prettyPrint);
    }

    public static T ToText<T>(string json)
    {
        if (string.IsNullOrEmpty(json)) return default;

        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elemType = typeof(T).GetGenericArguments()[0];
            var wrapperType = typeof(ListWrapper<>).MakeGenericType(elemType);
            var wrapper = JsonUtility.FromJson(json, wrapperType);
            return wrapper != null ? (T)wrapperType.GetField("Items").GetValue(wrapper) : default;
        }
        if (IsPrimitiveOrString(typeof(T)))
        {
            var wrapper = JsonUtility.FromJson<PrimitiveWrapper<T>>(json);
            return wrapper != null ? wrapper.Value : default;
        }

        return JsonUtility.FromJson<T>(json);
    }

    public static bool TryToText<T>(string json, out T result)
    {
        result = default;
        if (string.IsNullOrEmpty(json)) return false;
        try
        {
            result = ToText<T>(json);
            return result != null;
        }
        catch (Exception ex)
        {
            if (DebugMode) Debug.LogWarning($"[PersistentJSON] Failed parsing string: {ex.Message}");
            return false;
        }
    }

    public static bool TrySave<T>(T saveItem, string key)
    {
        if (saveItem == null) return false;
        string json = ToJson(saveItem, true);
        string slotKey = GetSlotKey(key);

        if (IsWebGLBuild) return TrySaveWebGL(saveItem, slotKey, json);

        string path = GetSlotPath<T>(key);
        File.WriteAllText(path, json);
        if (DebugMode) Debug.Log($"Saved {typeof(T).Name} to {path}");
        return true;
    }

    public static bool TryLoad<T>(out T target, string key)
    {
        target = default;
        string json = null;
        string slotKey = GetSlotKey(key);

        if (IsWebGLBuild)
        {
            if (!TryLoadWebGL(out json, slotKey)) return false;
        }
        else
        {
            string path = GetSlotPath<T>(key);
            if (!File.Exists(path))
            {
                if (DebugMode) Debug.LogWarning($"No save found at {path}");
                return false;
            }
            json = File.ReadAllText(path);
        }

        if (!TryToText(json, out target))
        {
            Debug.LogWarning($"Failed to deserialize {typeof(T).Name} from {(IsWebGLBuild ? "PlayerPrefs" : "file")}");
            return false;
        }

        if (DebugMode) Debug.Log($"Loaded {typeof(T).Name} successfully");
        return true;
    }

    private static bool IsPrimitiveOrString(Type t)
    {
        return t.IsPrimitive || t == typeof(string) || t == typeof(decimal) || t == typeof(double) || t == typeof(float);
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
                if (DebugMode) Debug.Log($"[PersistentJSON] Switched to save slot {_currentSlot}");
            }
        }
    }

    private static string GetSlotKey(string baseKey) => $"{baseKey}_slot{_currentSlot}";

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
            if (DebugMode) Debug.Log($"[PersistentJSON] Cleared slot folder: {slotFolder}");
        }
    }
}
#endregion