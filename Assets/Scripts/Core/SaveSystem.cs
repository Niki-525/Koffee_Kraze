// Scripts/Core/SaveSystem.cs
using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int day = 1;
    public int moneyCents;
    public int totalPoints;
    public float takeover = 0.3f;
    public float rivalSkill = 0.5f;
    public float rivalPriceMultiplier = 1f;
    public float rivalAggression = 0.5f;
}

public static class SaveSystem
{
    static string Path_ => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(SaveData data)
    {
        try { File.WriteAllText(Path_, JsonUtility.ToJson(data, true)); }
        catch (Exception e) { Debug.LogWarning($"Save failed: {e.Message}"); }
    }

    public static SaveData Load()
    {
        try
        {
            if (File.Exists(Path_))
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(Path_));
        }
        catch (Exception e) { Debug.LogWarning($"Load failed: {e.Message}"); }
        return new SaveData();
    }

    public static void Delete()
    {
        if (File.Exists(Path_)) File.Delete(Path_);
    }
}
