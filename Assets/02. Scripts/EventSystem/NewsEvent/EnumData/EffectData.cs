using System;
using System.Collections.Generic;

[Serializable]
public class EffectData {
    public string key;

    public GlobalMarketPhaseEffect globalMarketPhase;
    public List<TargetGroupEffect> targetGroups;
    public ListEventEffect listEvent;
    public float durationHours;

}
