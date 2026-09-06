using System;
using UnityEngine;

public static class GlobalEconomyManager {
    public static double UsdToKrw = 1350.0;
    public static double TargetUsdToKrw = 1350.0; // 뉴스/이벤트로 설정될 앵커
    public static double BaseRate = 1350.0;

    public static double BaseInterestRate = 4.25;
    public static double TargetInterestRate = 4.25;

    public static double TaxRate = 10.0;
    public static double TargetTaxRate = 10.0; // 이벤트로 변동될 목표 세율

    // 매 틱(Update/FixedUpdate)마다 호출
    public static void TickExchangeRate() {

        // -----------------------------------------------------------
        // 1. 0.5% 범위 내 랜덤 워크 (Random Walk with Mean Reversion)
        // -----------------------------------------------------------

        // (1) 허용 변동폭 계산 (0.5%)
        // 예: 1350원 기준 약 ±6.75원
        double maxDeviation = TargetUsdToKrw * 0.005;

        // (2) 현재 목표가와의 거리 계산
        double diff = TargetUsdToKrw - UsdToKrw;

        // (3) 복원력(Restoring Force) 계산
        // 목표가에서 멀어질수록 강하게 당겨옵니다. (고무줄 효과)
        // 0.1을 곱하여 서서히 당겨오게 함으로써 급격한 점프 방지
        double pullBack = diff * 0.1f;

        // (4) 랜덤 충격(Random Shock) 생성
        // 차트를 거칠게 만드는 핵심 요소입니다.
        // -1.5 ~ +1.5원 사이로 매 프레임 흔듭니다.
        float randomShock = UnityEngine.Random.Range(-1.5f, 1.5f);

        // (5) 최종 환율 적용
        // 기존 값 + (목표로 당기는 힘) + (랜덤 움직임)
        UsdToKrw += pullBack + randomShock;


        // -----------------------------------------------------------
        // 2. 안전장치 (하드 클램프)
        // -----------------------------------------------------------

        // 0.5% 범위를 절대 벗어나지 않게 하고 싶다면 Clamp 처리
        // (자연스러운 오버슈팅을 원하면 이 부분은 생략 가능)
        double minLimit = TargetUsdToKrw - maxDeviation;
        double maxLimit = TargetUsdToKrw + maxDeviation;

        UsdToKrw = Math.Clamp(UsdToKrw, minLimit, maxLimit);

        // 절대 하한선 (경제 붕괴 방지)
        if (UsdToKrw < 621.0) UsdToKrw = 621.0;


        // -----------------------------------------------------------
        // 3. 금리 로직 (기존 유지)
        // -----------------------------------------------------------
        if (Math.Abs(BaseInterestRate - TargetInterestRate) > 0.001) {
            BaseInterestRate = Mathf.Lerp((float)BaseInterestRate, (float)TargetInterestRate, 0.005f);
        }
        // 금리도 너무 매끈하면 재미없으니 아주 미세한 떨림 추가
        BaseInterestRate += UnityEngine.Random.Range(-0.002f, 0.002f);
        BaseInterestRate = Math.Clamp(BaseInterestRate, 0.0, 20.0);
    }
}