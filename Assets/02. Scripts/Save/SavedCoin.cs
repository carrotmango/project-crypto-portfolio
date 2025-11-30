using System;
using System.Collections.Generic;
using static ChartRenderer;

[Serializable]
public class SavedCoin {
    public string symbol;
    public double currentPrice;
    public double initialPrice;
    public bool isListed;

    public List<double> priceHistory = new();
    public List<CandleData> candles = new();

    public MarketPhase phaseOverride;
    public long phaseOverrideEndTicks;
}
