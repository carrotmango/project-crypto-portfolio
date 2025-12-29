using System;
using System.Collections.Generic;
using UnityEngine;

public class NewsRepository : MonoBehaviour {

    public TextAsset uiNewsJson;
    private Dictionary<string, UIEventData> map;

    void Awake() {
        map = new Dictionary<string, UIEventData>();
        var wrapper = JsonUtility.FromJson<Wrapper>(uiNewsJson.text);
        foreach (var item in wrapper.items) {
            map[item.key] = item;
        }
    }

    public UIEventData Get(string key) {
        map.TryGetValue(key, out var data);
        return data;
    }

    [Serializable]
    private class Wrapper {
        public List<UIEventData> items;
    }
}
