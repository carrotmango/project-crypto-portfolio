using System;
using System.Collections.Generic;
using UnityEngine;

public class NewsRepository : MonoBehaviour {

    public TextAsset uiNewsJson;
    private Dictionary<string, UIEventData> map;
    private List<UIEventData> allItems;
    private List<OccurredNewsData> occurredHistory = new List<OccurredNewsData>();

    public List<OccurredNewsData> GetResearchArticles() {
        // 발생했던 뉴스 중 article 객체가 존재하는 것들만 골라냄
        return occurredHistory.FindAll(x => x.data.article != null && !string.IsNullOrEmpty(x.data.article.title));
    }

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
        public bool isRead = false;
    }

    public void CleanUpOldNews(DateTime currentTime, double maxAgeDays, int maxCount) {
        if (occurredHistory.Count == 0) return;

        // 1. 설정한 일수(maxAgeDays)가 지난 '노이즈(noise_)' 게시글만 삭제
        occurredHistory.RemoveAll(x => x.data.key.StartsWith("noise_") &&
                                       (currentTime - x.occurredTime).TotalDays >= maxAgeDays);

        // 2. 남은 '노이즈' 게시글 개수 파악
        int noiseCount = 0;
        foreach (var item in occurredHistory) {
            if (item.data.key.StartsWith("noise_")) noiseCount++;
        }

        // 3. 노이즈 게시글이 최대 개수를 초과하면, 가장 오래된(리스트 앞쪽) 노이즈 글만 찾아 잘라냄
        if (noiseCount > maxCount) {
            int removeCount = noiseCount - maxCount;
            for (int i = 0; i < occurredHistory.Count && removeCount > 0;) {
                if (occurredHistory[i].data.key.StartsWith("noise_")) {
                    occurredHistory.RemoveAt(i);
                    removeCount--;
                } else {
                    i++; // 노이즈 글이 아닌 진짜 뉴스는 건너뜀
                }
            }
        }
    }
}
