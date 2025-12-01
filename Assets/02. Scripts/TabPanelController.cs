using UnityEngine;
using UnityEngine.UI;
using static CoinManager;

public class TabPanelController : MonoBehaviour {
    public GameObject coinScrollView;        // 불비트 코인 리스트
    public GameObject bankPanel;             // 사토시 은행 패널
    public GameObject totalAssetPanel;
    public GameObject statusPanel;
    public GameObject xbirdPanel; // X버드 전용 피드 패널
    public GameObject gamblePanel;

    public Button bullbitButton;
    private bool isBullbitButtonClicked = false;


    public GameObject appPanel;
    public WithdrawPanelController withdrawPanelController;

    public TotalAssetPanelController assetPanelController;
    public CoinManager coinManager; //  현재 앱 상태 확인용

    void Start() {
        if (bullbitButton != null) {
            bullbitButton.onClick.AddListener(() => {
                isBullbitButtonClicked = true;
                ShowMarketPanel(); // 버튼 누르면 바로 실행되게
            });
        }

        ShowMarketPanel();
    }

    public void OnGoout() {
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowConfirm("외출 기능 개발 중!");
        }
    }


    public void ShowAssetPanel() {
        CloseSubPanelsIfOpen(); 
        totalAssetPanel.SetActive(true);
        coinScrollView.SetActive(false);
        bankPanel.SetActive(false);
        appPanel.SetActive(false);
    }

    public void ShowMarketPanel(AppType? overrideApp = null) {
        
        if (coinManager.currentApp == AppType.Xbird && xbirdPanel != null)
            xbirdPanel.SetActive(false);

        if (coinManager.currentApp == AppType.Gamble)
            gamblePanel.SetActive(false);

        CloseSubPanelsIfOpen();
        totalAssetPanel.SetActive(false);
        appPanel.SetActive(false);

        AppType appToShow = overrideApp ?? coinManager.currentApp;

        if (appToShow == AppType.Bullbit || isBullbitButtonClicked) {
            coinScrollView.SetActive(true);
            bankPanel.SetActive(false);
            xbirdPanel.SetActive(false);
            gamblePanel.SetActive(false);
            isBullbitButtonClicked = false;
        } else if (appToShow == AppType.SatoshiBank) {
            coinScrollView.SetActive(false);
            bankPanel.SetActive(true);
            xbirdPanel.SetActive(false);
            gamblePanel.SetActive(false);
        } else if (appToShow == AppType.Xbird) {
            coinScrollView.SetActive(false);
            bankPanel.SetActive(false);
            xbirdPanel.SetActive(true);
            gamblePanel.SetActive(false);
        } else if (appToShow == AppType.Gamble) {
            coinScrollView.SetActive(false);
            bankPanel.SetActive(false);
            xbirdPanel.SetActive(false);
            gamblePanel.SetActive(true);
        }

        Debug.Log($"[ShowMarketPanel] current: {coinManager.currentApp}, target: {appToShow}");
    }




    public void ShowAppPanel() {
        CloseSubPanelsIfOpen();
        totalAssetPanel.SetActive(false);
        coinScrollView.SetActive(false);
        bankPanel.SetActive(false);
        appPanel.SetActive(true);
    }

    ///  하위 패널들 (포트폴리오, 출금창 등)을 닫는 통합 함수
    void CloseSubPanelsIfOpen() {
        if (assetPanelController != null) {
            assetPanelController.ClosePortfolioPanels();
        }

        if (withdrawPanelController != null && withdrawPanelController.IsOpen()) {
            withdrawPanelController.ClosePanel();
        }
        if (statusPanel != null) {
            statusPanel.SetActive(false); // 
        }
        if(xbirdPanel != null) {
            xbirdPanel.SetActive(false);
        }
        if(gamblePanel != null) {
            gamblePanel.SetActive(false);
        }

        if(coinScrollView != null) {
            coinScrollView.SetActive(false);
        }
        
    }
    public void ToggleStatusPanel() {
        CloseSubPanelsIfOpen(); 
        statusPanel.SetActive(true);
    }


}
