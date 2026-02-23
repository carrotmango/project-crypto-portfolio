using UnityEngine;

public class EconomyTestController : MonoBehaviour {
    [Header("테스트 설정")]
    public double testTargetRate = 1450.0;
    public double normalTargetRate = 1350.0;

    [Header("차트 갱신 설정")]
    public float updateInterval = 0.5f;
    private float timer = 0f;

    void Update() {
        if (Input.GetKeyDown(KeyCode.T)) {
            GlobalEconomyManager.TargetUsdToKrw = testTargetRate;

            if (CoinManager.Instance != null) {
                // 이제 Normal이 아니라 SuperFast로 쏩니다!
                CoinManager.Instance.SetTimeSpeed(TimeSpeed.test);
                Debug.Log("<color=red>초고속 테스트 모드 시작!</color>");
            }
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            GlobalEconomyManager.TargetUsdToKrw = normalTargetRate;

            if (CoinManager.Instance != null) {
                CoinManager.Instance.SetTimeSpeed(TimeSpeed.Normal);
                Debug.Log("<color=cyan>정상 속도 복구</color>");
            }
        }
    }

    //void FixedUpdate() {
    //    GlobalEconomyManager.TickExchangeRate();

    //    timer += Time.fixedDeltaTime;
    //    if (timer >= updateInterval) {
    //        if (ExchangeChartManager.Instance != null) {
    //            ExchangeChartManager.Instance.AddPriceData((float)GlobalEconomyManager.UsdToKrw);
    //        }
    //        timer = 0f;
    //    }
    //}
}