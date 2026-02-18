using System;
using System.Collections.Generic;
using UnityEngine;
using static ChartRenderer;

public enum CoinType { Normal, Stable }

public class CoinData {
    public string Name;
    public string Symbol;
    public double CurrentPrice;
    public double InitialPrice;
    public double Supply;
    public CoinType Type = CoinType.Normal;
    public double FixedDollarValue = 1.0;
    public bool IsDelisted = false;
    public string Theme;          // 테마 (문자열로 저장)
    public int Volatility;        // 변동성 레벨
    public string Description;    // 설명
    public bool IsListed;         // 현재 상장 여부
    public bool IsActiveListed;

    // ===== 기존 구조 유지 =====
    public const int MaxCandleHistory = 35;
    public MarketPhase CurrentPhaseOverride = MarketPhase.Sideways;
    public DateTime PhaseOverrideEndTime = DateTime.MinValue;

    public List<double> PriceHistory = new();
    public Dictionary<string, object> Attributes = new();
    public List<CandleData> CandleHistory = new();

    private double? currentOpen = null;
    private double currentHigh = double.MinValue;
    private double currentLow = double.MaxValue;
    public double OwnedAmount { get; private set; }

    public void SetOwnedAmount(double amount) {
        OwnedAmount = amount;
    }

    // ===== 런타임 캔들 구조 =====
    public RuntimeCandle CurrentRuntimeCandle { get; private set; }

    public void EnsureRuntimeCandle(double startPrice) {
        if (CurrentRuntimeCandle != null && !CurrentRuntimeCandle.IsClosed)
            return;
        CurrentRuntimeCandle = new RuntimeCandle();
        CurrentRuntimeCandle.Start(startPrice);
    }

    public void CloseRuntimeCandle() {
        if (CurrentRuntimeCandle == null || CurrentRuntimeCandle.IsClosed)
            return;
        CurrentRuntimeCandle.CloseCandle();
        CandleHistory.Add(new ChartRenderer.CandleData {
            open = CurrentRuntimeCandle.Open,
            high = CurrentRuntimeCandle.High,
            low = CurrentRuntimeCandle.Low,
            close = CurrentRuntimeCandle.Close
        });
        CurrentRuntimeCandle = null;
    }

    public class BaseCandle {
        public double open;
        public double high;
        public double low;
        public double close;

        public BaseCandle(double price) {
            open = price; high = price; low = price; close = price;
        }

        public void Update(double price) {
            high = Math.Max(high, price);
            low = Math.Min(low, price);
            close = price;
        }
    }

    public List<BaseCandle> BaseCandleHistory = new();
    public BaseCandle CurrentBaseCandle;
    public int MaxBaseCandles = 70;

    // ===== 생성자 (수정됨) =====
    // ===== 생성자 (최종 통합 버전) =====
    public CoinData(CoinMetaData meta) {
        // 1. 기본 정보 복사
        Name = meta.Name;
        Symbol = meta.Symbol;
        Supply = meta.MaxSupply;
        Volatility = meta.VolatilityLevel;
        Description = meta.Description;

        // 2. 테마 저장 (퀘스트 시스템용)
        Theme = meta.Theme.ToString();

        // 3. 스테이블 코인 및 가격 초기화
        if (meta.Theme == CoinTheme.Stable) {
            Type = CoinType.Stable;
            FixedDollarValue = 1.0;
            CurrentPrice = FixedDollarValue * GlobalEconomyManager.UsdToKrw;
        } else {
            Type = CoinType.Normal;
            CurrentPrice = meta.InitialPrice;
        }

        InitialPrice = CurrentPrice;
        PriceHistory.Add(CurrentPrice);

        // 4. 랜덤 상장 로직 (솔라나 등 비상장 문제 해결)
        if (meta.IsDefaultListed) {
            IsActiveListed = true; // 비트코인 등 무조건 상장
        } else if (meta.BullbitListed) {
            // 50% 확률로 이번 판 상장 여부 결정
            IsActiveListed = UnityEngine.Random.value > 0.5f;
        } else {
            IsActiveListed = false; // 이벤트 전용 등
        }

        // 상태 동기화
        IsListed = IsActiveListed;
        IsDelisted = !IsActiveListed;
    }

    public void ApplyRelist(double basePrice) {
        IsDelisted = false;
        CurrentPrice = basePrice > 0 ? basePrice : InitialPrice;
        InitialPrice = CurrentPrice;
        PhaseOverrideEndTime = DateTime.MinValue;
        CurrentPhaseOverride = MarketPhase.Sideways;
        currentOpen = null;
        currentHigh = double.MinValue;
        currentLow = double.MaxValue;
        CurrentBaseCandle = null;
        BaseCandleHistory.Clear();
    }

