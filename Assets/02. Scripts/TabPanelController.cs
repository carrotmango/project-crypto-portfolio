using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // [추가] 인풋필드 포커스 체크용
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

    [Header("Detail Trading Panel")]
    public GameObject detailTradingPanel;
    public BullBitDetailTradeManager detailTradeManager;

    [Header("Controllers")]
    public WithdrawPanelController withdrawPanelController;
    public TotalAssetPanelController assetPanelController;
    public CoinManager coinManager;
    public ResearchDetailPopup newsDetailPopup;

    [Header("외출 건물들")]
    public GameObject partimeJob;
    public GameObject sortingMinigame;
    public GameObject convPanel;

    [Header("UI Text Lables")]
    public TextMeshProUGUI marketTabLabel;

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

    // ========================================================
    // ★ [핵심] 단축키 입력 처리
    // ========================================================
    void Update() {
        // 1. 유저가 인풋필드(금액 입력 등)에 타이핑 중인지 체크합니다.
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null) {
            if (EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null) {
                return;
            }
        }

        if (gamblePanel != null && gamblePanel.activeInHierarchy) return;
        if (sortingMinigame != null && sortingMinigame.activeInHierarchy) return;

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.W)) {
            if (assetPanelController != null && detailTradeManager != null) {
                if (detailTradeManager.HasLastCoin) {
                    // 마지막 코인 정보가 있다면 디테일 패널 바로 오픈
                    CloseSubPanelsIfOpen();
                    detailTradeManager.ReopenLastCoin();
                } else {
                    // 기록된 코인이 없다면 그냥 일반 Market 리스트 오픈
                    OpenMarketViaButton();
                }
            }
            return; // Ctrl + W 처리가 끝났으므로 아래 일반 W 로직은 건너뜁니다.
        }

        // 2. 단축키 매핑
        if (Input.GetKeyDown(KeyCode.Q)) {
            ToggleStatusPanel();            // 업무
        } else if (Input.GetKeyDown(KeyCode.W)) {
            OpenMarketViaButton();          // 현물 및 선물 거래소
        } else if (Input.GetKeyDown(KeyCode.E)) {
            OpenXbirdPanel();               // 피드 (Xbird)
        } else if (Input.GetKeyDown(KeyCode.A)) {
            ShowAppPanel();                 // 앱
        } else if (Input.GetKeyDown(KeyCode.S)) {
            ShowAssetPanel();               // 자산관리
        } else if (Input.GetKeyDown(KeyCode.D)) {
            ShowOutingPanel();              // 외출
        }
    }
    // ========================================================

    public void OnGoout() => ShowOutingPanel();

    public void ShowAssetPanel() {
        CloseSubPanelsIfOpen();
        totalAssetPanel.SetActive(true);
        if (BgmPlayer.Instance != null) BgmPlayer.Instance.PlayTradingBgm();
    }

    // [추가] 피드(Xbird)를 단축키로 열기 위한 전용 함수
    public void OpenXbirdPanel() {
        if (coinManager != null) {
            coinManager.currentApp = AppType.Xbird;
        }
        ShowMarketPanel();
    }

    public void OpenMarketViaButton() {
        bool isFuturesUnlocked = OfficeManager.Instance != null && OfficeManager.Instance.IsFuturesUnlocked();

        bool isWithdrawOpen = withdrawPanelController != null && withdrawPanelController.IsOpen();
        bool isAnyMarketOpen = coinScrollView.activeSelf || perpPanel.activeSelf;

        if (isAnyMarketOpen && !isWithdrawOpen) {
            if (isFuturesUnlocked) {
                coinManager.currentApp = (coinManager.currentApp == AppType.Bullbit)
                                         ? AppType.Perp
                                         : AppType.Bullbit;
            } else {
                coinManager.currentApp = AppType.Bullbit;
            }
        } else {
            string perpText = LocalizationManager.GetText("LBL_PERPETUAL_EXCHANGE");

            if (marketTabLabel != null && marketTabLabel.text == perpText && isFuturesUnlocked) {
                coinManager.currentApp = AppType.Perp;
            } else {
                coinManager.currentApp = AppType.Bullbit;
            }
        }

        if (withdrawPanelController != null) withdrawPanelController.ClosePanel();

        UpdateMarketUI();
    }

    private void UpdateMarketUI() {
        if (marketTabLabel != null) {
            marketTabLabel.text = (coinManager.currentApp == AppType.Perp) ? LocalizationManager.GetText("LBL_PERPETUAL_EXCHANGE") : LocalizationManager.GetText("LBL_SPOT_EXCHANGE");
        }
        ShowMarketPanel();
    }

    public void ShowMarketPanel(AppType? overrideApp = null) {
        CloseSubPanelsIfOpen();
        totalAssetPanel.SetActive(false);
        appPanel.SetActive(false);

        AppType appToShow = overrideApp ?? coinManager.currentApp;

        if (appToShow == AppType.Bullbit) {
            if (coinScrollView != null) coinScrollView.SetActive(true);
        } else if (appToShow == AppType.Perp) {
            if (perpPanel != null) perpPanel.SetActive(true);
        } else if (appToShow == AppType.SatoshiBank) {
            if (bankPanel != null) bankPanel.SetActive(true);
        } else if (appToShow == AppType.Xbird) {
            if (xbirdPanel != null) xbirdPanel.SetActive(true);
        } else if (appToShow == AppType.Gamble) {
            if (gamblePanel != null) gamblePanel.SetActive(true);
        }
        if (BgmPlayer.Instance != null) BgmPlayer.Instance.PlayTradingBgm();

        isBullbitButtonClicked = false;
    }

    public void ShowAppPanel() {
        CloseSubPanelsIfOpen();
        appPanel.SetActive(true);
        if (BgmPlayer.Instance != null) BgmPlayer.Instance.PlayTradingBgm();
    }

    void CloseSubPanelsIfOpen() {
        if (assetPanelController != null) assetPanelController.ClosePortfolioPanels();
        if (withdrawPanelController != null && withdrawPanelController.IsOpen()) withdrawPanelController.ClosePanel();

        if (newsDetailPopup != null && newsDetailPopup.gameObject.activeSelf) {
            newsDetailPopup.gameObject.SetActive(false);
        }

        if (statusPanel != null) statusPanel.SetActive(false);
        if (glossaryPanel != null) glossaryPanel.SetActive(false);
        if (glossaries != null) glossaries.SetActive(false);
        if (capitalDeposit != null) capitalDeposit.SetActive(false);
        if (skillUpgrade != null) skillUpgrade.SetActive(false);
        if (chartPanel != null) chartPanel.SetActive(false);
        if (detailTradingPanel != null) detailTradingPanel.SetActive(false);
        if (officeButtons != null) officeButtons.SetActive(false);

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
        if (perpPanel != null) perpPanel.SetActive(false);
    }

    public void ToggleStatusPanel() {
        CloseSubPanelsIfOpen();

        if (officePanel != null) {
            officePanel.SetActive(true);

            if (officeButtons != null)
                officeButtons.SetActive(true);

            if (officeController != null) {
                officeController.EnterOffice();
            }
        }
        if (BgmPlayer.Instance != null) BgmPlayer.Instance.PlayTradingBgm();
    }

    public void ShowOutingPanel() {
        CloseSubPanelsIfOpen();
        if (outingPanel != null) outingPanel.SetActive(true);
        if (BgmPlayer.Instance != null) BgmPlayer.Instance.PlayOutingBgm();
    }
}