using System;
using System.Collections.Generic;

public enum TargetSegment {
    None,
    Total1,       // BTC + ETH + All Alts (전체)
    Alternative1, // ETH + All Alts (비트 제외)
    Alternative2, // All Alts (비트, 이더 제외)
    Trash         // 잡코인 (나중에 구현)
}

[Serializable]
public class EffectData {
    public string key;
    public GlobalMarketPhaseEffect globalMarketPhase;
    public List<TargetGroupEffect> targetGroups;
    public ListEventEffect listEvent;
    public float durationHours;
    public ExchangeRateEffect exchangeRateEffect;


}

[Serializable]
public class ExchangeRateEffect {
    public double targetRateMin; // 최소 목표치
    public double targetRateMax; // 최대 목표치
    public float durationHours;  // 도달 및 유지 시간
}