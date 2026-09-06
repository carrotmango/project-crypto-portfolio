using UnityEngine;
using System.Collections.Generic;
using System;

public enum ChartPatternType {
    None,
    // [상승형 - Bullish]
    ParabolicSurge,     // 무지성 떡상
    AscendingTriangle,  // 상승 삼각형
    BullFlag,           // 깃발형
    CupAndHandle,       // 컵앤핸들
    DoubleBottom,       // 쌍바닥
    InvertedHeadAndShoulders, // 역헤드앤숄더
    GoldenCross,        // 골든크로스

    // [하락형 - Bearish]
    DescendingTriangle, // 하락 삼각형
    BearFlag,           // 하락 깃발
    HeadAndShoulders,   // 헤드앤숄더
    DoubleTop,          // 쌍봉
    DeathCross,         // 데드크로스

    // [횡보/반전형 - Neutral/Reversal]
    SymmetricalTriangle,// 대칭 삼각형
    BoxRange,           // 박스권
    BroadeningWedge     // 확산형
}

public class ChartPatternEngine : MonoBehaviour {
    public static ChartPatternEngine Instance;

    private const int TICKS_PER_HOUR = 2;
    private Dictionary<string, PatternState> activePatterns = new Dictionary<string, PatternState>();

    public class PatternState {
        public ChartPatternType type;
        public double startPrice;
        public double targetPrice;
        public int totalTicks;
        public int currentTick;
        public float noiseFactor;
        public bool isScenarioPattern;
    }

    void Awake() { Instance = this; }

    // ==================================================================================
    // 1. 패턴 할당 로직 (똑똑해진 버전)
    // ==================================================================================

    public void AssignScenarioPattern(CoinData coin, MarketPhase phase, float durationHours, double targetMultiplier) {
        // 시나리오는 무조건 최우선 적용 (덮어쓰기)
        AssignPatternInternal(coin, phase, durationHours, targetMultiplier, isScenario: true);
    }

    public void AssignRandomPattern(CoinData coin, MarketPhase phase, float durationHours) {
        // 1. 시나리오(EffectManager 명령)가 돌고 있으면 절대 건드리지 않음
        if (IsScenarioPatternActive(coin.Symbol)) return;

        // 2. [핵심 수정] 분위기 파악 로직
        // 이미 패턴이 돌고 있는데, 시장 상황이랑 정반대라면? -> 강제 교체!
        if (activePatterns.TryGetValue(coin.Symbol, out PatternState current)) {
            int currentSentiment = GetPatternSentiment(current.type); // 1:상승, -1:하락, 0:중립

            bool isMarketBull = phase <= MarketPhase.BigBull; // 지금 불장인가?
            bool isMarketBear = phase >= MarketPhase.BigBear; // 지금 하락장인가?

            // 하락 패턴 돌고 있는데 시장이 불장이면 -> 교체
            if (currentSentiment < 0 && isMarketBull) {
                // Debug.Log($"[패턴교체] {coin.Symbol}: 하락패턴({current.type}) 중단 -> 불장 반영 시작");
                AssignPatternInternal(coin, phase, durationHours, null, isScenario: false);
                return;
            }

            // 상승 패턴 돌고 있는데 시장이 하락장이면 -> 교체
            if (currentSentiment > 0 && isMarketBear) {
                // Debug.Log($"[패턴교체] {coin.Symbol}: 상승패턴({current.type}) 중단 -> 하락장 반영 시작");
                AssignPatternInternal(coin, phase, durationHours, null, isScenario: false);
                return;
            }
        }

        // 3. 패턴이 없거나 분위기가 비슷하면 -> 그냥 신규 할당 (내부에서 덮어씀)
        AssignPatternInternal(coin, phase, durationHours, null, isScenario: false);
    }

    private void AssignPatternInternal(CoinData coin, MarketPhase phase, float durationHours, double? explicitMultiplier, bool isScenario) {
        int ticks = Mathf.CeilToInt(durationHours * TICKS_PER_HOUR);
        ticks = Mathf.Max(ticks, 2);

        PatternState state = new PatternState();
        state.startPrice = coin.CurrentPrice;
        state.totalTicks = ticks;
        state.currentTick = 0;
        state.noiseFactor = coin.Volatility * 0.05f;
        state.isScenarioPattern = isScenario;

        // 페이즈에 맞는 패턴 선택
        state.type = GetRandomPatternForPhase(phase);

        // 목표가 설정
        double multiplier = explicitMultiplier ?? GetMultiplierForPhase(coin, phase);
        state.targetPrice = coin.CurrentPrice * multiplier;

        if (activePatterns.ContainsKey(coin.Symbol)) {
            activePatterns[coin.Symbol] = state;
        } else {
            activePatterns.Add(coin.Symbol, state);
        }

        PrintDebugLog(coin, phase, state, isScenario);
    }

