using UnityEngine;
using TMPro;

public class PartTimeJobController : MonoBehaviour {
    public GameObject floatingLabelPrefab;
    public Transform labelSpawnParent;
    public PlayerManager playerManager;
    public CoinManager coinManager;
    public static PartTimeJobController Instance;
    public int lastWorkedDay = -1;
    public SortingPanelController boxGameController;

    private void Awake() {
        Instance = this;
    }

    // ==========================================
    // 1. 분류 게임 알바 (미니게임)
    // ==========================================
    public void DoPartTimeJobSort() {
        int today = coinManager.survivalDays;

        if (lastWorkedDay == today) {
            // [수정] 팝업 메시지 현지화
            UIManager.Instance.ShowConfirm(LocalizationManager.GetText("MSG_ALBA_ONCE_A_DAY"));
            return;
        }

        SortGameManager.Instance.StartGame();
    }

    // 게임 끝났을 때 호출됨
    public void OnSortWorkFinished(int score) {
        int today = coinManager.survivalDays;

        // 1. 기본 수익 계산
        long baseWage = Random.Range(60000, 100000);
        long scoreBonus = score * 300;
        long rawIncome = baseWage + scoreBonus; // 순수 노동 소득

        // 2. 직급 보너스 계산 (핵심)
        float multiplier = 1.0f;
        if (OfficeManager.Instance != null) {
            multiplier = OfficeManager.Instance.GetPartTimeJobMultiplier();
        }

        // 총액 = (기본급 + 성과급) * 배율
        long totalWage = (long)(rawIncome * multiplier);

        // 직급 보너스 금액 = 총액 - 순수 노동 소득
        long rankBonusAmount = totalWage - rawIncome;

        // 3. 지급 및 저장
        playerManager.satoshiBankCash += totalWage;
        PlayerManager.Instance.AddPartTimeJob(totalWage);

        // 4. 영수증 출력 (현지화 적용)
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");
        string message = string.Format(LocalizationManager.GetText("MSG_ALBA_COMPLETE"), baseWage.ToString("N0"), unit, scoreBonus.ToString("N0"));

        // 직급 보너스가 있을 때만 표시 (인턴은 0원이니까 안 뜸)
        if (rankBonusAmount > 0) {
            message += string.Format(LocalizationManager.GetText("MSG_ALBA_RANK_BONUS"), rankBonusAmount.ToString("N0"), unit);
        }

        message += string.Format(LocalizationManager.GetText("MSG_ALBA_TOTAL_WAGE"), totalWage.ToString("N0"), unit);

        // [수정] 거래 내역 로그 현지화
        TransactionManager.Instance.AddRecord("아르바이트", totalWage, "입금", "사토시 현금");

        UIManager.Instance.ShowConfirm(message);

        // 5. 마무으리
        lastWorkedDay = today;
        coinManager.UpdateCashText();

        if (coinManager.assetPanelController != null) {
            coinManager.assetPanelController.UpdatePlatformAssetTexts();
        }
    }

    // ==========================================
    // 2. 딸깍 알바 (퀵 모드)
    // ==========================================
    public void DoPartTimeJobSortQuick() {
        int today = coinManager.survivalDays;

        if (lastWorkedDay == today) {
            // [수정] 팝업 메시지 현지화
            UIManager.Instance.ShowConfirm(LocalizationManager.GetText("MSG_ALBA_ONCE_A_DAY"));
            return;
        }

        // 1. 기본 수익
        long baseWage = Random.Range(60000, 100000); // 퀵은 성과급 없음
        long rawIncome = baseWage;

        // 2. 직급 보너스 계산
        float multiplier = 1.0f;
        if (OfficeManager.Instance != null) {
            multiplier = OfficeManager.Instance.GetPartTimeJobMultiplier();
        }

        long totalWage = (long)(rawIncome * multiplier);
        long rankBonusAmount = totalWage - rawIncome;

        // 3. 지급
        playerManager.satoshiBankCash += totalWage;
        PlayerManager.Instance.AddPartTimeJob(totalWage);

        // 4. 메시지 (현지화 적용)
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");
        string message = string.Format(LocalizationManager.GetText("MSG_ALBA_QUICK_COMPLETE"), baseWage.ToString("N0"), unit);

        if (rankBonusAmount > 0) {
            message += string.Format(LocalizationManager.GetText("MSG_ALBA_RANK_BONUS"), rankBonusAmount.ToString("N0"), unit);
        }

        message += string.Format(LocalizationManager.GetText("LBL_ALBA_QUICK_TOTAL"), totalWage.ToString("N0"), unit);

        // [수정] 거래 내역 로그 현지화
        TransactionManager.Instance.AddRecord("아르바이트", totalWage, "입금", "사토시 현금");

        UIManager.Instance.ShowConfirm(message);

        // 5. 마무리
        lastWorkedDay = today;
        coinManager.UpdateCashText();

        if (coinManager.assetPanelController != null) {
            coinManager.assetPanelController.UpdatePlatformAssetTexts();
        }
    }
}