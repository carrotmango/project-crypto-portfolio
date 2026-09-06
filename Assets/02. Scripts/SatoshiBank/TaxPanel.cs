using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaxPanel : MonoBehaviour {
    public static TaxPanel Instance;

    [Header("Main Panel UI")]
    [SerializeField] private TextMeshProUGUI taxAmountText;    // 메인 텍스트 ("세금 : 1억" or "없습니다")
    [SerializeField] private TextMeshProUGUI taxRateText;      // "소득세: 10%"
    [SerializeField] private TextMeshProUGUI dueDateText;      // "납부기한 00/00/00 까지" (날짜만)
    [SerializeField] private TextMeshProUGUI penaltyLabel;     // ★ [추가됨] "기한 미납 시 강제징수..." 빨간 라벨
    [SerializeField] private Button openPaymentPopupButton;    // [납부] 버튼

    [Header("Confirmation Popup UI")]
    [SerializeField] private GameObject confirmPopupPanel;     // 팝업 패널
    [SerializeField] private TextMeshProUGUI confirmMsgText;   // 팝업 내 메시지
    [SerializeField] private TextMeshProUGUI warningPopupText; // "잔액 부족" 경고

    private void Awake() {
        if (Instance == null) Instance = this;
    }

    private void Start() {
        // 시작 시 UI 초기화
        if (confirmPopupPanel != null) confirmPopupPanel.SetActive(false);
        if (warningPopupText != null) warningPopupText.canvasRenderer.SetAlpha(0f);

        RefreshTaxUI();
    }

    private void OnEnable() {
        RefreshTaxUI();
    }

    // =========================================================
    // UI 갱신 로직 (수정됨)
    // =========================================================
    public void RefreshTaxUI() {
        if (TaxManager.Instance == null) return;

        double currentTax = TaxManager.Instance.unpaidTaxAmount;
        double currentRate = GlobalEconomyManager.TaxRate;
        bool hasTax = currentTax > 0;
        bool isBillIssued = TaxManager.Instance.isTaxBillIssued;

        string unit = LocalizationManager.GetText("UNIT_CURRENCY"); // 공통 화폐 단위

        // ★ 세금이 있고 + 고지서가 발행된 상태여야 버튼과 경고문을 보여줌
        bool showActiveUI = hasTax && isBillIssued;

        // 1. 메인 텍스트(taxAmountText) 처리 로직
        if (taxAmountText != null) {
            taxAmountText.gameObject.SetActive(true);

            if (showActiveUI) {
                // [세금이 있을 때]
                taxAmountText.text = string.Format(LocalizationManager.GetText("LBL_TAX_AMOUNT"), currentTax.ToString("N0"), unit);
                taxAmountText.color = Color.white;
            } else {
                // [세금이 없을 때]
                taxAmountText.text = LocalizationManager.GetText("LBL_TAX_NO_DUE");
                taxAmountText.color = Color.gray;
            }
        }

        // 2. 세율 표시
        if (taxRateText != null) {
            taxRateText.text = string.Format(LocalizationManager.GetText("LBL_TAX_RATE"), currentRate.ToString("F0"));
        }

        // 3. 납부 기한 표시 (이제 날짜만 깔끔하게 표시)
        if (dueDateText != null) {
            if (showActiveUI) {
                string dateStr = TaxManager.Instance.taxDueDate.ToString("MM/dd/yyyy");
                dueDateText.text = string.Format(LocalizationManager.GetText("LBL_TAX_DUE_DATE"), dateStr);
            } else {
                dueDateText.text = " "; // 안 보일 땐 공란
            }
        }

        // 4. [핵심] 버튼과 빨간 경고 라벨 끄고 켜기
        if (openPaymentPopupButton != null) {
            openPaymentPopupButton.gameObject.SetActive(showActiveUI);
        }

        if (penaltyLabel != null) {
            // 버튼이랑 똑같이, 세금 낼 거 있을 때만 켜짐!
            penaltyLabel.gameObject.SetActive(showActiveUI);
        }
    }

    // =========================================================
    // 버튼 이벤트 연결
    // =========================================================

    public void OnClickOpenPaymentPopup() {
        if (TaxManager.Instance.unpaidTaxAmount <= 0) return;

        if (confirmPopupPanel != null) {
            confirmPopupPanel.SetActive(true);

            if (confirmMsgText != null) {
                string unit = LocalizationManager.GetText("UNIT_CURRENCY");
                confirmMsgText.text = string.Format(LocalizationManager.GetText("MSG_TAX_CONFIRM_PAY"), TaxManager.Instance.unpaidTaxAmount.ToString("N0"), unit);
            }
        }
    }

    public void OnClickConfirmPayment() {
        double taxToPay = TaxManager.Instance.unpaidTaxAmount;

        // 1. 잔액 체크 (UI 표시용)
        if (PlayerManager.Instance.satoshiBankCash < taxToPay) {
            StartCoroutine(ShowWarningRoutine());
            return;
        }

        bool success = TaxManager.Instance.PayTax();

        if (success) {
            if (confirmPopupPanel != null) confirmPopupPanel.SetActive(false);
            RefreshTaxUI(); // UI 갱신
        }
    }

    public void OnClickClosePopup() {
        if (confirmPopupPanel != null) confirmPopupPanel.SetActive(false);
    }

    private IEnumerator ShowWarningRoutine() {
        if (warningPopupText == null) yield break;

        // [수정] 잔액 부족 경고 메시지 현지화
        warningPopupText.text = LocalizationManager.GetText("MSG_INSUFFICIENT_FUNDS_2");
        warningPopupText.canvasRenderer.SetAlpha(1f);
        yield return new WaitForSecondsRealtime(2f);
        warningPopupText.CrossFadeAlpha(0f, 0.5f, true);
    }
}