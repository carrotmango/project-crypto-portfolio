using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EffectDB {
    public EffectData[] effects;
}

public class EffectRepository : MonoBehaviour {
    public TextAsset effectJson;

    private Dictionary<string, EffectData> map = new();

    private void Awake() {
        Load();
    }

    void Load() {
        if (effectJson == null) {
            Debug.LogError("[EffectRepository] effectJson ¾øÀ½");
            return;
        }

        var db = JsonUtility.FromJson<EffectDB>(effectJson.text);
        if (db == null || db.effects == null) {
            Debug.LogError("[EffectRepository] effects ÆÄ½Ì ½ÇÆÐ");
            return;
        }

        map.Clear();
        foreach (var e in db.effects) {
            if (!string.IsNullOrEmpty(e.key))
                map[e.key] = e;
        }

        Debug.Log($"[EffectRepository] {map.Count} effects ·ÎµåµÊ");
    }

    public EffectData Get(string key) {
        return map.TryGetValue(key, out var e) ? e : null;
    }
}
