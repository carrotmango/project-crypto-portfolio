using System;
using UnityEngine;

public static class GlobalEconomyManager {
    public static double UsdToKrw = 1350.0;
    public static double TargetUsdToKrw = 1350.0; // 뉴스/이벤트로 설정될 목표 환율
    public static double BaseRate = 1350.0;

    // 매 틱마다 호출하여 환율을 미세하게 조정
    public static void TickExchangeRate() {
        // 목표가로 수렴하려는 성질 (뉴스 연동용)
        double convergenceSpeed = 0.05;
        UsdToKrw = Mathf.Lerp((float)UsdToKrw, (float)TargetUsdToKrw, (float)convergenceSpeed);

        // 평상시 랜덤 잔파동 (±0.5원 정도)
        double noise = UnityEngine.Random.Range(-0.5f, 0.5f);
        UsdToKrw += noise;

        // 너무 과도한 이탈 방지 (평상시 1100~1600원 사이 유지)
        UsdToKrw = Math.Clamp(UsdToKrw, 1100.0, 1600.0);
    }
}