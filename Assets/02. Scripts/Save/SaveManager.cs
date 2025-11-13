using UnityEngine;
using System.IO;

public static class SaveManager
{
    private static string path = Application.persistentDataPath + "/save.json";

    public static void Save(GameData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        Debug.Log($"[SaveManager] 저장 완료: {path}");
    }

    public static GameData Load()
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("[SaveManager] 저장 파일이 없습니다.");
            return null;
        }

        string json = File.ReadAllText(path);
        GameData data = JsonUtility.FromJson<GameData>(json);
        return data;
    }

    public static void DeleteSave()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("[SaveManager] 기존 세이브 삭제 완료.");
        }
    }
}