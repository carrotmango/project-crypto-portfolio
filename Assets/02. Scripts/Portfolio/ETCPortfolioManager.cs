using UnityEngine;
using TMPro;
using System.Collections;
using System; // [필수]

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
    [Header("4. 오락실 통계 (PlayerManager 연동)")] 
    public TextMeshProUGUI gambleSpentText; // 총 베팅금
    public TextMeshProUGUI gambleEarnedText; // 총 당첨금
    public TextMeshProUGUI gambleNetProfitText; // 순손익 (+/-)

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

        // [수정] 공통 단위 현지화 로드
        string unitCurrency = LocalizationManager.GetText("UNIT_CURRENCY");
        string unitTimes = LocalizationManager.GetText("UNIT_TIMES");
        string unitTickets = LocalizationManager.GetText("UNIT_TICKETS");

        // ---------------------------------------------------------
        // 1. 현재 급여
        // ---------------------------------------------------------
        if (office != null) {
            currentSalaryText.text = $"{LocalizationManager.GetText("LBL_CURRENT_SALARY")} {office.currentMonthlySalary:N0}{unitCurrency}";
        } else {
            currentSalaryText.text = $"{LocalizationManager.GetText("LBL_CURRENT_SALARY")} -";
        }

        // ---------------------------------------------------------
        // 2. 플레이어 통계
        // ---------------------------------------------------------
        if (pm != null) {
            totalSalaryText.text = $"{LocalizationManager.GetText("LBL_TOTAL_SALARY_RECEIVED")} {pm.totalSalaryReceived:N0}{unitCurrency}";

            albaCountText.text = $"{LocalizationManager.GetText("LBL_PART_TIME_COUNT")} {pm.totalPartTimeJobCount:N0}{unitTimes}";
            albaTotalIncomeText.text = $"{LocalizationManager.GetText("LBL_PART_TIME_EARNINGS")} {pm.totalPartTimeJobIncome:N0}{unitCurrency}";

            lottoCountText.text = $"{LocalizationManager.GetText("LBL_LOTTO_COUNT")} {pm.totalLotteryTicketCount:N0}{unitTickets}";
            lottoSpentText.text = $"{LocalizationManager.GetText("LBL_LOTTO_TOTAL_SPENT")} {pm.totalLotterySpentAmount:N0}{unitCurrency}";
            lottoWonText.text = $"{LocalizationManager.GetText("LBL_LOTTO_TOTAL_WON")} {pm.totalLotteryWonAmount:N0}{unitCurrency}";

            // 오락실 통계
            if (gambleSpentText != null) {
                gambleSpentText.text = $"{LocalizationManager.GetText("LBL_ARCADE_SPENT")} {pm.totalGambleSpent:N0}{unitCurrency}";
            }

            if (gambleEarnedText != null) {
                gambleEarnedText.text = $"{LocalizationManager.GetText("LBL_ARCADE_WON")} {pm.totalGambleEarned:N0}{unitCurrency}";
            }

            if (gambleNetProfitText != null) {
                long netProfit = pm.totalGambleEarned - pm.totalGambleSpent;
                string profitLabel = LocalizationManager.GetText("LBL_ARCADE_PROFIT");

                if (netProfit > 0) {
                    // 이득: 초록색 (▲)
                    gambleNetProfitText.text = $"{profitLabel} <color=#32D695>▲{netProfit:N0}{unitCurrency}</color>";
                } else if (netProfit < 0) {
                    // 손해: 빨간색 (▼)
                    gambleNetProfitText.text = $"{profitLabel} <color=#E63C3C>▼{Math.Abs(netProfit):N0}{unitCurrency}</color>";
                } else {
                    // 본전
                    gambleNetProfitText.text = $"{profitLabel} -";
                }
            }
        }
    }
}