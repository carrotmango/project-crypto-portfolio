using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EventRuleDB {
    public EventRule[] rules;
}

public class EventRuleRepository : MonoBehaviour {
    public TextAsset ruleJson;

    private Dictionary<string, EventRule> map = new();

    private void Awake() {
        Load();
    }

    void Load() {
        if (ruleJson == null) {
            Debug.LogError("[ScenarioRuleRepository] ruleJson ¾øÀ½");
            return;
        }

        var db = JsonUtility.FromJson<EventRuleDB>(ruleJson.text);
        if (db == null || db.rules == null) {
            Debug.LogError("[ScenarioRuleRepository] rules ÆÄ½Ì ½ÇÆÐ");
            return;
        }

        map.Clear();
        foreach (var r in db.rules) {
            if (!string.IsNullOrEmpty(r.key)) {
                map[r.key] = r;

                if (r.executionMode == ExecutionMode.None)
                    Debug.LogWarning($"[EventRule] executionMode None: {r.key}");

                if (r.repeatType == RepeatType.None)
                    Debug.LogWarning($"[EventRule] repeatType None: {r.key}");
            }
        }


        Debug.Log($"[ScenarioRuleRepository] {map.Count} rules ·ÎµåµÊ");
    }

    public IEnumerable<EventRule> All() {
        return map.Values;
    }

    public EventRule Get(string key) {
        return map.TryGetValue(key, out var rule) ? rule : null;
    }
}
