using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class FournanceStatisticsManager : MonoBehaviour {

    [Header("1. 실시간 변동 지표 (Live)")]
    public TextMeshProUGUI totalEquityText;       // 총 선물 자산 (Equity: 원금+PnL)
    public TextMeshProUGUI unrealizedPnLText;     // 미실현 손익 (평가 손익)

    [Header("2. 누적 기록 지표 (Cumulative)")]
    public TextMeshProUGUI realizedPnLText;       // 누적 실현 손익 (확정된 돈)
    public TextMeshProUGUI totalVolumeText;       // 누적 거래량
    public TextMeshProUGUI totalFeeText;          // 누적 수수료

    // 갱신 주기 (너무 빠르면 눈 아프니까 0.1초 정도 추천)
    private WaitForSecondsRealtime refreshDelay = new WaitForSecondsRealtime(0.1f);

    private void OnEnable() {
        // 패널이 켜지면 갱신 루프 시작
        StartCoroutine(RefreshRoutine());
    }

    private void OnDisable() {
        // 패널 꺼지면 루프 자동 중단
        StopAllCoroutines();
    }

    // [핵심] 계속 돌면서 최신 정보를 UI에 뿌려주는 루프
    IEnumerator RefreshRoutine() {
        // 매니저들이 준비될 때까지 잠깐 대기
        while (PlayerManager.Instance == null || FutureChartRenderer.Instance == null || FourNanceManager.Instance == null) {
            yield return null;
        }

        while (true) {
            RefreshUI();
            yield return refreshDelay; // 0.1초 대기
        }
    }

    // 행님이 말씀하신 그 "Refresh 함수"입니다.
    public void RefreshUI() {
        var pm = PlayerManager.Instance;
        var renderer = FutureChartRenderer.Instance;
        var manager = FourNanceManager.Instance;

        if (pm == null || renderer == null || manager == null) return;

        // -------------------------------------------------------------
        // A. PlayerManager에 저장된 '누적 데이터' (과거의 영광)
        // -------------------------------------------------------------

        // 1. 누적 거래량
        if (totalVolumeText != null) {
            totalVolumeText.text = string.Format(LocalizationManager.GetText("LBL_FN_TOTAL_VOL"), pm.fournanceTotalVolume.ToString("N0"));
        }

        // 2. 누적 수수료
        if (totalFeeText != null) {
            totalFeeText.text = string.Format(LocalizationManager.GetText("LBL_FN_TOTAL_FEE"), pm.fournanceTotalFee.ToString("N2"));
        }

        // 3. 누적 실현 손익 (이미 통장에 꽂힌 돈)
        if (realizedPnLText != null) {
            string realizedLabel = LocalizationManager.GetText("LBL_FN_REALIZED_PNL");
            SetColorText(realizedPnLText, realizedLabel, pm.fournanceRealizedPnL);
        }

        // -------------------------------------------------------------
        // B. FutureChartRenderer 등이 계산 중인 '실시간 데이터' (현재 상황)
        // -------------------------------------------------------------

        // 4. 현재 총 자산 (Equity) - FourNanceManager가 계산해 둔 것 가져오기
        if (totalEquityText != null) {
            double currentEquity = manager.GetTotalEquity();
            totalEquityText.text = string.Format(LocalizationManager.GetText("LBL_FN_TOTAL_EQUITY"), currentEquity.ToString("N2"));
        }

        // 5. 현재 평가 손익 (Unrealized PnL) - 차트 렌더러가 실시간 계산 중인 것
        if (unrealizedPnLText != null) {
            double unrealized = renderer.CalculateTotalUnrealizedPnL();
            string unrealizedLabel = LocalizationManager.GetText("LBL_FN_UNREALIZED_PNL");
            SetColorText(unrealizedPnLText, unrealizedLabel, unrealized);
        }
    }
    // [유틸] 양수면 초록색, 음수면 빨간색 칠해주는 함수
    void SetColorText(TextMeshProUGUI uiText, string label, double value) {
        if (value > 0) {
            uiText.text = $"{label}: <color=#32D695>+${value:N2}</color>"; // 초록
        } else if (value < 0) {
            uiText.text = $"{label}: <color=#E63C3C>-${Math.Abs(value):N2}</color>"; // 빨강
        } else {
            uiText.text = $"{label}: $0.00"; // 0원
        }
    }
}