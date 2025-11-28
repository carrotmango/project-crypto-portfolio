using UnityEngine;
using System.IO;

public static class SaveManager
{
    private static string path = Application.persistentDataPath + "/save.json";

    public static void Save()
    {
        GameData data = new GameData();
        data.playerName = PlayerManager.Instance.playerName;
        data.birthday = PlayerManager.Instance.birthday;
        data.characterIndex = PlayerManager.Instance.characterIndex;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);

        Debug.Log("저장 완료: " + path);
    }

    public static GameData Load()
    {
        if (!File.Exists(path)) {
            Debug.LogWarning("저장 파일 없음");
            return null;
        }

        string json = File.ReadAllText(path);
        GameData data = JsonUtility.FromJson<GameData>(json);
        Debug.Log("불러오기 완료: " + json);

        return data;
    }

    public static void DeleteSave()
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}