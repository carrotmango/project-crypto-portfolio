using System;
using System.Collections.Generic;
using UnityEngine;

public class NewsRepository : MonoBehaviour {

    public TextAsset uiNewsJson;
    private Dictionary<string, UIEventData> map;
    private List<UIEventData> allItems;
    private List<OccurredNewsData> occurredHistory = new List<OccurredNewsData>();

    void Awake() {
        map = new Dictionary<string, UIEventData>();
        var wrapper = JsonUtility.FromJson<Wrapper>(uiNewsJson.text);
        allItems = wrapper.items;

        foreach (var item in wrapper.items) {
            map[item.key] = item;
        }
    }

    public List<OccurredNewsData> GetNewsByCategory(NewsCategory category) {
        if (category == NewsCategory.All) return occurredHistory;
        return occurredHistory.FindAll(x => x.data.category == category);
    }

    public void AddToHistory(UIEventData data, DateTime time) {
        // 중복 방지 로직 (필요 시)
        if (occurredHistory.Exists(x => x.data.key == data.key)) return;

        occurredHistory.Add(new OccurredNewsData {
            data = data,
            occurredTime = time
        });
    }

    public UIEventData Get(string key) {
        map.TryGetValue(key, out var data);
        return data;
    }

    [Serializable]
    private class Wrapper {
        public List<UIEventData> items;
    }

    [Serializable]
    public class OccurredNewsData {
        public UIEventData data;
        public DateTime occurredTime;
    }
}
