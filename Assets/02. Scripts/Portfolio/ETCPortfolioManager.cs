using UnityEngine;
using TMPro;
using System.Collections; // [필수]

public class ETCPortfolioManager : MonoBehaviour {

    [Header("1. 급여 정보 (OfficeManager 연동)")]
    public TextMeshProUGUI currentSalaryText;
    public TextMeshProUGUI totalSalaryText;

    [Header("2. 아르바이트 통계 (PlayerManager 연동)")]
    public TextMeshProUGUI albaCountText;
    public TextMeshProUGUI albaTotalIncomeText;

    [Header("3. 복권 통계 (PlayerManager 연동)")]
    public TextMeshProUGUI lottoCountText;
    public TextMeshProUGUI lottoSpentText;
    public TextMeshProUGUI lottoWonText;

    private void OnEnable() {
        StartCoroutine(UpdateRoutine());
    }


    private void OnDisable() {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnStatsChanged -= UpdatePortfolioUI;

        if (OfficeManager.Instance != null)
            OfficeManager.Instance.OnSalaryChanged -= UpdatePortfolioUI;
    }



    IEnumerator UpdateRoutine() {
        // [수정] WaitUntil 대신 while문과 WaitForSecondsRealtime 사용
        // 게임 시간이 멈춰있어도(Time.timeScale = 0), 실제 시간으로 0.1초마다 체크합니다.

        // 1. PlayerManager가 생길 때까지 대기
        while (PlayerManager.Instance == null) {
            yield return new WaitForSecondsRealtime(0.1f);
        }

        // 2. OfficeManager가 생길 때까지 대기
        while (OfficeManager.Instance == null) {
            yield return new WaitForSecondsRealtime(0.1f);
        }

        PlayerManager.Instance.OnStatsChanged += UpdatePortfolioUI;

        OfficeManager.Instance.OnSalaryChanged += UpdatePortfolioUI;

        // 3. 둘 다 준비됐으니 갱신 시작!
        UpdatePortfolioUI();
    }

    public void UpdatePortfolioUI() {
        var pm = PlayerManager.Instance;
        var office = OfficeManager.Instance;

        // ---------------------------------------------------------
        // 1. 현재 급여
        // ---------------------------------------------------------
        if (office != null) {
            currentSalaryText.text = $"현재 급여: {office.currentMonthlySalary:N0}원";
        } else {
            currentSalaryText.text = "현재 급여: -";
        }

        // ---------------------------------------------------------
        // 2. 플레이어 통계
        // ---------------------------------------------------------
        if (pm != null) {
            totalSalaryText.text = $"수령한 급여: {pm.totalSalaryReceived:N0}원";

            albaCountText.text = $"알바 횟수: {pm.totalPartTimeJobCount:N0}회";
            albaTotalIncomeText.text = $"수령한 알바 급여: {pm.totalPartTimeJobIncome:N0}원";

            lottoCountText.text = $"구매한 복권 수: {pm.totalLotteryTicketCount:N0}장";
            lottoSpentText.text = $"누적 복권 구매 금액: {pm.totalLotterySpentAmount:N0}원";
            lottoWonText.text = $"누적 복권 당첨 금액: {pm.totalLotteryWonAmount:N0}원";
        }
    }
}