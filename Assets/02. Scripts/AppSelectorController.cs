using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static CoinManager;

public class AppSelectorController : MonoBehaviour {

    [Header("탭 컨트롤러 연결")]
    public TabPanelController tabPanelController;

    [Header("UI 연결")]
    public TextMeshProUGUI marketTabLabel;

    [Header("패널들")]
    public GameObject marketPanel;    // 코인 리스트 패널
    public GameObject PerpPanel;      // Forunace (선물)
    public GameObject bankPanel;      // 은행 패널
    public GameObject xbirdPanel;     // 엑스버드 패널
    //public GameObject GamblePanel;  // 도박패널
    public GameObject RealEstatePanel;// 부동산 패널
    public GameObject OutingPanel;    // 외출패널

    [Header("CoinManager")]
    public CoinManager coinManager;

    [Header("App Panel")]
    public GameObject appPanel;

    // ========================================================
    // [NEW] 앱 아이콘 잠금 비주얼 처리를 위한 변수들
    // ========================================================
    [Header("App Icons & Texts (Lock Visuals)")]
    // 부동산 (App_RealEstate / App_RealEstate_Text)
    public Image realEstateIcon;
    public TextMeshProUGUI realEstateText;

    // 선물거래 (App_FourNance / App_FourNance_Text)
    public Image fourNanceIcon;
    public TextMeshProUGUI fourNanceText;

    // 잠금 상태 색상 정의
    private Color32 lockedIconColor = new Color32(50, 50, 50, 255);     // 어두운 회색
    private Color32 lockedTextColor = new Color32(180, 180, 180, 255);  // 흐린 회색
    private Color32 unlockedColor = new Color32(255, 255, 255, 255);    // 흰색 (기본)


    private void Start() {
        // 게임 시작하면 승진 이벤트에 구독 신청
        if (OfficeManager.Instance != null) {
            OfficeManager.Instance.OnPromotion += RefreshAppStatus;
        }
    }

    private void OnDestroy() {
        // 게임 꺼지거나 이 객체 사라질 때 구독 해제 (메모리 누수 방지)
        if (OfficeManager.Instance != null) {
            OfficeManager.Instance.OnPromotion -= RefreshAppStatus;
        }
    }

    private void OnEnable() {
        // 켜질 때도 갱신 (기존 유지)
        RefreshAppStatus();
    }

    // 직급에 따라 아이콘 색상을 변경하는 함수
    public void RefreshAppStatus() {
        if (OfficeManager.Instance == null) return;

        // 1. 부동산 체크 (자본주의 대리 이상)
        bool isEstateUnlocked = OfficeManager.Instance.IsRealEstateUnlocked();
        UpdateAppVisuals(isEstateUnlocked, realEstateIcon, realEstateText);

        // 2. 선물거래(FourNance) 체크 (납입왕 차장 이상)
        bool isFuturesUnlocked = OfficeManager.Instance.IsFuturesUnlocked();
        UpdateAppVisuals(isFuturesUnlocked, fourNanceIcon, fourNanceText);
    }

    // 색상 변경 헬퍼 함수
    private void UpdateAppVisuals(bool isUnlocked, Image icon, TextMeshProUGUI text) {
        if (icon == null || text == null) return;

        if (isUnlocked) {
            // 해금됨: 원래 색상 (흰색)
            icon.color = unlockedColor;
            text.color = unlockedColor;
        } else {
            // 잠김: 어두운 색상
            icon.color = lockedIconColor;
            text.color = lockedTextColor;
        }
    }

    // ========================================================
    // 앱 실행 함수들 (클릭 이벤트)
    // ========================================================

    public void OpenBullbitApp() {
        if (marketTabLabel != null) marketTabLabel.text = "불비트";
        if (coinManager != null) {
            coinManager.currentApp = AppType.Bullbit;
            coinManager.UpdateCashText();
        }
        if (tabPanelController != null) tabPanelController.ShowMarketPanel();
    }

    public void OpenBankApp() {
        if (coinManager != null) {
            coinManager.currentApp = AppType.SatoshiBank;
            coinManager.UpdateCashText();
        }
        if (tabPanelController != null) tabPanelController.ShowMarketPanel();
    }

    public void OpenXbirdApp() {
        if (coinManager != null) {
            coinManager.currentApp = AppType.Xbird;
            coinManager.UpdateCashText();
        }
        if (tabPanelController != null) tabPanelController.ShowMarketPanel();
    }

    public void CloseXbirdApp() {
        if (xbirdPanel != null) xbirdPanel.SetActive(false);
        if (appPanel != null) appPanel.SetActive(true);
        if (coinManager != null) coinManager.currentApp = AppType.Bullbit;
    }

    // [수정됨] 부동산 앱 열기 (잠금 체크 추가)
    public void OpenEstateApp() {
        // 1. 해금 여부 체크
        if (OfficeManager.Instance != null && !OfficeManager.Instance.IsRealEstateUnlocked()) {
            UIManager.Instance.ShowConfirm("자본주의 대리 직급 이상\n이용 가능합니다.");
            return; // 열지 않고 리턴
        }

        // 2. 해금되었으면 오픈
        if (RealEstatePanel != null) {
            RealEstatePanel.SetActive(true);
        }
    }

    public void CloseEstateApp() {
        if (RealEstatePanel != null) {
            RealEstatePanel.SetActive(false);
        }
    }

    public void CloseSatoshiBank() {
        if(bankPanel != null) {
            bankPanel.SetActive(false);
            if (marketPanel != null && marketPanel.activeSelf) {
                return;
            } else {
                if(appPanel != null) {
                    appPanel.SetActive(true);
                }
            }
        }
    }

    // [수정됨] 포낸스(선물) 앱 열기 (잠금 체크 추가)
    public void OpenFourNance() {
        // 1. 해금 여부 체크
        if (OfficeManager.Instance != null && !OfficeManager.Instance.IsFuturesUnlocked()) {
            UIManager.Instance.ShowConfirm("납입왕 차장 직급 이상\n이용 가능합니다.");
            return; // 열지 않고 리턴
        }

        // 2. 해금되었으면 오픈
        if (appPanel != null) {
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