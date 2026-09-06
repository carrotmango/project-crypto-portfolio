using System.Collections.Generic;
using System;

[Serializable]
public class TargetGroupEffect {
    public TargetSegment targetSegment;

    public List<string> symbols;

    // 0 = Listing(상장), 1 = Delisting(상폐), 2 = Relisting(재상장), 3 = Rename(이름변경)  4 = Halt(거래정지), 5 = Resume(거래재개)
    public int coinEventType = -1;

    // -1 기본,  1 = 유의 , 2 위험 
    public int coinAlertType = -1;
    public int eventType;
    public double relistBasePrice;

    // 0: Layer1, 1: Layer2, 2: Meme, 3: AI, 4: RWA, 5: ZK, 6: DeFi, 7: Stable
    public int targetTheme = -1;

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
