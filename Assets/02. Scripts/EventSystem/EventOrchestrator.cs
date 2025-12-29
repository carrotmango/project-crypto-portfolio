using UnityEngine;
using System;
using System.Collections.Generic;

public class EventOrchestrator : MonoBehaviour {
    [Header("레퍼런스")]
    public XPostRepository repo;
    public XFeedSpawner feed;
    public CoinManager coinManager;
    public List<(XPostData data, DateTime startTime)> activeEffects = new();
    private HashSet<string> executedEvents = new(); // 중복 실행 방지용
    public static EventOrchestrator Instance;
    public NewsManager newsManager;
    public NewsRepository newsRepo;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update() {

        DateTime now = coinManager.CurrentDateTime;

        // 이벤트 지속시간 체크
        for (int i = activeEffects.Count - 1; i >= 0; i--) {
            var (data, startTime) = activeEffects[i];
            DateTime endTime = startTime.AddHours(data.durationHours);

            if (now >= endTime) {
                EndTweetEffect(data);
                activeEffects.RemoveAt(i);
            }
        }
        newsManager.Tick();
    }
    public bool HasExecuted(string eventKey) {
        return executedEvents.Contains(eventKey);
    }

    public void RunTweetEvent(string authorId, string eventKey) {

        if (executedEvents.Contains(eventKey)) return;

        // 1. UI 처리 (여기서 알림 여부 결정)
        TriggerUI(eventKey);

        // 2. XPost / Effect
        if (repo.TryGet(authorId, eventKey, out var data)) {

            if (data.postYn) {
                feed.SpawnFromData(data); // 피드만
            }

            ApplyTweetEffect(data);
            activeEffects.Add((data, coinManager.CurrentDateTime));
        }

        executedEvents.Add(eventKey);
        Debug.Log($"[Event 실행] {authorId}/{eventKey}");
    }



    private void ApplyTweetEffect(XPostData data) {

        if (data.targetGroups == null) return;

        foreach (var group in data.targetGroups) {
            foreach (var symbol in group.symbols) {

                var coin = coinManager.coins.Find(c => c.Symbol == symbol);
                if (coin == null) continue;

                // =========================
                // 1. 이벤트 타입 (문자열 기반)
                // =========================
                if (group.eventType == "Relisting") {
                    // 실제 Relist는 Effect에서 끝났다고 가정
                    // 여기선 안전 보정만
                    if (coin.IsDelisted) {
                        coin.ApplyRelist(
                            group.relistBasePrice > 0
                                ? group.relistBasePrice
                                : coin.InitialPrice
                        );
                    }
                } else if (group.eventType == "Delisting") {
                    // XPost에서는 상태 변경 금지
                    continue;
                }

                // =========================
                // 2. 가격 연출
                // =========================
                if (!coin.IsDelisted) {
                    float percent = UnityEngine.Random.Range(
                        group.priceChangeMin,
                        group.priceChangeMax
                    );

                    double before = coin.CurrentPrice;
                    coin.CurrentPrice *= 1f + percent / 100f;

                    Debug.Log(
                        $"[TweetEffect] {symbol} {percent}% | {before} → {coin.CurrentPrice}"
                    );
                }

                // =========================
                // 3. 페이즈 연출
                // =========================
                if (!string.IsNullOrEmpty(group.marketPhaseToSet)) {
                    coin.CurrentPhaseOverride = ParsePhase(group.marketPhaseToSet);
                    coin.PhaseOverrideEndTime =
                        coinManager.CurrentDateTime.AddHours(group.durationHours);
                }
            }
        }
    }

    private void EndTweetEffect(XPostData data) {
        MarketPhase afterPhase = ParsePhase(data.marketPhaseAfter, MarketPhase.Sideways);
        coinManager.CurrentMarket = afterPhase;
    }

    public MarketPhase ParsePhase(string value, MarketPhase fallback = MarketPhase.Sideways) {
        if (!string.IsNullOrEmpty(value) && Enum.TryParse(value, out MarketPhase parsed)) {
            return parsed;
        }
        return fallback;
    }
    public void RestoreEffect(XPostData data, DateTime startTime) {
        // 지속 효과 재등록
        activeEffects.Add((data, startTime));

        // 개별 코인 페이즈 복원
        if (data.targetGroups != null) {
            foreach (var group in data.targetGroups) {
                foreach (var symbol in group.symbols) {
                    var coin = coinManager.coins.Find(c => c.Symbol == symbol);
                    if (coin == null) continue;

                    if (!string.IsNullOrEmpty(group.marketPhaseToSet)) {
                        coin.CurrentPhaseOverride = ParsePhase(group.marketPhaseToSet);
                        coin.PhaseOverrideEndTime = startTime.AddHours(group.durationHours);
                    }
                }
            }
        }

        // 전체 마켓 페이즈 복원
        if (data.overrideMarketPhase && !string.IsNullOrEmpty(data.marketPhaseToSet)) {
            coinManager.CurrentMarket = ParsePhase(data.marketPhaseToSet);
        }

        Debug.Log("[RestoreEffect] 효과 복원: " + data.key);
    }
    private void TriggerUI(string uiKey) {
        if (string.IsNullOrEmpty(uiKey)) return;

        var uiData = newsRepo.Get(uiKey);
        if (uiData == null) return;

        // UI 출력
        EventUIManager.Instance.Show(uiData);

        if (uiData.type == UIEventType.News) {
            XNotificationManager.Instance?.Show(
                uiData.authorName,
                uiData.key
            );
        }
    }



}