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

            // [수정 포인트 A] 전체 코인에 대해 '시나리오 패턴' 강제 할당
            // "불장이다" -> "모든 코인은 이제부터 30% 상승(혹은 페이즈별 배율)을 향해 간다"
            if (ChartPatternEngine.Instance != null) {
                float duration = data.globalMarketPhase.durationHours;
                MarketPhase phase = data.globalMarketPhase.phase;

                // 페이즈별 기본 목표 배율 가져오기 (예: SuperBull = 1.3배)
                double targetMult = GetDefaultMultiplier(phase);

                foreach (var coin in coinManager.coins) {
                    if (!coin.IsListed || coin.IsDelisted || coin.Type == CoinType.Stable) continue;

                    // 변동성(Volatility)에 따라 목표가 차등 적용 (잡코인은 더 크게)
                    double coinMult = ApplyVolatilityToMultiplier(targetMult, coin.Volatility);

                    // 엔진에게 명령: "이 코인, 이 시간동안, 이 배율까지 패턴 그려라"
                    ChartPatternEngine.Instance.AssignScenarioPattern(
                        coin, phase, duration, coinMult
                    );
                }
                Debug.Log($"[Effect] 글로벌 시나리오 패턴 발동: {phase}, 목표배율 약 x{targetMult:F2}");
            }

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
            // [보완] 세그먼트 리스트와 개별 심볼 리스트를 합칩니다.
            List<string> targetSymbols = new List<string>();

            if (group.targetSegment != TargetSegment.None) {
                targetSymbols.AddRange(GetSymbolsBySegment(group.targetSegment));
            }

            if (group.symbols != null && group.symbols.Count > 0) {
                // 중복 심볼 방지하며 추가
                foreach (var s in group.symbols) {
                    if (!targetSymbols.Contains(s)) targetSymbols.Add(s);
                }
            }

            // 아무 타겟도 없을 때의 기본 처리 (Stable/Trash 제외 전체)
            if (targetSymbols.Count == 0 && group.targetSegment == TargetSegment.None) {
                targetSymbols = coinManager.coins
                    .Where(c => c.Class != CoinClass.Trash && c.Class != CoinClass.Stable)
                    .Select(c => c.Symbol).ToList();
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
                    // [추가 필요] 이 코드가 있어야 엔진이 "아 가격이 바뀌었네?" 하고 즉시 인지합니다.
                    coin.OnPriceUpdate(coin.CurrentPrice);

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
                    MarketPhase phase = (MarketPhase)group.targetMarketPhase;
                    coin.CurrentPhaseOverride = phase;
                    coin.PhaseOverrideEndTime = now.AddHours(group.durationHours);

                    // [핵심 수정] 펌핑된 가격을 '시작점'으로 하는 새로운 패턴을 즉시 부여!
                    // 이렇게 해야 엔진이 펌핑된 가격을 "정상"으로 인식하고 위로 더 쏩니다.
                    if (ChartPatternEngine.Instance != null) {
                        double targetMult = GetDefaultMultiplier(phase);
                        double coinMult = ApplyVolatilityToMultiplier(targetMult, coin.Volatility);

                        ChartPatternEngine.Instance.AssignScenarioPattern(
                            coin, phase, group.durationHours, coinMult
                        );
                        Debug.Log($"[Effect] {symbol} 펌핑 후 시나리오 패턴 재설정 (목표: x{coinMult})");
                    }
                }
            }
        }
    }
    private double GetDefaultMultiplier(MarketPhase phase) {
        return phase switch {
            MarketPhase.MegaBull => 1.50,  // +50%
            MarketPhase.SuperBull => 1.30, // +30% (행님 요구사항)
            MarketPhase.BigBull => 1.15,   // +15%
            MarketPhase.Bull => 1.08,      // +8%
            MarketPhase.Sideways => 1.0,   // 0%
            MarketPhase.Bear => 0.90,      // -10%
            MarketPhase.BigBear => 0.70,   // -30%
            MarketPhase.SuperBear => 0.50, // -50%
            MarketPhase.MegaBear => 0.30,  // -70%
            _ => 1.0
        };
    }

    // [헬퍼 함수 추가] 변동성 적용
    private double ApplyVolatilityToMultiplier(double baseMult, int volatility) {
        // baseMult가 1.5(MegaBull)일 때
        if (baseMult > 1.0) {
            // [수정] 안정도(volatility)가 낮으면 목표가 상승분(0.5)을 대폭 삭감합니다.
            // Vol 1(비트코인)은 상승분의 40%만 반영 (예: 1.5 -> 1.2로 하향 조정)
            // Vol 5(알트)는 100% 반영, Vol 8(잡코인)은 160% 반영
            double boost = 0.2 + (volatility * 0.2);
            return 1.0 + (baseMult - 1.0) * boost;
        } else if (baseMult < 1.0) {
            double dropScale = 0.2 + (volatility * 0.2);
            return 1.0 - (1.0 - baseMult) * dropScale;
        }
        return 1.0;
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
    // =========================================================
    // [신규] 세그먼트 자동 분류 로직
    // =========================================================
    private List<string> GetSymbolsBySegment(TargetSegment segment) {

        // 1. 현재 상장된 유효 코인 리스트
        var activeCoins = coinManager.coins.Where(c => c.IsListed && !c.IsDelisted).ToList();
        List<string> result = new List<string>();

        switch (segment) {
            case TargetSegment.Total1:
                // 비트 + 이더 + 모든 알트 (Trash, Stable 제외)
                result = activeCoins
                    .Where(c => c.Class != CoinClass.Stable && c.Class != CoinClass.Trash)
                    .Select(c => c.Symbol).ToList();
                break;

            case TargetSegment.Alternative1:
                // 이더 + 모든 알트 (BTC 제외, Trash/Stable 당연히 제외)
                result = activeCoins
                    .Where(c => c.Symbol != "BTC" && c.Class != CoinClass.Stable && c.Class != CoinClass.Trash)
                    .Select(c => c.Symbol).ToList();
                break;

            case TargetSegment.Alternative2:
                // 비트, 이더 제외 모든 알트 (Trash/Stable 제외)
                result = activeCoins
                    .Where(c => c.Symbol != "BTC" && c.Symbol != "ETH" && c.Class != CoinClass.Stable && c.Class != CoinClass.Trash)
                    .Select(c => c.Symbol).ToList();
                break;

            case TargetSegment.Trash:
                // 오직 Trash 등급만
                result = activeCoins
                    .Where(c => c.Class == CoinClass.Trash)
                    .Select(c => c.Symbol).ToList();
                break;
        }
        return result;
    }
}
