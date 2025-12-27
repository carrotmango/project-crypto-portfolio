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

        if (repo.TryGet(authorId, eventKey, out var data)) {
            // postYn이 true일 때만 트윗 UI 생성
            if (data.postYn) {
                feed.SpawnFromData(data);

                XNotificationManager.Instance.Show(data.name, data.key);
            }

            // 마켓 및 코인 효과 적용
            ApplyTweetEffect(data);

            // 지속 등록
            activeEffects.Add((data, coinManager.CurrentDateTime));

            // 한 번 실행한 이벤트는 다시 실행 안 함
            executedEvents.Add(eventKey);

            // 실행 시각으로 date/time 필드 덮어쓰기
            data.date = coinManager.CurrentDateTime.ToString("MM/dd/yyyy");
            data.time = coinManager.CurrentDateTime.ToString("HH:mm");


            Debug.Log($"[트윗 실행] {authorId}/{eventKey}");
        } else {
            Debug.LogWarning($"Lookup failed: {authorId} / {eventKey}");
        }
    }

    private void ApplyTweetEffect(XPostData data) {
        HashSet<string> individuallyHandled = new();

        if (data.targetGroups != null) 
        {
            foreach (var group in data.targetGroups) {
                // 상장 이벤트 처리
                if (!string.IsNullOrEmpty(data.eventType) && data.eventType == "Listing") // 상장
                {
                    foreach (var symbol in group.symbols) {
                        coinManager.ListNewCoin(symbol);
                    }
                    continue; // 상장 이벤트는 가격 변동 없이 여기서 끝
                }

                // 기존 가격/페이즈 변동 처리
                foreach (var symbol in group.symbols) {
                    var coin = coinManager.coins.Find(c => c.Symbol == symbol);
                    if (coin == null) continue;

                    individuallyHandled.Add(symbol);

                    float percent = UnityEngine.Random.Range(group.priceChangeMin, group.priceChangeMax);
                    coin.CurrentPrice *= 1f + percent / 100f;

                    if (!string.IsNullOrEmpty(group.marketPhaseToSet))
                    {
                        coin.CurrentPhaseOverride = ParsePhase(group.marketPhaseToSet);
                        coin.PhaseOverrideEndTime = coinManager.CurrentDateTime.AddHours(group.durationHours);
                    }
                }
            }
        }

        // 2. 전체 마켓 페이즈 적용 (개별 코인에 포함되지 않은 경우에만)
        if (data.overrideMarketPhase && !string.IsNullOrEmpty(data.marketPhaseToSet)) 
        {
            MarketPhase parsedPhase = ParsePhase(data.marketPhaseToSet);
            coinManager.CurrentMarket = parsedPhase;
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
}