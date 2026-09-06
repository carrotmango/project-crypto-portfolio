using System;
using System.Collections.Generic;
using UnityEngine;

public class NewsRepository : MonoBehaviour {

    private Dictionary<string, UIEventData> map;
    private List<UIEventData> allItems;
    private List<OccurredNewsData> occurredHistory = new List<OccurredNewsData>();

    public List<OccurredNewsData> GetResearchArticles() {
        return occurredHistory.FindAll(x => x.data.article != null && !string.IsNullOrEmpty(x.data.article.title));
    }

    void Awake() {
        LoadData();
    }

    public void LoadData() {
        map = new Dictionary<string, UIEventData>();

        int savedLang = PlayerPrefs.GetInt("Saved_Language", 1);

        // [핵심 원인 해결] 행님이 말씀하신 'json' 폴더 경로를 명시해줍니다!
        string fileName = (savedLang == 0) ? "json/UI_En" : "json/UI";

        TextAsset targetJson = Resources.Load<TextAsset>(fileName);

        if (targetJson == null) {
            Debug.LogError($"[NewsRepository] '{fileName}' 파일을 찾을 수 없습니다! 폴더 위치를 확인하세요.");
            return;
        }

        var wrapper = JsonUtility.FromJson<Wrapper>(targetJson.text);
        if (wrapper != null && wrapper.items != null) {
            allItems = wrapper.items;

            foreach (var item in wrapper.items) {
                map[item.key] = item;
            }
            Debug.Log($"[NewsRepository] 언어 교체 완료! {fileName} 로드 성공. (항목: {map.Count}개)");
        }
    }

    public List<OccurredNewsData> GetNewsByCategory(NewsCategory category) {
        if (category == NewsCategory.All) return occurredHistory;
        return occurredHistory.FindAll(x => x.data.category == category);
    }

    public void AddToHistory(UIEventData data, DateTime time) {
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
        public bool isRead = false;
    }

    public void CleanUpOldNews(DateTime currentTime, double maxAgeDays, int maxCount) {
        if (occurredHistory.Count == 0) return;

        occurredHistory.RemoveAll(x => x.data.key.StartsWith("noise_") &&
                                       (currentTime - x.occurredTime).TotalDays >= maxAgeDays);

        int noiseCount = 0;
        foreach (var item in occurredHistory) {
            if (item.data.key.StartsWith("noise_")) noiseCount++;
        }

        if (noiseCount > maxCount) {
            int removeCount = noiseCount - maxCount;
            for (int i = 0; i < occurredHistory.Count && removeCount > 0;) {
                if (occurredHistory[i].data.key.StartsWith("noise_")) {
                    occurredHistory.RemoveAt(i);
                    removeCount--;
                } else {
                    i++;
                }
            }
        }
    }
}