    // ===== 가격 생성 (핵심 로직 수정) =====
    public void GenerateNextPrice(MarketPhase inputPhase, int volatilityLevel = 1, float maxChangePct = 1f, float externalBias = 0f) {
        if (IsDelisted) return;

        if (Type == CoinType.Stable) {
            // 스테이블 코인: 환율을 추종하며 미세한 노이즈(디페깅) 발생
            double depeggingNoise = UnityEngine.Random.Range(-0.0005f, 0.0005f);
            CurrentPrice = (FixedDollarValue + depeggingNoise) * GlobalEconomyManager.UsdToKrw;
        } else {
            // 일반 코인: 기존 변동성 알고리즘 사용
            MarketPhase phase = inputPhase;

            double directionBias = phase switch {
                MarketPhase.MegaBull => 0.90,
                MarketPhase.SuperBull => 0.75,
                MarketPhase.BigBull => 0.65,
                MarketPhase.Bull => 0.60,
                MarketPhase.MildBull => 0.55,
                MarketPhase.Sideways => 0.5,
                MarketPhase.MildBear => 0.45,
                MarketPhase.Bear => 0.35,
                MarketPhase.BigBear => 0.20,
                MarketPhase.SuperBear => 0.10,
                MarketPhase.MegaBear => 0.05,
                _ => 0.5,
            };

            double baseMagnitude = phase switch {
                MarketPhase.MegaBull => 1.8,
                MarketPhase.MegaBear => 2.2,
                MarketPhase.SuperBull => 1.0,
                MarketPhase.SuperBear => 1.2,
                MarketPhase.Sideways => 0.12,
                MarketPhase.MildBull => 0.25,
                MarketPhase.MildBear => 0.25,
                _ => 0.5,
            };

            double volMultiplier = volatilityLevel switch {
                1 => 0.85,
                2 => 1.25,
                3 => 2.0,
                4 => 3.2,
                5 => 4.5,
                _ => 1.0,
            };

            double finalMaxMagnitude = baseMagnitude * volMultiplier;

            double baseBias = phase switch {
                MarketPhase.MegaBull => 0.06 * volMultiplier,
                MarketPhase.MegaBear => -0.09 * volMultiplier,
                MarketPhase.Sideways => 0,
                _ => (baseMagnitude * 0.02) * volMultiplier
            };

            bool isUp = UnityEngine.Random.value < directionBias;
            double magnitude = UnityEngine.Random.Range(0f, (float)finalMaxMagnitude);
            double deltaPct = (isUp ? magnitude : -magnitude) + baseBias + externalBias;

            CurrentPrice = CurrentPrice * (1 + deltaPct / 100.0);
            CurrentPrice = Math.Max(CurrentPrice, 0.01);
        }

        OnPriceUpdate(CurrentPrice);
        PriceHistory.Add(CurrentPrice);
    }

    public void OnPriceUpdate(double newPrice) {
        CurrentPrice = newPrice;
        if (currentOpen == null) {
            currentOpen = newPrice; currentHigh = newPrice; currentLow = newPrice;
        } else {
            currentHigh = Math.Max(currentHigh, newPrice);
            currentLow = Math.Min(currentLow, newPrice);
        }

        if (CurrentBaseCandle == null) {
            CurrentBaseCandle = new BaseCandle(newPrice);
        } else {
            CurrentBaseCandle.Update(newPrice);
        }

        if (CurrentRuntimeCandle != null && !CurrentRuntimeCandle.IsClosed) {
            CurrentRuntimeCandle.UpdatePrice(newPrice);
        }
    }

    public void RecordCurrentCandle() {
        if (currentOpen == null) return;
        CandleHistory.Add(new CandleData {
            open = (float)currentOpen.Value,
            close = (float)CurrentPrice,
            high = (float)currentHigh,
            low = (float)currentLow
        });
        if (CandleHistory.Count > MaxCandleHistory) CandleHistory.RemoveAt(0);
        currentOpen = CurrentPrice; currentHigh = CurrentPrice; currentLow = CurrentPrice;
    }

    public void CloseBaseCandle() {
        if (CurrentBaseCandle == null) return;
        BaseCandleHistory.Add(CurrentBaseCandle);
        if (BaseCandleHistory.Count > MaxBaseCandles) BaseCandleHistory.RemoveAt(0);
        CurrentBaseCandle = new BaseCandle(CurrentBaseCandle.close);
    }

    public MarketPhase GetEffectivePhase(DateTime now, MarketPhase globalPhase) {
        if (now < PhaseOverrideEndTime) return CurrentPhaseOverride;
        return globalPhase;
    }

    public string GetFormattedPriceKRW() {
        if (CurrentPrice >= 1000) return $"₩{CurrentPrice:N0}";
        else if (CurrentPrice >= 100) return $"₩{CurrentPrice:N2}";
        else if (CurrentPrice >= 10) return $"₩{CurrentPrice:N3}";
        else return $"₩{CurrentPrice:N4}";
    }

    public void AddAttribute(string key, object value) { Attributes[key] = value; }
    public T GetAttribute<T>(string key, T defaultValue = default) {
        if (Attributes.TryGetValue(key, out var value) && value is T t) return t;
        return defaultValue;
    }
    public double GetCurrentPriceUSD() {
        // 현재 원화가격을 실시간 환율로 나눔
        return CurrentPrice / GlobalEconomyManager.UsdToKrw;
    }

    public string GetFormattedPriceUSD(double? customPrice = null) {
        double price = customPrice ?? GetCurrentPriceUSD();
        // 달러 표기법: $1,234.56
        if (price >= 1000) return $"${price:N2}";
        else if (price >= 1) return $"${price:N2}";
        else return $"${price:N4}"; // 소액 코인은 소수점 4자리
    }
}