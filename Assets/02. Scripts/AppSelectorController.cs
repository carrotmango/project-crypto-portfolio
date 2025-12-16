using UnityEngine;
using TMPro;
using static CoinManager;

public class AppSelectorController : MonoBehaviour {

    [Header("탭 컨트롤러 연결")]
    public TabPanelController tabPanelController;

    [Header("UI 연결")]
    public TextMeshProUGUI marketTabLabel;

    [Header("패널들")]
    public GameObject marketPanel; // 코인 리스트 패널
    public GameObject bankPanel;   // 은행 패널

    [Header("CoinManager 연결")]
    public CoinManager coinManager;

    [Header("Xbird 연결")]
    public GameObject xbirdPanel;

    [Header("App Panel 가져오기")]
    public GameObject appPanel;


    [Header("App Panel 가져오기")]
    public GameObject GamblePanel;


    public void OpenBullbitApp() {
        if (marketTabLabel != null)
            marketTabLabel.text = "불비트";

        if (coinManager != null) {
            coinManager.currentApp = AppType.Bullbit;
            coinManager.UpdateCashText();
        }

        if (tabPanelController != null)
            tabPanelController.ShowMarketPanel();
    }

    public void OpenBankApp() {
        if (coinManager != null) {
            coinManager.currentApp = AppType.SatoshiBank;
            coinManager.UpdateCashText();
  
        }

        if (tabPanelController != null)
            tabPanelController.ShowMarketPanel();
    }

    public void OpenXbirdApp() {

        if (coinManager != null) {
            coinManager.currentApp = AppType.Xbird;
            coinManager.UpdateCashText();

        }

        if (tabPanelController != null)
            tabPanelController.ShowMarketPanel(); // 
    }

    public void CloseXbirdApp() {
        if (xbirdPanel != null)
            xbirdPanel.SetActive(false);
            appPanel.SetActive(true);
        if (coinManager != null)
            coinManager.currentApp = AppType.Bullbit; // enum에 None 추가해도 됨
    }

    public void OpenEstateApp() {
        if(UIManager.Instance != null) {
            UIManager.Instance.ShowConfirm("부동산 컨텐츠는 개발 중!");
        }
    }

    public void OpenFourNance() {
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowConfirm("포낸스 선물 거래소 개발 중!");
        }
    }

    public void OpenGhostWallet() {
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowConfirm("지갑 컨텐츠 개발 중!");
        }
    }

    public void OpenMangoSwap() {
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowConfirm("망고 스왑 DEX 거래소 개발 중!");
        }
    }

    public void OpenGambleApp() {
        if (coinManager != null) {
            coinManager.currentApp = AppType.Gamble;
            coinManager.UpdateCashText();
        }

        if (tabPanelController != null)
            tabPanelController.ShowMarketPanel();

        if (GamblePanel != null) {
            GamblePanel.SetActive(true); // 패널 열기

            // 🟢 GambleManager 컴포넌트 받아와서 베팅 버튼 초기화
            GambleManager gm = GamblePanel.GetComponent<GambleManager>();
            if (gm != null) {
                gm.UpdateBetButtonStates();
            }
        }
    }

    public void CloseGambleApp() {
        if (GamblePanel != null)
            GamblePanel.SetActive(false);
        appPanel.SetActive(true);
        if (coinManager != null)
            GamblePanel.SetActive(false);
        coinManager.currentApp = AppType.Bullbit; 
    }

}
