using System;

[Serializable]
public class TimeRule {
    public TimeRuleType type;   // Any, Fixed, Range

    // Fixed
    public int hour;
    public int minute;

    // Range
    public int startHour;
    public int endHour;
}

public enum TimeRuleType {
    Any = 0,   // 시간 보정 없음 (기준 시간 유지)
    Fixed = 1,   // 특정 시각
    Range = 2    // 시간 범위 랜덤
}

