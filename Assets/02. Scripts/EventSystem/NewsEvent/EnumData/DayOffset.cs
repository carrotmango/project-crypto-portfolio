using System;

[Serializable]
public class DayOffset {
    public DayOffsetType type;   // None, Fixed, Range

    // Fixed
    public int days;
    //  +
    public int hours;
    public int minutes;


    // Range
    public int min;
    public int max;



    // 옵션
    public bool forceAfterMax;   // max 지나면 무조건 실행
}

public enum DayOffsetType {
    None = 0,   // 날짜 이동 없음
    Fixed = 1,  // 고정 일수
    Range = 2   // 범위 랜덤
}

