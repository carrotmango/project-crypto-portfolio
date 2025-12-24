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

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

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

    private void ApplyTweetEffect(XPostData data) {
        HashSet<string> individuallyHandled = new();

        // 1. 개별 코인 그룹(targetGroups)
        if (data.targetGroups != null) // 수정됨: = 를 != 로 변경
        {
            foreach (var group in data.targetGroups) {
                // 상장 이벤트 처리
                if (!string.IsNullOrEmpty(data.eventType) && data.eventType == "Listing") // 수정됨: ! 추가
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

    public MarketPhase ParsePhase(string value, MarketPhase fallback = MarketPhase.Sideways) {
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


    // -------------------------------------------------------------------
    // 디버그: Duration + MarketPhase 변화 로그
    // -------------------------------------------------------------------

    private DateTime lastDebugHour;
    private DateTime lastPhaseCheck;
    private MarketPhase lastLoggedMarketPhase;

    private void LateUpdate() {
        DateTime now = coinManager.CurrentDateTime;

        // DurationHours 디버그
        if (now.Hour != lastDebugHour.Hour || (now - lastDebugHour).TotalHours >= 1) {
            lastDebugHour = now;
            PrintDurationDebug(now);
        }

        // 전체 MarketPhase 디버그 및 변화 감지
        if (now.Hour != lastPhaseCheck.Hour || (now - lastPhaseCheck).TotalHours >= 1) {
            lastPhaseCheck = now;

            MarketPhase current = coinManager.CurrentMarket;

            if (current != lastLoggedMarketPhase) {
                Debug.Log($"[MarketPhase] 변화 감지: {lastLoggedMarketPhase} → {current}");
            }

            Debug.Log($"[MarketPhase] 현재 전체 MarketPhase: {current}");
            lastLoggedMarketPhase = current;
        }
    }

    private void PrintDurationDebug(DateTime now) {
        Debug.Log("========== Duration Debug ==========");

        // 1) 전체 마켓페이즈 Duration
        foreach (var eff in activeEffects) {
            var data = eff.data;

            if (data.overrideMarketPhase && data.durationHours > 0) {
                DateTime end = eff.startTime.AddHours(data.durationHours);
                double remaining = (end - now).TotalHours;
                if (remaining < 0) remaining = 0;

                //Debug.Log($"[MarketPhase] 전체 MarketPhase: {coinManager.CurrentMarket}");
                //Debug.Log($"[MarketPhase] Remaining DurationHours: {remaining:F1}");
                // 마켓페이즈 확인용 로그 중요
            }
        }

        // 2) 개별 코인 페이즈
        //foreach (var coin in coinManager.coins) {
        //    if (coin.PhaseOverrideEndTime > now) {
        //        DateTime end = coin.PhaseOverrideEndTime;
        //        double remaining = (end - now).TotalHours;
        //        if (remaining < 0) remaining = 0;

        //        Debug.Log(
        //            $"[CoinPhase] {coin.Symbol} (개별 페이즈 {coin.CurrentPhaseOverride}) " +
        //            $"Remaining: {remaining:F1}h"
        //        );
        //    }
        //}

        Debug.Log("=====================================");
    }



    //[ContextMenu("TEST: Elon Event 1")]
    //public void Test_Elon1() => RunTweetEvent("elon", "elonevent1");

    //[ContextMenu("TEST: Elon Event 2")]
    //public void Test_Elon2() => RunTweetEvent("elon", "elonevent2");

    //[ContextMenu("TEST: Trump Event 1")]
    //public void Test_Trump1() => RunTweetEvent("trump", "trumpevent1");
}