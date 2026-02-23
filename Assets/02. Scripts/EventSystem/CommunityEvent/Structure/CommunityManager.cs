using UnityEngine;
using System.Collections.Generic;
using System;

public class CommunityManager : MonoBehaviour {
    [Header("Dependencies")]
    public CoinManager coinManager;
    public NewsPanel newsPanel;
    public NewsRepository newsRepo;

    [Header("Probability Settings")]
    [Range(0f, 1f)] public float postProbability = 0.3f;
    [Range(0f, 1f)] public float frenzyProbability = 0.9f;
    public float frenzyThreshold = 30f;

    [Header("Cooldown Settings")]
    public float duplicateCooltimeHours = 24f; // 모든 글은 24시간 쿨타임 고정
    public float authorCooltimeHours = 2f;


    public RetroToggleController communityToggle;

    private Dictionary<string, DateTime> lastPostedTime = new Dictionary<string, DateTime>();
    private Dictionary<string, DateTime> lastAuthorTime = new Dictionary<string, DateTime>();
    private HashSet<string> usedOneTimePosts = new HashSet<string>();

    private void Start() {
        if (coinManager != null) coinManager.OnMarketUpdated += OnTick;
    }

    private void OnDestroy() {
        if (coinManager != null) coinManager.OnMarketUpdated -= OnTick;
    }

    private void OnTick() {
        var postDB = CommunityDataLoader.Instance.PostDB;
        var userDB = CommunityDataLoader.Instance.UserDB;
        if (postDB == null || userDB == null) return;

        // 1. 광기 모드 체크 (연출용 확률 보정)
        bool isFrenzy = false;
        foreach (var c in coinManager.coins) {
            if (!c.IsListed || c.InitialPrice <= 0) continue;
            double rate = Math.Abs((c.CurrentPrice - c.InitialPrice) / c.InitialPrice * 100.0);
            if (rate >= frenzyThreshold) { isFrenzy = true; break; }
        }

        float currentProb = isFrenzy ? frenzyProbability : postProbability;
        if (UnityEngine.Random.value > currentProb) return;

        DateTime now = coinManager.CurrentDateTime;
        List<KeyValuePair<CoinData, List<PostTemplate>>> potentialPosts = new List<KeyValuePair<CoinData, List<PostTemplate>>>();

        foreach (var coin in coinManager.coins) {
            if (!coin.IsListed) continue;
            List<PostTemplate> validForThisCoin = GetFilteredTemplates(postDB, coin, now);
            if (validForThisCoin.Count > 0) {
                potentialPosts.Add(new KeyValuePair<CoinData, List<PostTemplate>>(coin, validForThisCoin));
            }
        }

        if (potentialPosts.Count == 0) return;

        var selectedEntry = potentialPosts[UnityEngine.Random.Range(0, potentialPosts.Count)];
        CoinData targetCoin = selectedEntry.Key;
        List<PostTemplate> templates = selectedEntry.Value;

        ShuffleList(templates);

        // 2. 버스트 로직 (중복 없이 와바박 생성)
        int burstCount = isFrenzy ? UnityEngine.Random.Range(3, 6) : 1;
        int sentCount = 0;

        HashSet<string> sessionPostedContent = new HashSet<string>();
        HashSet<string> sessionAuthors = new HashSet<string>();

        for (int i = 0; i < templates.Count; i++) {
            if (sentCount >= burstCount) break;

            var pick = templates[i];
            if (sessionPostedContent.Contains(pick.content)) continue;

            if (GeneratePost(pick, targetCoin, userDB, now, sessionAuthors)) {
                sessionPostedContent.Add(pick.content);
                sentCount++;
            }
        }
    }

