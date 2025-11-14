using UnityEngine;
using System;
using System.Collections.Generic;

public class EventOrchestrator : MonoBehaviour {
    [Header("레퍼런스")]
    public XPostRepository repo;
    public XFeedSpawner feed;
    public CoinManager coinManager;

    private List<(XPostData data, DateTime startTime)> activeEffects = new();
    private HashSet<string> executedEvents = new(); // 중복 실행 방지용

    private void Update() {
        DateTime now = coinManager.CurrentDateTime;

        // 이벤트 지속 시간 체크
        for (int i = activeEffects.Count - 1; i >= 0; i--) {
            var (data, startTime) = activeEffects[i];
            DateTime endTime = startTime.AddHours(data.durationHours);

            if (now >= endTime) {
                EndTweetEffect(data);
                activeEffects.RemoveAt(i);
            }
        }

        CheckAndSpawnRandomEvents();
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

    private void ApplyTweetEffect(XPostData data)
    {
        HashSet<string> individuallyHandled = new();

        // 1. 개별 코인 그룹(targetGroups)
        if (data.targetGroups != null) // 수정됨: = 를 != 로 변경
        {
            foreach (var group in data.targetGroups)
            {
                // 상장 이벤트 처리
                if (!string.IsNullOrEmpty(data.eventType) && data.eventType == "Listing") // 수정됨: ! 추가
                {
                    foreach (var symbol in group.symbols)
                    {
                        coinManager.ListNewCoin(symbol);
                    }
                    continue; // 상장 이벤트는 가격 변동 없이 여기서 끝
                }

                // 기존 가격/페이즈 변동 처리
                foreach (var symbol in group.symbols)
                {
                    var coin = coinManager.coins.Find(c => c.Symbol == symbol);
                    if (coin == null) continue;

                    individuallyHandled.Add(symbol);

                    float percent = UnityEngine.Random.Range(group.priceChangeMin, group.priceChangeMax);
                    coin.CurrentPrice *= 1f + percent / 100f;

                    /*Debug.Log($"[개별 코인 영향] {coin.Symbol} +{percent}%");*/

                    if (!string.IsNullOrEmpty(group.marketPhaseToSet)) // 수정됨: ! 추가
                    {
                        coin.CurrentPhaseOverride = ParsePhase(group.marketPhaseToSet);
                        coin.PhaseOverrideEndTime = coinManager.CurrentDateTime.AddHours(group.durationHours);
                        /*Debug.Log($"[개별 코인 페이즈] {coin.Symbol} → {coin.CurrentPhaseOverride} ({group.durationHours}시간)");*/
                    }
                }
            }
        }

        // 2. 전체 마켓 페이즈 적용 (개별 코인에 포함되지 않은 경우에만)
        if (data.overrideMarketPhase && !string.IsNullOrEmpty(data.marketPhaseToSet)) // 수정됨: ! 추가
        {
            MarketPhase parsedPhase = ParsePhase(data.marketPhaseToSet);
            coinManager.CurrentMarket = parsedPhase;

            /*Debug.Log($"[전체 마켓 페이즈 적용] → {coinManager.CurrentMarket} (개별 코인 제외)");*/
        }
    }



    private void EndTweetEffect(XPostData data) {
        MarketPhase afterPhase = ParsePhase(data.marketPhaseAfter, MarketPhase.Sideways);
        coinManager.CurrentMarket = afterPhase;
        /*Debug.Log($"[시장 페이즈 종료 → 복귀] {afterPhase}");*/
    }

    private MarketPhase ParsePhase(string value, MarketPhase fallback = MarketPhase.Sideways) {
        if (!string.IsNullOrEmpty(value) && Enum.TryParse(value, out MarketPhase parsed)) {
            return parsed;
        }
        return fallback;
    }

    public void CheckAndSpawnRandomEvents() {
        DateTime now = coinManager.CurrentDateTime;

        foreach (var author in repo.authors) {
            foreach (var data in author.events) {
                if (executedEvents.Contains(data.key)) continue;

                if (DateTime.TryParse(data.startDate, out var start) &&
                    DateTime.TryParse(data.endDate, out var end)) {

                    if (now >= start && now <= end) {
                        if (data.eventMustRequired || UnityEngine.Random.value < 0.02f) {
                            RunTweetEvent(author.id, data.key);
                        }
                    }
                }
            }
        }
    }

    //[ContextMenu("TEST: Elon Event 1")]
    //public void Test_Elon1() => RunTweetEvent("elon", "elonevent1");

    //[ContextMenu("TEST: Elon Event 2")]
    //public void Test_Elon2() => RunTweetEvent("elon", "elonevent2");

    //[ContextMenu("TEST: Trump Event 1")]
    //public void Test_Trump1() => RunTweetEvent("trump", "trumpevent1");
}