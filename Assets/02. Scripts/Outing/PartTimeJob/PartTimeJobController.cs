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
            UIManager.Instance.ShowConfirm(
                "아르바이트는 하루에 한 번만 진행할 수 있습니다."
            );
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

        // 4. 영수증 출력
        string message = $"아르바이트 완료!\n" +
                         $"기본 급여: {baseWage:N0}원\n" +
                         $"성과 보너스: {scoreBonus:N0}원\n";

        // 직급 보너스가 있을 때만 표시 (인턴은 0원이니까 안 뜸)
        if (rankBonusAmount > 0) {
            message += $"<color=#00FF00>직급 보너스: +{rankBonusAmount:N0}원</color>\n";
        }

        message += $"--------------------\n" +
                   $"총 지급액: {totalWage:N0}원";

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
            UIManager.Instance.ShowConfirm(
                "아르바이트는 하루에 한 번만 진행할 수 있습니다."
            );
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

        // 4. 메시지
        string message = $"간편 아르바이트 완료!\n" +
                         $"기본 급여: {baseWage:N0}원\n";

        if (rankBonusAmount > 0) {
            message += $"<color=#00FF00>직급 보너스: +{rankBonusAmount:N0}원</color>\n";
        }

        message += $"총 지급액: {totalWage:N0}원";

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