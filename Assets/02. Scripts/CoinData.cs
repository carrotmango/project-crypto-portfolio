using System;
using System.Collections.Generic;
using UnityEngine;
using static ChartRenderer;

public enum CoinType { Normal, Stable }

public class CoinData {
    public string Name;
    public string Symbol;
    public CoinClass Class;
    public double CurrentPrice;
    public double InitialPrice;
    public long CirculatingSupply;
    public long MaxSupply;
    public double Supply;          // 기존 코드 호환용
    public CoinType Type = CoinType.Normal;
    public double FixedDollarValue = 1.0;
    public bool IsDelisted = false;
    public string Theme;
    public int Volatility;
    public string Description;
    public bool IsListed;
    public bool IsActiveListed;
    public double AllTimeHigh;
    public double AllTimeLow;
    public bool IsTradingSuspended = false;
    public CoinAlertType AlertType = CoinAlertType.None;

    public const int MaxCandleHistory = 35;
    public MarketPhase CurrentPhaseOverride = MarketPhase.Sideways;
    public DateTime PhaseOverrideEndTime = DateTime.MinValue;

    // [중요] 락업 & 반감기 전용 변수 유지
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

    public void SetOwnedAmount(double amount) { OwnedAmount = amount; }

    // ===== 런타임 캔들 구조 =====
    public RuntimeCandle CurrentRuntimeCandle { get; private set; }

    public void EnsureRuntimeCandle(double startPrice) {
        if (CurrentRuntimeCandle != null && !CurrentRuntimeCandle.IsClosed) return;
        CurrentRuntimeCandle = new RuntimeCandle();
        CurrentRuntimeCandle.Start(startPrice);
    }

    public void MarkTradeOnCurrentCandle(TradeType tradeType) {
        if (CurrentRuntimeCandle != null) {
            // 비트 연산으로 누적 (현물 매수와 선물 매수가 한 캔들에 동시에 있을 수도 있으므로)
            CurrentRuntimeCandle.TradeFlag |= tradeType;
            Debug.Log($"[{Symbol}] 런타임 캔들에 {tradeType} 마커 기록 완료!");
        }
    }

    public void CloseRuntimeCandle() {
        if (CurrentRuntimeCandle == null || CurrentRuntimeCandle.IsClosed) return;
        CurrentRuntimeCandle.CloseCandle();

        CandleHistory.Add(new ChartRenderer.CandleData {
            open = (float)CurrentRuntimeCandle.Open,
            high = (float)CurrentRuntimeCandle.High,
            low = (float)CurrentRuntimeCandle.Low,
            close = (float)CurrentRuntimeCandle.Close,
            TradeFlag = CurrentRuntimeCandle.TradeFlag,
            Timestamp = CurrentRuntimeCandle.Timestamp
        });
        CurrentRuntimeCandle = null;
    }

    public class BaseCandle {
        public double open; public double high; public double low; public double close;
        public BaseCandle(double price) { open = price; high = price; low = price; close = price; }
        public void Update(double price) {
            high = Math.Max(high, price); low = Math.Min(low, price); close = price;
        }
    }

    public List<BaseCandle> BaseCandleHistory = new();
    public BaseCandle CurrentBaseCandle;
    public int MaxBaseCandles = 70;

    // ===== 생성자 (Metadata 연동) =====
    public CoinData(CoinMetaData meta) {
        Name = meta.Name; Symbol = meta.Symbol;
        Class = meta.Class;
        CirculatingSupply = meta.CirculatingSupply; MaxSupply = meta.MaxSupply;
        Supply = MaxSupply; Volatility = meta.VolatilityLevel;
        Description = meta.Description; Theme = meta.Theme.ToString();

        if (meta.Theme == CoinTheme.Stable) {
            Type = CoinType.Stable; FixedDollarValue = 1.0;
            CurrentPrice = FixedDollarValue * GlobalEconomyManager.UsdToKrw;
        } else {
            Type = CoinType.Normal; CurrentPrice = meta.InitialPrice;
        }

        InitialPrice = CurrentPrice; AllTimeHigh = CurrentPrice; AllTimeLow = CurrentPrice;
        PriceHistory.Add(CurrentPrice);

        IsActiveListed = meta.IsDefaultListed || (meta.BullbitListed && UnityEngine.Random.value > 0.5f);
        IsListed = IsActiveListed; IsDelisted = !IsActiveListed;
    }

    public void ApplyRelist(double basePrice) {
        IsDelisted = false; CurrentPrice = basePrice > 0 ? basePrice : InitialPrice;
        InitialPrice = CurrentPrice; PhaseOverrideEndTime = DateTime.MinValue;
        CurrentPhaseOverride = MarketPhase.Sideways;
        lockupAndHalvingBias = 0; lockupAndHalvingBiasEndTime = DateTime.MinValue;
        currentOpen = null; currentHigh = double.MinValue; currentLow = double.MaxValue;
        CurrentBaseCandle = null; BaseCandleHistory.Clear();
    }

