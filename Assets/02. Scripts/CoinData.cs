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
    public long CirculatingSupply; // 실시간 유통량 (매일 늘어날 변수)
    public long MaxSupply;         // 최대 발행 가능량 (게이지의 100% 기준)
    public double Supply;          // (기존 변수 유지용)
    public CoinType Type = CoinType.Normal;
    public double FixedDollarValue = 1.0;
    public bool IsDelisted = false;
    public string Theme;          // 테마 (문자열로 저장)
    public int Volatility;        // 변동성 레벨
    public string Description;    // 설명
    public bool IsListed;         // 현재 상장 여부
    public bool IsActiveListed;
    public double AllTimeHigh; // 역대 최고가
    public double AllTimeLow;  // 역대 최저가

    // ===== 기존 구조 유지 =====
    public const int MaxCandleHistory = 35;
    public MarketPhase CurrentPhaseOverride = MarketPhase.Sideways;
    public DateTime PhaseOverrideEndTime = DateTime.MinValue;

    // [신규 추가] 락업 & 반감기 전용 독립 가격 개입 변수 (창과 방패)
    public double lockupAndHalvingBias = 0;
    public DateTime lockupAndHalvingBiasEndTime = DateTime.MinValue;

    public List<double> PriceHistory = new();
    public List<double> DailyHistory = new List<double>();
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

    // ===== 생성자 (최종 통합 버전) =====
    public CoinData(CoinMetaData meta) {
        Name = meta.Name;
        Symbol = meta.Symbol;
        CirculatingSupply = meta.CirculatingSupply;
        MaxSupply = meta.MaxSupply;
        Supply = MaxSupply;
        Volatility = meta.VolatilityLevel;
        Description = meta.Description;

        Theme = meta.Theme.ToString();

        if (meta.Theme == CoinTheme.Stable) {
            Type = CoinType.Stable;
            FixedDollarValue = 1.0;
            CurrentPrice = FixedDollarValue * GlobalEconomyManager.UsdToKrw;
        } else {
            Type = CoinType.Normal;
            CurrentPrice = meta.InitialPrice;
        }

        InitialPrice = CurrentPrice;
        AllTimeHigh = CurrentPrice;
        AllTimeLow = CurrentPrice;
        PriceHistory.Add(CurrentPrice);

        if (meta.IsDefaultListed) {
            IsActiveListed = true;
        } else if (meta.BullbitListed) {
            IsActiveListed = UnityEngine.Random.value > 0.5f;
        } else {
            IsActiveListed = false;
        }

        IsListed = IsActiveListed;
        IsDelisted = !IsActiveListed;
    }

    public void ApplyRelist(double basePrice) {
        IsDelisted = false;
        CurrentPrice = basePrice > 0 ? basePrice : InitialPrice;
        InitialPrice = CurrentPrice;
        PhaseOverrideEndTime = DateTime.MinValue;
        CurrentPhaseOverride = MarketPhase.Sideways;

        // [신규] 재상장 시 독립 변수도 초기화
        lockupAndHalvingBias = 0;
        lockupAndHalvingBiasEndTime = DateTime.MinValue;

        currentOpen = null;
        currentHigh = double.MinValue;
        currentLow = double.MaxValue;
        CurrentBaseCandle = null;
        BaseCandleHistory.Clear();
    }

    // [핵심 변경] now(현재 게임 시간)를 받아와서 Bias 만료 체크를 합니다.
    public void GenerateNextPrice(MarketPhase inputPhase, int volatilityLevel = 1, float maxChangePct = 1f, float externalBias = 0f, DateTime now = default) {
        if (IsDelisted) return;

        if (Type == CoinType.Stable) {
            double depeggingNoise = UnityEngine.Random.Range(-0.0005f, 0.0005f);
            CurrentPrice = (FixedDollarValue + depeggingNoise) * GlobalEconomyManager.UsdToKrw;
        } else {
            MarketPhase phase = inputPhase;

            // 1. 승률 조절 (격차를 확 줄여서 복리 스노우볼 방지)
            double directionBias = phase switch {
                MarketPhase.MegaBull => 0.65,
                MarketPhase.SuperBull => 0.60,
                MarketPhase.BigBull => 0.57,
                MarketPhase.Bull => 0.54,
                MarketPhase.MildBull => 0.52,
                MarketPhase.Sideways => 0.50, // 완벽한 5:5 보합
                MarketPhase.MildBear => 0.48,
                MarketPhase.Bear => 0.46,
                MarketPhase.BigBear => 0.43,
                MarketPhase.SuperBear => 0.40,
                MarketPhase.MegaBear => 0.35,
                _ => 0.50,
            };

            // 2. 틱당 진폭(%) 현실화 (하루 48틱이므로 매우 작아야 함)
            double baseMagnitude = phase switch {
                MarketPhase.MegaBull or MarketPhase.MegaBear => 1.5,   // 초대형 호재/악재 터졌을 때만 큼
                MarketPhase.SuperBull or MarketPhase.SuperBear => 1.0,
                MarketPhase.BigBull or MarketPhase.BigBear => 0.7,
                _ => 0.3, // Sideways, Mild, Bull 등 평상시는 틱당 0.3% 내외로 잔잔하게
            };

            double volMultiplier = volatilityLevel switch {
                1 => 0.75,
                2 => 1.05,
                3 => 1.8,
                4 => 2.5,
                5 => 3.7,
                6 => 6.0,
                7 => 8.0,
                8 => 15.0,
                _ => 1.0,
            };

            double finalMaxMagnitude = baseMagnitude * volMultiplier;

            // 3. 강제 추세 밀어주기 (Mega 급에서만 발동되도록 제한)
            double baseBias = phase switch {
                MarketPhase.MegaBull => 0.05 * volMultiplier,
                MarketPhase.MegaBear => -0.05 * volMultiplier,
                _ => 0.0 // 나머지는 억지 상승/하락 없음
            };

            bool isUp = UnityEngine.Random.value < directionBias;
            // 캔들 길이 계산 (최소 길이 보장)
            double magnitude = UnityEngine.Random.Range((float)finalMaxMagnitude * 0.1f, (float)finalMaxMagnitude);

            // =========================================================
            // [핵심] 차트 흔들기 (Super, Mega 급에서만 양봉/음봉 길이 차이 발생)
            // Mild나 평상시엔 길이가 비슷해서 차트가 부드럽게 지그재그를 그림
            // =========================================================
            if (phase == MarketPhase.MegaBull || phase == MarketPhase.SuperBull) {
                if (isUp) magnitude *= 1.3; else magnitude *= 0.7; // 상승장 특유의 장대양봉
            } else if (phase == MarketPhase.MegaBear || phase == MarketPhase.SuperBear) {
                if (!isUp) magnitude *= 1.3; else magnitude *= 0.7; // 폭락장 특유의 장대음봉
            }

            // [락업 및 반감기 시나리오 강제 개입 로직 (유지)]
            double activeLockupBias = 0;
            if (now != default && now < lockupAndHalvingBiasEndTime) {
                if (UnityEngine.Random.value < 0.3f) {
                    activeLockupBias = -lockupAndHalvingBias * UnityEngine.Random.Range(0.5f, 1.5f);
                    if (lockupAndHalvingBias > 0) isUp = false;
                    else if (lockupAndHalvingBias < 0) isUp = true;
                } else {
                    activeLockupBias = lockupAndHalvingBias * UnityEngine.Random.Range(0.8f, 1.2f);
                }
            }

            // 최종 가격 연산
            double deltaPct = (isUp ? magnitude : -magnitude) + baseBias + externalBias + activeLockupBias;
            CurrentPrice = CurrentPrice * (1 + deltaPct / 100.0);
            CurrentPrice = Math.Max(CurrentPrice, 0.0001);
        }

        OnPriceUpdate(CurrentPrice);
        PriceHistory.Add(CurrentPrice);
    }

    public void OnPriceUpdate(double newPrice) {
        CurrentPrice = newPrice;
        if (newPrice > AllTimeHigh) AllTimeHigh = newPrice;
        if (newPrice < AllTimeLow) AllTimeLow = newPrice;
        if (currentOpen == null) {
            currentOpen = newPrice; currentHigh = newPrice; currentLow = newPrice;
        } else {
            currentHigh = Math.Max(currentHigh, newPrice);
            currentLow = Math.Min(currentLow, newPrice);
        }
        if (CurrentBaseCandle == null) CurrentBaseCandle = new BaseCandle(newPrice);
        else CurrentBaseCandle.Update(newPrice);

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

    public double GetCurrentPriceUSD() => CurrentPrice / GlobalEconomyManager.UsdToKrw;

    public string GetFormattedPriceUSD(double? customPrice = null) {
        double price = customPrice ?? GetCurrentPriceUSD();
        if (price >= 1000) return $"${price:N2}";
        else if (price >= 1) return $"${price:N2}";
        else return $"${price:N4}";
    }

    public void RecordDailyClosePrice() {
        if (IsDelisted) return;
        DailyHistory.Add(CurrentPrice);
        if (DailyHistory.Count > 100) DailyHistory.RemoveAt(0);
    }
}