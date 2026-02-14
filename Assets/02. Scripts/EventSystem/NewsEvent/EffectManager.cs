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

        // =========================================================
        // 1. 환율 조작 (독립 실행)
        // =========================================================
        if (data.exchangeRateEffect != null) {
            double finalTarget = UnityEngine.Random.Range(
                (float)data.exchangeRateEffect.targetRateMin,
                (float)data.exchangeRateEffect.targetRateMax
            );

            // [핵심 수정] 목표가가 0원(또는 비정상적으로 낮은 값)이면 적용하지 않음!
            // 보통 환율이 500원 밑으로 갈 일은 없으므로 안전하게 100~500 정도로 커트라인 설정
            if (finalTarget > 100.0) {
                GlobalEconomyManager.TargetUsdToKrw = finalTarget;
                Debug.Log($"[Effect] 환율 조작 시작: 목표가 {finalTarget:F2}원");
            } else {
                // 값이 0으로 들어왔다면 로그만 찍고 무시 (기존 환율 유지)
                // Debug.LogWarning($"[Effect] 환율 데이터가 0입니다. 변동을 스킵합니다. (입력값: {finalTarget})");
            }
        }

        // =========================================================
        // 2. 글로벌 마켓 페이즈 (독립 실행)
        // =========================================================
        if (data.globalMarketPhase != null) {
            coinManager.CurrentMarket = data.globalMarketPhase.phase;

            Debug.Log(
                $"[Effect] GlobalMarketPhase → {data.globalMarketPhase.phase} " +
                $"({data.globalMarketPhase.durationHours}h)"
            );
        }

        // =========================================================
        // 3. 코인별 효과 (Target Groups)
        // =========================================================
        if (data.targetGroups == null) return;

        foreach (var group in data.targetGroups) {

            // [수정 포인트 1] 심볼 리스트가 비어있으면 '전체 코인'을 타겟으로 설정
            List<string> targetSymbols = new List<string>();

            if (group.symbols == null || group.symbols.Count == 0) {
                // 현재 존재하는 모든 코인의 심볼을 가져옴
                targetSymbols = coinManager.coins.Select(c => c.Symbol).ToList();
            } else {
                // 지정된 코인만 가져옴
                targetSymbols = group.symbols;
            }

            foreach (var symbol in targetSymbols) {

                // -------------------------------------------------
                // A. 상태 변경 이벤트 (Listing, Delisting 등)
                // -------------------------------------------------
                // 상태 변경은 코인 객체가 없어도 실행될 수 있으므로(상장 등) 먼저 처리
                if (group.coinEventType >= 0) {
                    CoinEventType eventType = (CoinEventType)group.coinEventType;

                    switch (eventType) {
                        case CoinEventType.Listing:
                            coinManager.ListNewCoin(symbol);
                            Debug.Log($"[Effect] 신규 상장: {symbol}");
                            continue;

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

                // -------------------------------------------------
                // B. 코인 객체 가져오기 (가격/페이즈 조작용)
                // -------------------------------------------------
                var coin = coinManager.coins.FirstOrDefault(c => c.Symbol == symbol);

                if (coin == null) {
                    // 상장/폐지 이벤트가 아닌데 코인이 없으면 스킵
                    if (group.coinEventType < 0) {
                        // Debug.LogWarning($"[Effect] 코인 없음 (가격/페이즈 스킵): {symbol}");
                    }
                    continue;
                }

                // -------------------------------------------------
                // C. 가격 변동 (변동성 레벨 반영 로직 추가)
                // -------------------------------------------------
                float basePercent = UnityEngine.Random.Range(
                    group.priceChangeMin,
                    group.priceChangeMax
                );

                if (Math.Abs(basePercent) > 0.0001f) {
                    // [수정 포인트 2] 변동성 레벨(Volatility) 가져오기
                    var meta = CoinMetaDatabase.AllCoins.FirstOrDefault(m => m.Symbol == symbol);
                    int volatility = meta != null ? meta.VolatilityLevel : 3; // 기본값 3

                    // [수정 포인트 3] 레벨별 가중치 계산 (Lv1: 0.7배 ~ Lv5: 1.5배)
                    float volatilityMultiplier = 0.5f + (0.2f * volatility);
                    float finalPercent = basePercent * volatilityMultiplier;

                    double before = coin.CurrentPrice;
                    coin.CurrentPrice *= 1.0 + finalPercent / 100.0;

                    Debug.Log(
                        $"[Effect] {symbol}(Lv{volatility}) " +
                        $"설정:{basePercent:F1}% x {volatilityMultiplier:F1} = {finalPercent:F1}% | " +
                        $"{before:N0} → {coin.CurrentPrice:N0}"
                    );
                }

                // -------------------------------------------------
                // D. 개별 코인 페이즈 오버라이드
                // -------------------------------------------------
                if (group.targetMarketPhase >= 0) {
                    // 0 이상일 때만 실제 Enum으로 변환하여 적용
                    coin.CurrentPhaseOverride = (MarketPhase)group.targetMarketPhase;
                    coin.PhaseOverrideEndTime = now.AddHours(group.durationHours);

                    Debug.Log(
                        $"[Effect] {symbol} 페이즈 변경 → " +
                        $"{(MarketPhase)group.targetMarketPhase} ({group.durationHours}h)"
                    );
                }
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
