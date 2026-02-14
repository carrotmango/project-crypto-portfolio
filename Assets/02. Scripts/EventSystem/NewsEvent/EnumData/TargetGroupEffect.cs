using System.Collections.Generic;
using System;

[Serializable]
public class TargetGroupEffect {
    public List<string> symbols;

    // 0 = Listing, 1 = Delisting, 2 = Rename
    public int coinEventType = -1;
    public int eventType;
    public double relistBasePrice;

    // Rename 전용
    public string newSymbol;
    public string newName;

    // 시장/가격 영향
    public int targetMarketPhase = -1;
    public float priceChangeMin;
    public float priceChangeMax;

    // 지속 시간
    public int durationHours;
}
