using UnityEngine;
using TMPro;
using UnityEngine.InputSystem.iOS;

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

    public void OnSortWorkFinished(int score) {
        int today = coinManager.survivalDays;

        int baseWage = Random.Range(60000, 100000);
        int bonus = score * 300;
        int totalWage = baseWage + bonus;

        playerManager.satoshiBankCash += totalWage;

        UIManager.Instance.ShowConfirm(
            $"아르바이트 완료!\n" +
            $"기본 급여: {baseWage:N0}원\n" +
            $"성과 보너스: {bonus:N0}원\n" +
            $"총 지급액: {totalWage:N0}원"
        );

        lastWorkedDay = today;

        coinManager.UpdateCashText();

        if (coinManager.assetPanelController != null) {
            coinManager.assetPanelController.UpdatePlatformAssetTexts();
        }
    }


    public void DoPartTimeJobSortQuick() {
        int today = coinManager.survivalDays; // 게임일수참조

        if (lastWorkedDay == today) {
            UIManager.Instance.ShowConfirm(
                "아르바이트는 하루에 한 번만 진행할 수 있습니다."
            );
            return;
        }

        int wage = Random.Range(60000, 100000);
        playerManager.satoshiBankCash += wage;

        UIManager.Instance.ShowConfirm(
    $"아르바이트로 {wage:N0}원을 벌었습니다."
);

        lastWorkedDay = today;

        coinManager.UpdateCashText();

        if (coinManager.assetPanelController != null) {
            coinManager.assetPanelController.UpdatePlatformAssetTexts();
        }
    }

    //void ShowFloatingLabel(string message) {
    //    var label = Instantiate(floatingLabelPrefab, labelSpawnParent);
    //    label.GetComponent<TextMeshProUGUI>().text = message;
    //    Destroy(label, 2f);
    //}
}
