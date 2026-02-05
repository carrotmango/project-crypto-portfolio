using UnityEngine;
using UnityEngine.UI;
using static CoinManager;

public class TabPanelController : MonoBehaviour {
    [Header("Main Panels")]
    public GameObject coinScrollView;
    public GameObject bankPanel;
    public GameObject totalAssetPanel;
    public GameObject officePanel;
    public GameObject xbirdPanel;
    public GameObject gamblePanel;
    public GameObject outingPanel;
    public GameObject appPanel;
    public GameObject chartPanel;
    public GameObject realEstatePanel;
    public GameObject perpPanel;

    [Header("Office Sub Panels (Direct Management)")]
    public GameObject officeButtons;    // Hierarchy의 'Buttons'
    public GameObject statusPanel;      // Hierarchy의 'UserStatusPanel'
    public GameObject glossaryPanel;    // Hierarchy의 'GlossaryPanel'
    public GameObject glossaries;       // Hierarchy의 'Glossaries' (에어드랍 상세 등 부모)
    public GameObject capitalDeposit;
    public GameObject skillUpgrade;
    public OfficePanelController officeController;


    [Header("Controllers")]
    public WithdrawPanelController withdrawPanelController;
    public TotalAssetPanelController assetPanelController;
    public CoinManager coinManager;

    [Header("외출 건물들")]
    public GameObject partimeJob;
    public GameObject convPanel;

    public Button bullbitButton;
    private bool isBullbitButtonClicked = false;

    void Start() {
        if (bullbitButton != null) {
            bullbitButton.onClick.AddListener(() => {
                isBullbitButtonClicked = true;
                ShowMarketPanel();
            });
        }
        ShowMarketPanel();
    }

    public void OnGoout() => ShowOutingPanel();

    public void ShowAssetPanel() {
        CloseSubPanelsIfOpen();
        totalAssetPanel.SetActive(true);
    }

    public void ShowMarketPanel(AppType? overrideApp = null) {
        CloseSubPanelsIfOpen();
        totalAssetPanel.SetActive(false);
        appPanel.SetActive(false);

        AppType appToShow = overrideApp ?? coinManager.currentApp;

        if (appToShow == AppType.Bullbit || isBullbitButtonClicked) {
            coinScrollView.SetActive(true);
            isBullbitButtonClicked = false;
        } else if (appToShow == AppType.SatoshiBank) {
            bankPanel.SetActive(true);
        } else if (appToShow == AppType.Xbird) {
            xbirdPanel.SetActive(true);
        } else if (appToShow == AppType.Gamble) {
            gamblePanel.SetActive(true);
        }
    }

    public void ShowAppPanel() {
        CloseSubPanelsIfOpen();
        appPanel.SetActive(true);
    }

    void CloseSubPanelsIfOpen() {
        // 1. 외부 컨트롤러 정리
        if (assetPanelController != null) assetPanelController.ClosePortfolioPanels();
        if (withdrawPanelController != null && withdrawPanelController.IsOpen()) withdrawPanelController.ClosePanel();

        // 2. 오피스 하위 패널들 직접 리셋
        if (statusPanel != null) statusPanel.SetActive(false);
        if (glossaryPanel != null) glossaryPanel.SetActive(false);
        if (glossaries != null) glossaries.SetActive(false);
        if (capitalDeposit != null) capitalDeposit.SetActive(false);
        if (skillUpgrade != null) skillUpgrade.SetActive(false);

        // 차트
        if (chartPanel != null) chartPanel.SetActive(false);

        // 메인 버튼은 일단 꺼둡니다
        if (officeButtons != null) officeButtons.SetActive(false);

        // 3. 모든 메인 탭 패널들 끄기
        if (officePanel != null) officePanel.SetActive(false);
        if (xbirdPanel != null) xbirdPanel.SetActive(false);
        if (gamblePanel != null) gamblePanel.SetActive(false);
        if (coinScrollView != null) coinScrollView.SetActive(false);
        if (bankPanel != null) bankPanel.SetActive(false);
        if (totalAssetPanel != null) totalAssetPanel.SetActive(false);
        if (appPanel != null) appPanel.SetActive(false);
        if (outingPanel != null) outingPanel.SetActive(false);
        if (partimeJob != null) partimeJob.SetActive(false);
        if (realEstatePanel != null) realEstatePanel.SetActive(false);
        if (convPanel != null) convPanel.SetActive(false);
        if (perpPanel !=null) perpPanel.SetActive(false);
    }


    public void ToggleStatusPanel() {
        CloseSubPanelsIfOpen();

        if (officePanel != null) {
            officePanel.SetActive(true);

            if (officeButtons != null)
                officeButtons.SetActive(true);

            // GetComponent를 쓰지 않고, 직접 연결된 변수를 사용합니다.
            if (officeController != null) {
                // 이제 무조건 실행됩니다.
                officeController.EnterOffice();
            } else {
            }
        }
    }


    public void ShowOutingPanel() {
        CloseSubPanelsIfOpen();
        if (outingPanel != null) outingPanel.SetActive(true);
    }
}