using UnityEngine;
using TMPro;
using static CoinManager;

public class OutingManager : MonoBehaviour {

    [Header("연결된 패널들")]
    public GameObject GamblePanel;
    public TabPanelController tabPanelController;

    [Header("CoinManager 연결")]
    public CoinManager coinManager;

    [Header("툴팁 UI")]
    public TextMeshProUGUI tooltipText;  //  미리 UI에 배치해둘 TextMeshPro
    public GameObject tooltipObject;     //  TextMeshPro 부모 오브젝트(패널)

    [Header("건물 오브젝트")]
    public GameObject partimeJob;
    public GameObject partimePanel;
    public GameObject office;
    public GameObject conv;

    // 외출 오락실 진입
    public void EnterArcade() {
        if (coinManager != null) {
            coinManager.currentApp = AppType.Gamble;
            coinManager.UpdateCashText();
        }

        if (tabPanelController != null)
            tabPanelController.ShowMarketPanel();

        if (GamblePanel != null) {
            GamblePanel.SetActive(true);

            GambleManager_renewal gm = GamblePanel.GetComponent<GambleManager_renewal>();
            if (gm != null)               
            gm.OpenPanel();
        }
    }

    public void EnterPartimeJob() {
        if (partimeJob != null) {
            partimeJob.SetActive(true);
        }
    }

    public void ExitPartimeJob() {
        if (partimeJob != null) {
            partimeJob.SetActive(false);
        }
    }

    public void EnterOffice() {
        if (office != null) {
            office.SetActive(true);
        }
    }

    public void ExitOffice() {
        if (office != null) {
            office.SetActive(false);
        }
    }

    public void EnterConv() {
        if (conv != null) {
            conv.SetActive(true);
        }
    }

    public void ExitConv() {
        if (conv != null) {
            conv.SetActive(false);
        }
    }


    // 툴팁 표시
    public void ShowTooltip(string text) {
        if (tooltipText != null)
            tooltipText.text = text;

        if (tooltipObject != null)
            tooltipObject.SetActive(true);
    }

    // 툴팁 숨기기
    public void HideTooltip() {
        if (tooltipObject != null)
            tooltipObject.SetActive(false);
    }
}
