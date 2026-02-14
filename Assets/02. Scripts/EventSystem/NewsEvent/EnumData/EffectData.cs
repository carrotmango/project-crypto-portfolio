using System;
using System.Collections.Generic;

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