    // ==================================================================================
    // 2. 틱 업데이트 (자동 청소 기능 추가)
    // ==================================================================================
    public double GetPatternDelta(CoinData coin) {
        if (!activePatterns.TryGetValue(coin.Symbol, out PatternState state)) return 0;

        state.currentTick++;
        float t = (float)state.currentTick / state.totalTicks;
        if (t >= 1.0f) { activePatterns.Remove(coin.Symbol); return 0; }

        // [핵심] 안정도(Volatility Level) 기반 무게추 계산
        // 비트코인(Lv 1)은 0.35배로 아주 무겁게, 잡코인(Lv 8)은 1.4배로 가볍게 무빙
        double stabilityWeight = 0.2 + (coin.Volatility * 0.15);
        bool isBullish = state.targetPrice > state.startPrice;

        double normalizedPos = CalculatePatternCurve(state.type, t);
        double expectedPrice = state.startPrice + (state.targetPrice - state.startPrice) * normalizedPos;

        // [상승장 전용 로직] 펌핑 방어 및 고점 유지
        if (isBullish) {
            if (coin.CurrentPrice > expectedPrice * 1.05) {
                state.startPrice = coin.CurrentPrice;
                state.targetPrice = Math.Max(state.targetPrice, coin.CurrentPrice * 1.02);
            }
            double gapPercent = Math.Max(0, (expectedPrice - coin.CurrentPrice) / coin.CurrentPrice);
            return (gapPercent * (state.isScenarioPattern ? 0.6 : 0.4) * stabilityWeight) +
                   (UnityEngine.Random.Range(-0.001f, 0.001f) * coin.Volatility * 0.1);
        }
        // [하락장/횡보장 로직] 인위적인 고점 지지 없이 부드럽게 목표가로 견인
        else {
            double gapPercent = (expectedPrice - coin.CurrentPrice) / coin.CurrentPrice;
            double pullForce = 0.3 * stabilityWeight; // 하락은 상승보다 좀 더 부드럽게
            return (gapPercent * pullForce) + (UnityEngine.Random.Range(-0.001f, 0.001f) * coin.Volatility * 0.1);
        }
    }

    // ==================================================================================
    // 3. 유틸리티 (성향 분석 함수 추가)
    // ==================================================================================

    // [신규] 패턴이 상승형인지 하락형인지 판별 (-1: 하락, 0: 횡보, 1: 상승)
    private int GetPatternSentiment(ChartPatternType type) {
        switch (type) {
            case ChartPatternType.ParabolicSurge:
            case ChartPatternType.AscendingTriangle:
            case ChartPatternType.BullFlag:
            case ChartPatternType.CupAndHandle:
            case ChartPatternType.DoubleBottom:
            case ChartPatternType.InvertedHeadAndShoulders:
            case ChartPatternType.GoldenCross:
                return 1; // Bullish

            case ChartPatternType.DescendingTriangle:
            case ChartPatternType.BearFlag:
            case ChartPatternType.HeadAndShoulders:
            case ChartPatternType.DoubleTop:
            case ChartPatternType.DeathCross:
                return -1; // Bearish

            default:
                return 0; // Neutral (횡보)
        }
    }

    public bool IsScenarioPatternActive(string symbol) {
        if (activePatterns.TryGetValue(symbol, out PatternState state)) {
            return state.isScenarioPattern && state.currentTick < state.totalTicks;
        }
        return false;
    }

    // ... (CalculatePatternCurve, GetRandomPatternForPhase, GetMultiplierForPhase 등 하단 로직은 기존 코드 그대로 유지) ...
    // (아까 ParabolicSurge 추가해드린 버전 그대로 쓰시면 됩니다)