    // ===== 가격 생성 로직 (30분 틱 밸런스 조정 및 락업 병합) =====
    public void GenerateNextPrice(MarketPhase inputPhase, int volatilityLevel = 1, float maxChangePct = 1f, float externalBias = 0f, DateTime now = default) {
        if (IsDelisted) return;

        if (IsTradingSuspended) {
            OnPriceUpdate(CurrentPrice);
            PriceHistory.Add(CurrentPrice);
            return;
        }

        if (Type == CoinType.Stable) {
            // 스테이블도 너무 일자면 재미없으니 노이즈를 아주 살짝 키움 (0.0005 -> 0.0015)
            double depeggingNoise = UnityEngine.Random.Range(-0.0015f, 0.0015f);
            CurrentPrice = (FixedDollarValue + depeggingNoise) * GlobalEconomyManager.UsdToKrw;
        } else {
            MarketPhase phase = inputPhase;

            // 1. 변동성 배율 (전체적으로 상향 조정하여 캔들 몸통을 키움)
            double volMultiplier = volatilityLevel switch {
                1 => 0.8,  // 비트코인도 최소한의 숨은 쉬게 (0.6 -> 0.8)
                2 => 1.2,
                3 => 1.8,
                4 => 2.5,  // 잡코인은 더 활발하게
                5 => 3.5,
                6 => 5.0,
                7 => 8.0,
                8 => 15.0,
                _ => 1.0,
            };

            // [삭제] 보합장이라고 변동성을 죽이는 로직 제거! (이게 실선 차트의 주범)
            // if (phase == MarketPhase.Sideways) volMultiplier *= 0.7; 

            // 2. [수정] 누락된 Mild 페이즈 추가 및 확률 조정
            (double bias, double probability) = phase switch {
                MarketPhase.MegaBull => (0.0035, 0.65),
                MarketPhase.SuperBull => (0.0040, 0.65),
                MarketPhase.BigBull => (0.0015, 0.60),  // BigBull도 간격 조정을 위해 살짝 상향 (0.56 -> 0.60)
                MarketPhase.Bull => (0.0010, 0.57),     // Bull도 간격 조정을 위해 상향 (0.54 -> 0.57)

                // [신규] 약상승: 승률을 56%로 올리고, 추세 방향성(Bias)을 3배가량 강화
                MarketPhase.MildBull => (0.0008, 0.55),

                MarketPhase.Sideways => (0.0000, 0.50),

                // [신규] 약하락: 승률을 44%로 내리고, 추세 방향성(Bias) 강화
                MarketPhase.MildBear => (-0.0008, 0.45),

                MarketPhase.Bear => (-0.0010, 0.43),    // (0.46 -> 0.43)
                MarketPhase.BigBear => (-0.0015, 0.40), // (0.44 -> 0.40)
                MarketPhase.SuperBear => (-0.015, 0.35),// (0.42 -> 0.35)
                MarketPhase.MegaBear => (-0.0025, 0.35),// (0.38 -> 0.35)
                _ => (0.0, 0.50)
            };
            // 3. [핵심] 기본 진폭(Magnitude) 대폭 상향 (실선 방지)
            // 기존 0.5에서 1.2로 올려서 캔들 몸통을 두껍게 만듭니다.
            double baseMagnitude = 1.2 * volMultiplier;

            // 최소한의 노이즈 보장 (아무리 조용한 장이라도 0.1% 정도는 흔들리게)
            float minNoise = 0.1f;
            double magnitude = UnityEngine.Random.Range(minNoise, (float)baseMagnitude);

            bool isUp = UnityEngine.Random.value < probability;

            // 휩소(속임수) 패턴: 10% 확률로 역추세 발생 (가끔 툭 튀는 꼬리 생성)
            if (UnityEngine.Random.value < 0.10) {
                isUp = !isUp;
                magnitude *= 1.5;
            }

            // 4. 락업 & 반감기 Bias 계산 (기존 유지)
            double activeLockupBias = 0;
            if (now != default && now < lockupAndHalvingBiasEndTime) {
                if (UnityEngine.Random.value < 0.3f) {
                    activeLockupBias = -lockupAndHalvingBias * UnityEngine.Random.Range(0.5f, 1.5f);
                } else {
                    activeLockupBias = lockupAndHalvingBias * UnityEngine.Random.Range(0.8f, 1.2f);
                }
            }

            // 5. 최종 델타 계산
            double finalDeltaPct = (isUp ? magnitude : -magnitude) + (bias * 100) + (activeLockupBias * 100) + externalBias;

            // 6. 가격 적용
            CurrentPrice = CurrentPrice * (1 + finalDeltaPct / 100.0);
            CurrentPrice = Math.Max(CurrentPrice, 0.0001);
        }

        OnPriceUpdate(CurrentPrice);
        PriceHistory.Add(CurrentPrice);
    }

    public void OnPriceUpdate(double newPrice) {
        CurrentPrice = newPrice;
        if (newPrice > AllTimeHigh) AllTimeHigh = newPrice;
        if (newPrice < AllTimeLow) AllTimeLow = newPrice;
        if (currentOpen == null) { currentOpen = newPrice; currentHigh = newPrice; currentLow = newPrice; } else { currentHigh = Math.Max(currentHigh, newPrice); currentLow = Math.Min(currentLow, newPrice); }
        if (CurrentBaseCandle == null) CurrentBaseCandle = new BaseCandle(newPrice);
        else CurrentBaseCandle.Update(newPrice);
        if (CurrentRuntimeCandle != null && !CurrentRuntimeCandle.IsClosed) CurrentRuntimeCandle.UpdatePrice(newPrice);
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