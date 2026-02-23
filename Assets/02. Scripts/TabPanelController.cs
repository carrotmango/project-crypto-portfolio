using TMPro;
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
    public ResearchDetailPopup newsDetailPopup;

    [Header("외출 건물들")]
    public GameObject partimeJob;
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

    public void OnGoout() => ShowOutingPanel();

    public void ShowAssetPanel() {
        CloseSubPanelsIfOpen();
        totalAssetPanel.SetActive(true);
        if (BgmPlayer.Instance != null) BgmPlayer.Instance.PlayTradingBgm();
    }
    public void OpenMarketViaButton() {
        bool isFuturesUnlocked = OfficeManager.Instance != null && OfficeManager.Instance.IsFuturesUnlocked();

        // [추가] 현재 앱이 거래소 관련 앱(현물/선물)이 아니면, 무조건 불비트로 일단 세팅
        if (coinManager.currentApp != AppType.Bullbit && coinManager.currentApp != AppType.Perp) {
            coinManager.currentApp = AppType.Bullbit;
        } else if (isFuturesUnlocked) {
            // 이미 거래소 관련 앱이 켜져 있는 상태(activeSelf)에서 또 누를 때만 토글 스왑
            if (coinScrollView.activeSelf || perpPanel.activeSelf) {
                coinManager.currentApp = (coinManager.currentApp == AppType.Bullbit)
                                         ? AppType.Perp
                                         : AppType.Bullbit;
            }
        } else {
            // 해금 안 됐으면 무조건 불비트
            coinManager.currentApp = AppType.Bullbit;
        }

        // 텍스트와 패널 갱신
        UpdateMarketUI();
    }

    private void UpdateMarketUI() {
        // 1. 텍스트 변경
        if (marketTabLabel != null) {
            marketTabLabel.text = (coinManager.currentApp == AppType.Perp) ? "선물 거래소" : "현물 거래소";
        }

        // 2. 패널 갱신 (이미 작성하신 ShowMarketPanel 호출)
        ShowMarketPanel();
    }

    public void ShowMarketPanel(AppType? overrideApp = null) {
        CloseSubPanelsIfOpen();
        totalAssetPanel.SetActive(false);
        appPanel.SetActive(false);

        // 우선순위: 1. 인자로 넘어온 앱, 2. 현재 설정된 앱
        AppType appToShow = overrideApp ?? coinManager.currentApp;

        // [수정] 조건문 순서를 명확하게 분리합니다.
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
        // 불비트 버튼 전용 플래그 리셋 (이제 필요 없으면 삭제해도 무방)
        isBullbitButtonClicked = false;
    }

    public void ShowAppPanel() {
        CloseSubPanelsIfOpen();
        appPanel.SetActive(true);
        if (BgmPlayer.Instance != null) BgmPlayer.Instance.PlayTradingBgm();
    }

    void CloseSubPanelsIfOpen() {
        // 1. 외부 컨트롤러 정리
        if (assetPanelController != null) assetPanelController.ClosePortfolioPanels();
        if (withdrawPanelController != null && withdrawPanelController.IsOpen()) withdrawPanelController.ClosePanel();

        if (newsDetailPopup != null && newsDetailPopup.gameObject.activeSelf) {
            // 애니메이션 없이 즉시 끄는 게 전환 시에는 더 깔끔합니다.
            newsDetailPopup.gameObject.SetActive(false);
        }

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
        if (BgmPlayer.Instance != null) BgmPlayer.Instance.PlayTradingBgm();
    }


    public void ShowOutingPanel() {
        CloseSubPanelsIfOpen();
        if (outingPanel != null) outingPanel.SetActive(true);
        if (BgmPlayer.Instance != null) BgmPlayer.Instance.PlayOutingBgm();
    }
}