using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public static class GamePrefs
{
    private static Dictionary<string, object> cache = new();

    private static bool isDirty = false;
    public static string SavePath => Application.persistentDataPath + "/game_data.json";

    public static UniTask Init()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                var loaded = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                cache = loaded ?? new Dictionary<string, object>();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GamePrefs] Load error: {e.Message}");
                cache = new Dictionary<string, object>();
            }
        }
        else
        {
            cache = new Dictionary<string, object>();
        }

        Debug.Log($"[GamePrefs] Init done. Loaded {cache.Count} keys from {SavePath}");

        StartAutoSaveLoop();
        return UniTask.CompletedTask;
    }

    public static void Set<T>(string key, T value)
    {
        if (cache.TryGetValue(key, out object existingValue))
            if (existingValue != null && existingValue.Equals(value)) return;

        cache[key] = value;
        isDirty = true;
    }

    public static T Get<T>(string key, T defaultValue = default)
    {
        if (cache.TryGetValue(key, out object value))
        {
            try
            {
                if (value is Newtonsoft.Json.Linq.JToken token)
                    return token.ToObject<T>();
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public static bool HasKey(string key) => cache.ContainsKey(key);

    public static void DeleteKey(string key)
    {
        if (cache.Remove(key)) isDirty = true;
    }

    public static void Save()
    {
        if (!isDirty) return;
        try
        {
            string json = JsonConvert.SerializeObject(cache);
            File.WriteAllText(SavePath, json);
            isDirty = false;
        }
        catch (Exception e)
        {
            Debug.LogError("[GamePrefs] Save error: " + e.Message);
        }
    }

    private static async void StartAutoSaveLoop()
    {
        var token = Application.exitCancellationToken;

        try
        {
            while (!token.IsCancellationRequested)
            {
                await Awaitable.WaitForSecondsAsync(10f, token);
                if (isDirty) Save();
            }
        }
        catch (OperationCanceledException)
        {
            if (isDirty) Save();
        }
    }

    public static void ClearAll()
    {
        cache.Clear();
        isDirty = false;

        if (File.Exists(SavePath))
        {
            try
            {
                File.Delete(SavePath);
                Debug.Log($"[GamePrefs] Cleared all data. Deleted {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GamePrefs] Clear error: {e.Message}");
            }
        }
    }
}

public class PrefVar<T>
{
    private string key;
    private T defaultValue;

    public PrefVar(string newKey, T newDefaultValue = default)
    {
        key = newKey;
        defaultValue = newDefaultValue;
    }

    public T Value
    {
        get => GamePrefs.Get<T>(key, defaultValue);
        set => GamePrefs.Set<T>(key, value);
    }

    public static implicit operator T(PrefVar<T> prefVar) => prefVar.Value;
}