using UnityEngine;
using System;

public static class EventTimeCalculator {
    public static DateTime Calculate(
        DateTime baseTime,
        DayOffset dayOffset,
        TimeRule timeRule
    ) {
        DateTime result = baseTime;

        // 1. DayOffset 적용
        if (dayOffset != null) {
            switch (dayOffset.type) {
                case DayOffsetType.None:
                    // 날짜 이동 없음
                    break;

                case DayOffsetType.Fixed:
                    result = result.AddDays(dayOffset.days);
                    break;

                case DayOffsetType.Range:
                    int d = UnityEngine.Random.Range(dayOffset.min, dayOffset.max + 1);
                    result = result.AddDays(d);
                    break;
            }
        }

        // 2. TimeRule 적용 (시간 덮어쓰기)
        if (timeRule != null) {
            switch (timeRule.type) {
                case TimeRuleType.Any:
                    // 시간 보정 없음
                    break;

                case TimeRuleType.Fixed:
                    result = new DateTime(
                        result.Year,
                        result.Month,
                        result.Day,
                        timeRule.hour,
                        timeRule.minute,
                        0
                    );
                    break;

                case TimeRuleType.Range:
                    int h = UnityEngine.Random.Range(
                        timeRule.startHour,
                        timeRule.endHour + 1
                    );
                    result = new DateTime(
                        result.Year,
                        result.Month,
                        result.Day,
                        h,
                        0,
                        0
                    );
                    break;
            }
        }

        // 3. Range + forceAfterMax 안전장치
        if (dayOffset != null &&
            dayOffset.type == DayOffsetType.Range &&
            dayOffset.forceAfterMax &&
            result <= baseTime) {

            // 최소한 baseTime 이후로 보정
            result = baseTime.AddMinutes(1);
        }

        return result;
    }
}
