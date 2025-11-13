using System;
using System.Collections.Generic;
using UnityEngine;
using static ChartRenderer;

public class CoinData {
    public string Name;
    public string Symbol;
    public double CurrentPrice;
    public double InitialPrice;
    public double Supply;

    public MarketPhase CurrentPhaseOverride = MarketPhase.Sideways;
    public DateTime PhaseOverrideEndTime = DateTime.MinValue;


    public List<double> PriceHistory = new();
    public Dictionary<string, object> Attributes = new();
    public List<CandleData> CandleHistory = new();

    private double? currentOpen = null;
    private double currentHigh = double.MinValue;
    private double currentLow = double.MaxValue;

    public CoinData(string name, string symbol, double startPrice, double supply) {
        Name = name;
        Symbol = symbol;
        InitialPrice = startPrice;
        CurrentPrice = startPrice;
        Supply = supply;
        PriceHistory.Add(startPrice);
    }
    

    public void GenerateNextPrice(MarketPhase inputPhase, float maxChangePct = 1f, float externalBias = 0f) {
        MarketPhase phase = Symbol == "MOVE" ? GetRandomMovePhase() : inputPhase;

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

        double baseBias = phase switch {
            MarketPhase.MegaBull => 0.02,
            MarketPhase.SuperBull => 0.01,
            MarketPhase.BigBull => 0.005,
            MarketPhase.Bull => 0.0025,
            MarketPhase.MildBull => 0.0015,
            MarketPhase.Sideways => 0,
            MarketPhase.MildBear => -0.002,
            MarketPhase.Bear => -0.005,
            MarketPhase.BigBear => -0.01,
            MarketPhase.SuperBear => -0.015,
            MarketPhase.MegaBear => -0.03,
            _ => 0,
        };

        bool isUp = UnityEngine.Random.value < directionBias;
        double magnitude = UnityEngine.Random.Range(0f, maxChangePct);
        double deltaPct = (isUp ? magnitude : -magnitude) + baseBias + externalBias;

        double newPrice = CurrentPrice * (1 + deltaPct / 100.0);
        newPrice = Math.Max(newPrice, 0.01);

        OnPriceUpdate(newPrice);
        PriceHistory.Add(CurrentPrice);

        // 개발용 로그 확인
        /*Debug.Log($"[{Symbol}] {phase} Δ={deltaPct:F3}% → ₩{CurrentPrice:N2}");*/
    }

    public void OnPriceUpdate(double newPrice) {
        CurrentPrice = newPrice;

        if (currentOpen == null) {
            currentOpen = newPrice;
            currentHigh = newPrice;
            currentLow = newPrice;
        } else {
            currentHigh = Math.Max(currentHigh, newPrice);
            currentLow = Math.Min(currentLow, newPrice);
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

        currentOpen = CurrentPrice;
        currentHigh = CurrentPrice;
        currentLow = CurrentPrice;
    }

    private MarketPhase GetRandomMovePhase() {
        float roll = UnityEngine.Random.value;
        if (roll < 0.15f) return MarketPhase.MildBull;
        if (roll < 0.70f) return MarketPhase.Sideways;
        if (roll < 0.85f) return MarketPhase.MildBear;
        if (roll < 0.95f) return MarketPhase.Bear;
        return MarketPhase.BigBear;
    }

    public string GetFormattedPriceKRW() {
        if (CurrentPrice >= 1000)
            return $"₩{CurrentPrice:N0}";
        else if (CurrentPrice >= 100)
            return $"₩{CurrentPrice:N2}";
        else if (CurrentPrice >= 10)
            return $"₩{CurrentPrice:N3}";
        else
            return $"₩{CurrentPrice:N4}";
    }

    public void AddAttribute(string key, object value) {
        Attributes[key] = value;
    }

    public T GetAttribute<T>(string key, T defaultValue = default) {
        if (Attributes.TryGetValue(key, out var value) && value is T t)
            return t;
        return defaultValue;
    }

    public MarketPhase GetEffectivePhase(DateTime now, MarketPhase globalPhase) {
        if (now < PhaseOverrideEndTime) {
            return CurrentPhaseOverride;
        }
        return globalPhase;
    }

}
