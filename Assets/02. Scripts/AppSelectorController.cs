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
    public GameObject PerpPanel; //Forunace
    public GameObject bankPanel;   // 은행 패널
    public GameObject xbirdPanel; // 엑스버드 패널
    //public GameObject GamblePanel; // 도박패널
    public GameObject RealEstatePanel; // 부동산 패널
    public GameObject OutingPanel; // 외출패널

    [Header("CoinManager")]
    public CoinManager coinManager;

  
    [Header("App Panel")]
    public GameObject appPanel;



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
    if(RealEstatePanel != null) {
            RealEstatePanel.SetActive(true);
        }
    }

    public void CloseEstateApp() {
        if (RealEstatePanel != null) {
            RealEstatePanel.SetActive(false);
        }
    }

    public void OpenFourNance() {
        if(appPanel != null) {
            appPanel.SetActive(false);
        }
        if (PerpPanel != null) {
            PerpPanel.SetActive(true);
        }
    }

    public void CloseFourNance() {
        if (PerpPanel != null) {
            PerpPanel.SetActive(false);
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

    public void OpenOpenLake() {
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowConfirm("OPENLAKE NFT 거래소 개발 중!");
        }
    }





}
