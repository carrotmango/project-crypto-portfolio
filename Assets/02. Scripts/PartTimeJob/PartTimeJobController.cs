using UnityEngine;
using TMPro;

public class PartTimeJobController : MonoBehaviour {
    public GameObject floatingLabelPrefab;
    public Transform labelSpawnParent;
    public PlayerManager playerManager;
    public CoinManager coinManager;

    public static PartTimeJobController Instance;


    public int lastWorkedDay = -1;


    private void Awake() {
        Instance = this;
    }

    public void DoPartTimeJob() {
        int today = coinManager.survivalDays; // 게임일수참조

        if (lastWorkedDay == today) {
            ShowFloatingLabel($"아르바이트는 하루에 한번 진행할 수 있습니다");
            return;
        }

        int wage = Random.Range(45000, 80000);
        playerManager.satoshiBankCash += wage;

        ShowFloatingLabel($"당신은 막노동으로 {wage:N0}원을 벌었습니다.\n급여는 사토시 은행으로 입금되었습니다");

        lastWorkedDay = today;

        coinManager.UpdateCashText();

        if (coinManager.assetPanelController != null) {
            coinManager.assetPanelController.UpdatePlatformAssetTexts();
        }
    }

    void ShowFloatingLabel(string message) {
        var label = Instantiate(floatingLabelPrefab, labelSpawnParent);
        label.GetComponent<TextMeshProUGUI>().text = message;
        Destroy(label, 2f);
    }
}