    private double CalculatePatternCurve(ChartPatternType type, float t) {
        // 모든 곡선은 t=0일 때 0, t=1일 때 1을 반환하도록 정규화 완료
        switch (type) {
            case ChartPatternType.ParabolicSurge: return Mathf.Pow(t, 0.5f);
            case ChartPatternType.AscendingTriangle:
                return t < 0.6f ? t * 0.2f : 0.2f + Mathf.Pow((t - 0.6f) / 0.4f, 2) * 0.8f;
            case ChartPatternType.GoldenCross: return t * t;

            // 하락형 패턴 (0에서 시작해서 1로 부드럽게 수렴)
            case ChartPatternType.DescendingTriangle:
                return t < 0.6f ? t * 0.2f : 0.2f + Mathf.Pow((t - 0.6f) / 0.4f, 2) * 0.8f;
            case ChartPatternType.BearFlag:
                if (t < 0.3f) return t * 1.5f; // 데드캣 바운스(살짝 반등 후 하락)
                else return 0.45f + ((t - 0.3f) / 0.7f) * 0.55f;
            case ChartPatternType.HeadAndShoulders:
                return 0.5f - Mathf.Cos(t * Mathf.PI) * 0.5f; // 부드러운 S자형 하락
            case ChartPatternType.DeathCross:
                return Mathf.Pow(t, 2);
            case ChartPatternType.DoubleTop:
                return t * 0.5f + Mathf.Pow(t, 3) * 0.5f;

            default: return t;
        }
    }

    private ChartPatternType GetRandomPatternForPhase(MarketPhase phase) {
        if (phase <= MarketPhase.BigBull) {
            return PickRandom(ChartPatternType.ParabolicSurge, ChartPatternType.GoldenCross, ChartPatternType.AscendingTriangle);
        } else if (phase >= MarketPhase.BigBear) { // MegaBear, SuperBear, BigBear
            return PickRandom(
                ChartPatternType.DescendingTriangle,
                ChartPatternType.HeadAndShoulders,
                ChartPatternType.DeathCross,
                ChartPatternType.DoubleTop
            );
        } else if (phase > MarketPhase.Sideways) { // MildBear, Bear
            return PickRandom(ChartPatternType.DescendingTriangle, ChartPatternType.BearFlag, ChartPatternType.DeathCross);
        } else if (phase < MarketPhase.Sideways) { // Bull, MildBull
            return PickRandom(ChartPatternType.AscendingTriangle, ChartPatternType.BullFlag, ChartPatternType.DoubleBottom);
        } else return PickRandom(ChartPatternType.BoxRange, ChartPatternType.SymmetricalTriangle);
    }

    private ChartPatternType PickRandom(params ChartPatternType[] types) {
        return types[UnityEngine.Random.Range(0, types.Length)];
    }

    private double GetMultiplierForPhase(CoinData coin, MarketPhase phase) {
        double baseRate = phase switch {
            MarketPhase.MegaBull => 0.40,
            MarketPhase.SuperBull => 0.25,
            MarketPhase.BigBull => 0.15,
            MarketPhase.Bull => 0.08,
            MarketPhase.Sideways => 0.0,
            MarketPhase.Bear => -0.10,
            MarketPhase.MegaBear => -0.40,
            _ => 0.0
        };
        double volMultiplier = 1.0 + (coin.Volatility - 1) * 0.5;
        return 1.0 + (baseRate * volMultiplier);
    }

    private void PrintDebugLog(CoinData coin, MarketPhase phase, PatternState state, bool isScenario) {
        string sourceTag = isScenario ? "<color=yellow>[SCENARIO]</color>" : "<color=cyan>[RANDOM]</color>";
        string priceColor = state.targetPrice >= state.startPrice ? "<color=green>" : "<color=red>";
        Debug.Log($"{sourceTag} <b>{coin.Symbol}</b> ({phase}) → Pattern: <b>{state.type}</b> | {priceColor}목표: {state.targetPrice:N0}</color>");
    }
    public string GetPatternDebugInfo(string symbol) {
        if (activePatterns.TryGetValue(symbol, out PatternState state)) {
            float progress = (float)state.currentTick / state.totalTicks * 100f;
            double hoursLeft = (state.totalTicks - state.currentTick) * 0.5f; // 1틱=0.5시간
            string typeStr = state.isScenarioPattern ? "<color=yellow>[시나리오]</color>" : "[랜덤]";

            // 예: "[시나리오] BullFlag (45%) | 남은: 12.0h | 목표: 1,500"
            return $"{typeStr} <b>{state.type}</b> ({progress:F0}%) | 남은시간: {hoursLeft:F1}h | 목표: {state.targetPrice:N0}";
        }
        return "None (패턴 없음)";
    }
}