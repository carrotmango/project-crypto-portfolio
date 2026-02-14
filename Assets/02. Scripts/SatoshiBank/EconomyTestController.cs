using UnityEngine;

public class EconomyTestController : MonoBehaviour {
    [Header("테스트 설정")]
    public double testTargetRate = 1450.0;
    public double normalTargetRate = 1350.0;

    [Header("차트 갱신 설정")]
    public float updateInterval = 0.5f; // 0.5초마다 차트 데이터 추가
    private float timer = 0f;

    void Update() {
        if (Input.GetKeyDown(KeyCode.T)) {
            GlobalEconomyManager.TargetUsdToKrw = testTargetRate;
            Debug.Log($"<color=red>이벤트 발생:</color> 목표 환율이 {testTargetRate}원으로 설정되었습니다.");
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            GlobalEconomyManager.TargetUsdToKrw = normalTargetRate;
            Debug.Log($"<color=cyan>이벤트 종료:</color> 목표 환율이 {normalTargetRate}원으로 복구되었습니다.");
        }
    }

    void FixedUpdate() {
        // 환율 계산 로직은 물리 프레임마다 정교하게 돌려줍니다.
        GlobalEconomyManager.TickExchangeRate();

        // 차트 데이터 추가 및 렌더링은 타이머에 맞춰서 수행합니다.
        timer += Time.fixedDeltaTime;
        if (timer >= updateInterval) {
            if (ExchangeChartManager.Instance != null) {
                ExchangeChartManager.Instance.AddPriceData((float)GlobalEconomyManager.UsdToKrw);
            }
            timer = 0f; // 타이머 초기화
        }
    }
}