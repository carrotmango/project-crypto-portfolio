using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class EffectManager : MonoBehaviour {
    public static EffectManager Instance;

    [Header("References")]
    public EffectRepository repo;
    public CoinManager coinManager;

    // 활성 Effect 목록
    private List<(EffectData data, DateTime startTime)> activeEffects = new();

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update() {
        DateTime now = coinManager.CurrentDateTime;

        // 디버그용 (문제 생기면 이거부터 확인)
        // Debug.Log($"[Effect] Update | active={activeEffects.Count}");

        for (int i = activeEffects.Count - 1; i >= 0; i--) {
            var (data, startTime) = activeEffects[i];

            bool shouldEnd = false;

            // -----------------------------
            // 글로벌 마켓 페이즈 종료 체크
            // -----------------------------
            if (data.globalMarketPhase != null) {
                DateTime endTime = startTime.AddHours(
                    data.globalMarketPhase.durationHours
                );

                if (now >= endTime) {
                    Debug.Log($"[Effect] Global 종료 조건 충족: {data.key}");
                    EndEffect(data);
                    shouldEnd = true;
                }
            }

            // -----------------------------
            // Effect 자체 종료 조건
            // (globalMarketPhase 없는 Effect도 종료 가능)
            // -----------------------------
            if (data.durationHours > 0) {
                DateTime endTime = startTime.AddHours(data.durationHours);
                if (now >= endTime) {
                    Debug.Log($"[Effect] Effect 종료 조건 충족: {data.key}");
                    shouldEnd = true;
                }
            }

            if (shouldEnd) {
                Debug.Log($"[Effect] 종료: {data.key}");
                activeEffects.RemoveAt(i);
            }
        }
    }

    // =========================================================
    // 외부 호출
    // =========================================================
    public void Apply(string effectKey) {
        var data = repo.Get(effectKey);
        if (data == null) {
            Debug.LogWarning($"[Effect] effectKey 없음: {effectKey}");
            return;
        }

        DateTime now = coinManager.CurrentDateTime;

        Debug.Log($"[Effect] 적용 시작: {effectKey} @ {now}");

        ApplyEffect(data, now);
        activeEffects.Add((data, now));
    }

    // =========================================================
    // 적용 로직
    // =========================================================
    private void ApplyEffect(EffectData data, DateTime now) {

        // -----------------------------
        // 글로벌 마켓 페이즈
        // -----------------------------
        if (data.globalMarketPhase != null) {
            coinManager.CurrentMarket = data.globalMarketPhase.phase;

            Debug.Log(
                $"[Effect] GlobalMarketPhase → {data.globalMarketPhase.phase} " +
                $"({data.globalMarketPhase.durationHours}h)"
            );
        }

        if (data.targetGroups == null) return;

        foreach (var group in data.targetGroups) {
            foreach (var symbol in group.symbols) {

                CoinEventType eventType = (CoinEventType)group.coinEventType;

                // =============================
                // 상태 변경 이벤트 먼저 처리
                // =============================
                if (group.coinEventType >= 0) {

                    switch (eventType) {

                        case CoinEventType.Listing:
                            coinManager.ListNewCoin(symbol);
                            Debug.Log($"[Effect] 신규 상장: {symbol}");
                            continue; // 여기서 끝

                        case CoinEventType.Delisting:
                            coinManager.DelistCoin(symbol);
                            Debug.Log($"[Effect] 상폐 처리: {symbol}");
                            continue;

                        case CoinEventType.Relisting: {
                                var meta = CoinMetaDatabase.AllCoins
                                    .FirstOrDefault(c => c.Symbol == symbol);

                                if (meta == null) {
                                    Debug.LogError($"[Effect] Relisting 실패 - Meta 없음: {symbol}");
                                    continue;
                                }

                                double basePrice = group.relistBasePrice > 0
                                    ? group.relistBasePrice
                                    : meta.InitialPrice;

                                coinManager.RelistCoin(symbol, basePrice);
                                Debug.Log($"[Effect] 재상장 처리: {symbol}");
                                continue;
                            }



                        case CoinEventType.Rename:
                            Debug.Log($"[Effect] Rename 이벤트 (미구현): {symbol}");
                            continue;
                    }
                }

                // =============================
                // 여기부터는 코인이 반드시 있어야 함
                // =============================
                var coin = coinManager.coins
                    .FirstOrDefault(c => c.Symbol == symbol);

                if (coin == null) {
                    Debug.LogWarning($"[Effect] 코인 없음 (가격/페이즈 스킵): {symbol}");
                    continue;
                }

                // =============================
                // 가격 변동
                // =============================
                float percent = UnityEngine.Random.Range(
                    group.priceChangeMin,
                    group.priceChangeMax
                );

                if (Math.Abs(percent) > 0.0001f) {
                    double before = coin.CurrentPrice;
                    coin.CurrentPrice *= 1.0 + percent / 100.0;

                    Debug.Log(
                        $"[Effect] {symbol} 가격변동 {percent}% | " +
                        $"{before} → {coin.CurrentPrice}"
                    );
                }

                // =============================
                // 개별 코인 페이즈
                // =============================
                coin.CurrentPhaseOverride = group.targetMarketPhase;
                coin.PhaseOverrideEndTime = now.AddHours(group.durationHours);

                Debug.Log(
                    $"[Effect] {symbol} PhaseOverride → " +
                    $"{group.targetMarketPhase} ({group.durationHours}h)"
                );
            }
        }
    }

    // =========================================================
    // 종료 로직
    // =========================================================
    private void EndEffect(EffectData data) {

        // 글로벌 페이즈만 원복
        if (data.globalMarketPhase != null) {
            coinManager.CurrentMarket = MarketPhase.Sideways;

            Debug.Log("[Effect] GlobalMarketPhase 종료 → Sideways");
        }

        // targetGroups 종료는
        // CoinManager에서 PhaseOverrideEndTime으로 처리
    }
}
