using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class CommunityManager : MonoBehaviour {
    [Header("Dependencies")]
    public CoinManager coinManager;
    public NewsPanel newsPanel;
    public NewsRepository newsRepo;
    public RetroToggleController communityToggle;

    [Header("Settings")]
    public float exchangeRate = 1350f;

    [Header("Probability Settings")]
    [Range(0f, 1f)] public float postProbability = 0.05f;   // [수정] 30% -> 5% (난사 방지)
    [Range(0f, 1f)] public float frenzyProbability = 0.2f;  // [수정] 80% -> 20%

    [Header("Global Throttling (난사 방지)")]
    public float globalCooltimeGameMinutes = 60f; // [추가] 글 한번 쓰면 게임시간 60분간 침묵
    private DateTime lastGlobalPostTime;          // [추가] 마지막으로 글 쓴 시간 기록

    [Header("Cooldown Settings")]
    public float globalTemplateCooltimeHours = 12f;
    public float coinSpecificCooltimeHours = 4f;
    public float authorCooltimeHours = 2f;

    private Dictionary<string, DateTime> lastTemplateUsedTime = new Dictionary<string, DateTime>();
    private Dictionary<string, DateTime> lastCoinReactedTime = new Dictionary<string, DateTime>();
    private Dictionary<string, DateTime> lastAuthorTime = new Dictionary<string, DateTime>();
    private HashSet<string> usedOneTimePosts = new HashSet<string>();

    public int maxPostCount = 10;          // 최대 보관할 게시글 수 
    public double deleteOldDays = 5.0;

    private void Start() {
        if (coinManager != null) coinManager.OnMarketUpdated += OnTick;
        // 시작하자마자 글 뜨는거 방지하려면 아래 주석 해제
        // lastGlobalPostTime = coinManager.CurrentDateTime; 
    }

    private void OnDestroy() {
        if (coinManager != null) coinManager.OnMarketUpdated -= OnTick;
    }

    private void OnTick() {
        var postDB = CommunityDataLoader.Instance.PostDB;
        var userDB = CommunityDataLoader.Instance.UserDB;
        if (postDB == null || userDB == null) return;

        DateTime now = coinManager.CurrentDateTime;

        // [핵심 수정] 전체 쿨타임 체크 (난사 방지 1순위)
        // 마지막 글 쓴 지 'globalCooltimeGameMinutes'가 안 지났으면 무조건 리턴
        if ((now - lastGlobalPostTime).TotalMinutes < globalCooltimeGameMinutes) return;

        // 1. 활성 코인 리스트업
        List<CoinData> activeCoins = new List<CoinData>();
        foreach (var coin in coinManager.coins) {
            if (!coin.IsListed || coin.InitialPrice <= 0) continue;

            if (lastCoinReactedTime.TryGetValue(coin.Symbol, out DateTime lastTime)) {
                if ((now - lastTime).TotalHours < coinSpecificCooltimeHours) continue;
            }

            double rate = (coin.CurrentPrice - coin.InitialPrice) / coin.InitialPrice * 100.0;
            if (Math.Abs(rate) >= 14.0) activeCoins.Add(coin);
        }

        if (activeCoins.Count == 0) return;

        // 2. 확률 체크
        float currentProb = isAnyFrenzy() ? frenzyProbability : postProbability;
        if (UnityEngine.Random.value > currentProb) return;

        // 3. 타겟 코인 그룹 선정
        int targetCount = UnityEngine.Random.Range(1, Math.Min(activeCoins.Count + 1, 4));
        ShuffleList(activeCoins);
        var selectedCoins = activeCoins.GetRange(0, targetCount);

        // 4. 템플릿 필터링
        double baseRate = (selectedCoins[0].CurrentPrice - selectedCoins[0].InitialPrice) / selectedCoins[0].InitialPrice * 100.0;
        var validTemplates = GetFilteredTemplates(postDB, selectedCoins, now, baseRate, targetCount);

        if (validTemplates.Count == 0) return;

        // 5. 포스팅
        PostTemplate pick = validTemplates[UnityEngine.Random.Range(0, validTemplates.Count)];
        if (GeneratePost(pick, selectedCoins, userDB, now)) {
            // [핵심] 글 썼으면 전체 쿨타임 갱신
            lastGlobalPostTime = now;

            foreach (var c in selectedCoins) UpdateCooltime(lastCoinReactedTime, c.Symbol, now);
        }

        if (newsRepo != null) newsRepo.CleanUpOldNews(now, deleteOldDays, maxPostCount);
        if (newsPanel != null) newsPanel.CleanUpUI(maxPostCount);

    }

    private List<PostTemplate> GetFilteredTemplates(CommunityPostDB db, List<CoinData> coins, DateTime now, double changeRate, int coinCount) {
        List<PostTemplate> filtered = new List<PostTemplate>();
        foreach (var t in db.templates) {
            if (coinCount < t.minCoins || coinCount > t.maxCoins) continue;
            if (t.isOneTime && usedOneTimePosts.Contains(t.content)) continue;

            if (lastTemplateUsedTime.TryGetValue(t.content, out DateTime lastTime)) {
                if ((now - lastTime).TotalHours < globalTemplateCooltimeHours) continue;
            }

            if (t.targetSymbols != null && t.targetSymbols.Length > 0) {
                bool matchFound = false;
                foreach (var coin in coins) {
                    if (Array.Exists(t.targetSymbols, s => s.Equals(coin.Symbol, StringComparison.OrdinalIgnoreCase))) {
                        matchFound = true;
                        break;
                    }
                }
                if (!matchFound) continue;
            }

            // 변동률 체크
            if (t.minChange != 0 && changeRate < t.minChange) continue;
            if (t.maxChange != 0 && changeRate > t.maxChange) continue;

            filtered.Add(t);
        }
        return filtered;
    }

    private bool GeneratePost(PostTemplate pick, List<CoinData> coins, UserDB userDB, DateTime now) {
        HashSet<string> sessionAuthors = new HashSet<string>();
        string author = GetRandomAuthor(userDB, pick, now, sessionAuthors);
        if (string.IsNullOrEmpty(author)) return false;

        string combinedNames = "";
        if (coins.Count == 1) {
            combinedNames = coins[0].Name;
        } else {
            var nameList = new List<string>();
            nameList.Add(coins[0].Name);
            for (int i = 1; i < coins.Count; i++) nameList.Add(coins[i].Symbol);
            combinedNames = string.Join(", ", nameList);
        }

        double avgRate = coins.Average(c => (c.CurrentPrice - c.InitialPrice) / c.InitialPrice * 100.0);
        double priceKRW = coins[0].CurrentPrice;
        double priceUSD = priceKRW / (double)exchangeRate;

        string formattedMessage = string.Format(pick.content,
            combinedNames,
            avgRate.ToString("F1"),
            priceKRW.ToString("N0"),
            coins[0].Symbol,
            FormatPriceUSD(priceUSD),
            (priceUSD < 1.0 ? priceUSD.ToString("F6").TrimEnd('0').TrimEnd('.') : priceUSD.ToString("N0"))
        );

        if (pick.isOneTime) usedOneTimePosts.Add(pick.content);
        UpdateCooltime(lastTemplateUsedTime, pick.content, now);
        UpdateCooltime(lastAuthorTime, author, now);

        UIEventData data = new UIEventData {
            key = "noise_" + Guid.NewGuid().ToString(),
            type = UIEventType.News,
            category = NewsCategory.Community,
            authorName = author,
            profileImage = userDB.profiles[UnityEngine.Random.Range(0, userDB.profiles.Length)],
            message = formattedMessage,
            width = pick.width,
            height = pick.height
        };

        if (newsRepo != null) newsRepo.AddToHistory(data, now);
        if (communityToggle != null && communityToggle.IsCommunityVisible) newsPanel?.Show(data, now);

        return true;
    }

    private string FormatPriceUSD(double price) {
        if (price < 0.1) return price.ToString("F6").TrimEnd('0').TrimEnd('.');
        if (price >= 1000000) return (price / 1000000.0).ToString("F1").Replace(".0", "") + "M";
        if (price >= 1000) return (price / 1000.0).ToString("F1").Replace(".0", "") + "k";
        return price.ToString("N2");
    }

    private string GetRandomAuthor(UserDB userDB, PostTemplate pick, DateTime now, HashSet<string> session) {
        if (!string.IsNullOrEmpty(pick.fixedName)) return pick.fixedName;
        for (int i = 0; i < 15; i++) {
            string name = userDB.names[UnityEngine.Random.Range(0, userDB.names.Length)];
            if (!session.Contains(name) && !IsAuthorCoolingDown(name, now)) return name;
        }
        return null;
    }

    private void UpdateCooltime(Dictionary<string, DateTime> dict, string key, DateTime now) => dict[key] = now;

    private void ShuffleList<T>(List<T> list) {
        for (int i = 0; i < list.Count; i++) {
            int rnd = UnityEngine.Random.Range(0, list.Count);
            T temp = list[i]; list[i] = list[rnd]; list[rnd] = temp;
        }
    }

    private bool IsAuthorCoolingDown(string name, DateTime now) =>
        lastAuthorTime.TryGetValue(name, out DateTime last) && (now - last).TotalHours < authorCooltimeHours;

    private bool isAnyFrenzy() =>
        coinManager.coins.Any(c => c.IsListed && Math.Abs((c.CurrentPrice - c.InitialPrice) / c.InitialPrice * 100.0) >= 30.0);
}


//번호, 데이터 내용, 실제 출력 예시, 용도
//{0},이름 조립,"비트코인 / 비트코인, ETH 이랑 SOL", 주어(코인 명칭)
//{1},평균 변동률,24.5 / -15.2, 등락 수치 강조
//{2},KRW 가격,"95,000,000", 한국 원화 가격
//{3},대표 심볼, BTC / PEPE, 짧은 티커 명칭
//{4},USD 스마트,70k / 0.00012, 전문가/트위터 감성 (K단위/소수점)
//{5},USD 풀네임,"70,370 / 0.000123", 일반 유저 달러 가격