    private List<PostTemplate> GetFilteredTemplates(CommunityPostDB db, CoinData coin, DateTime now) {
        List<PostTemplate> filtered = new List<PostTemplate>();
        double currentPrice = coin.CurrentPrice;
        double changeRate = coin.InitialPrice > 0 ? (currentPrice - coin.InitialPrice) / coin.InitialPrice * 100.0 : 0;

        foreach (var t in db.templates) {
            if (t.isOneTime && usedOneTimePosts.Contains(t.content)) continue;

            // [심플 쿨타임] 무조건 전역 설정(24시간)만 따짐
            if (lastPostedTime.TryGetValue(t.content, out DateTime lastTime)) {
                if ((now - lastTime).TotalHours < duplicateCooltimeHours) continue;
            }

            if (t.targetSymbols != null && t.targetSymbols.Length > 0) {
                bool match = false;
                foreach (var s in t.targetSymbols) { if (s == coin.Symbol) { match = true; break; } }
                if (!match) continue;
            }

            if (t.minPrice > 0 && currentPrice < t.minPrice) continue;
            if (t.maxPrice > 0 && currentPrice > t.maxPrice) continue;
            if (t.minChange != 0 && changeRate < t.minChange) continue;
            if (t.maxChange != 0 && changeRate > t.maxChange) continue;
            if (t.minHour != -1 && now.Hour < t.minHour) continue;
            if (t.maxHour != -1 && now.Hour > t.maxHour) continue;

            filtered.Add(t);
        }
        return filtered;
    }

    private bool GeneratePost(PostTemplate pick, CoinData coin, UserDB userDB, DateTime now, HashSet<string> sessionAuthors) {
        string author = "";
        string profile = "";

        if (!string.IsNullOrEmpty(pick.fixedName)) {
            if (sessionAuthors.Contains(pick.fixedName) || IsAuthorCoolingDown(pick.fixedName, now)) return false;
            author = pick.fixedName;
            profile = !string.IsNullOrEmpty(pick.fixedProfile) ? pick.fixedProfile : userDB.profiles[UnityEngine.Random.Range(0, userDB.profiles.Length)];
        } else {
            int retry = 0;
            bool found = false;
            while (retry < 15) {
                string temp = userDB.names[UnityEngine.Random.Range(0, userDB.names.Length)];
                if (!sessionAuthors.Contains(temp) && !IsAuthorCoolingDown(temp, now)) {
                    author = temp;
                    profile = userDB.profiles[UnityEngine.Random.Range(0, userDB.profiles.Length)];
                    found = true;
                    break;
                }
                retry++;
            }
            if (!found) return false;
        }

        if (pick.isOneTime) usedOneTimePosts.Add(pick.content);
        UpdateCooltime(lastPostedTime, pick.content, now);
        UpdateCooltime(lastAuthorTime, author, now);
        sessionAuthors.Add(author);

        UIEventData data = new UIEventData {
            // [수정] CommunityManager에서 생성하는 모든 글은 'noise_' 접두사를 붙여서 똥글로 분류
            key = "noise_" + Guid.NewGuid().ToString(),

            type = UIEventType.News,
            category = NewsCategory.Community,
            authorName = author,
            profileImage = profile,
            message = string.Format(pick.content, coin.Name, coin.CurrentPrice.ToString("N0")),
            contentImage = pick.contentImage,
            width = pick.width,
            height = pick.height
        };

        // 기록은 무조건 합니다.
        if (newsRepo != null) newsRepo.AddToHistory(data, now);

        // [수정] 알림 노출 여부: 토글이 켜져 있을 때만 알림(Show)을 띄웁니다.
        // (이곳에서 생성되는 글은 무조건 노이즈이므로 토글 상태만 체크하면 됨)
        if (communityToggle != null && communityToggle.IsCommunityVisible) {
            if (newsPanel != null) {
                newsPanel.Show(data, now);
            }
        }

        return true;
    }

    private void ShuffleList<T>(List<T> list) {
        for (int i = 0; i < list.Count; i++) {
            int rnd = UnityEngine.Random.Range(0, list.Count);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    private bool IsAuthorCoolingDown(string name, DateTime now) {
        if (lastAuthorTime.TryGetValue(name, out DateTime last)) {
            return (now - last).TotalHours < authorCooltimeHours;
        }
        return false;
    }

    private void UpdateCooltime(Dictionary<string, DateTime> dict, string key, DateTime now) {
        if (dict.ContainsKey(key)) dict[key] = now;
        else dict.Add(key, now);
    }
}