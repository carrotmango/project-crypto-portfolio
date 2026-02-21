using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BankTransferUIController : MonoBehaviour {
    public TMP_Dropdown platformDropdown;
    public TMP_InputField amountInputField;
    public Button confirmButton;
    public Button all;
    public WithdrawPanelController withdrawPanelController;
    public TextMeshProUGUI possibleAmount;

    void Start() {
        InitializePlatformDropdown();
        amountInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
        amountInputField.onValueChanged.AddListener(ValidateInput);
        confirmButton.onClick.AddListener(OnConfirmTransfer);
        ValidateInput(""); // 초기 상태 확인
        amountInputField.onEndEdit.AddListener(FormatInputAsCurrency);
        all.onClick.AddListener(OnAllClicked);
        RefreshUI();
    }

    void InitializePlatformDropdown() {
        platformDropdown.ClearOptions();
        platformDropdown.AddOptions(new System.Collections.Generic.List<string> { "불비트" });
    }

    void ValidateInput(string input) {
        string raw = input.Replace(",", "");
        if (!double.TryParse(raw, out double amount) || amount <= 0) {
            confirmButton.interactable = false;
            return;
        }

        double available = System.Math.Floor(PlayerManager.Instance.satoshiBankCash);
        confirmButton.interactable = amount <= available;
    }

    public void RefreshUI() {
        showPossibleAmount();
        ValidateInput(amountInputField.text);
    }

    void showPossibleAmount() {
        double amount = PlayerManager.Instance.satoshiBankCash;

        if (possibleAmount != null) {
            // :N0는 천 단위 콤마를 찍어줍니다 (예: 1,000,000)
            possibleAmount.text = $"가능 금액: {amount:N0}원";
        }
    }

    void OnConfirmTransfer() {
        string raw = amountInputField.text.Replace(",", "");
        double.TryParse(raw, out double amount);

        // [수정] 비교 대상 잔액도 정수로 처리
        double available = System.Math.Floor(PlayerManager.Instance.satoshiBankCash);

        if (amount > available) return;

        string platform = platformDropdown.options[platformDropdown.value].text;

        if (platform == "불비트") {
            // [핵심] 입력받은 '정수' 금액만큼만 정확히 뺍니다. 
            // 잔액에 남은 0.213...원은 그대로 은행에 남겨두거나 아예 무시합니다.
            PlayerManager.Instance.satoshiBankCash -= amount;
            PlayerManager.Instance.ChangeBullbitCash(amount);

            TransactionManager.Instance.AddRecord("불비트", amount, "출금", "사토시 현금");
            Debug.Log($"사토시 → 불비트 {amount:N0}원 이체 완료 (소수점 제외)");
        }

        amountInputField.text = "";
        confirmButton.interactable = false;
        CoinManager.Instance.UpdateCashText();

        RefreshUI();

        if (withdrawPanelController != null)
            withdrawPanelController.ClosePanel();

        CoinManager.Instance.RenderBankCashOnce();
    }

    void FormatInputAsCurrency(string input) {
        string raw = input.Replace(",", "");
        if (double.TryParse(raw, out double value)) {
            amountInputField.text = $"{value:N0}";
            amountInputField.caretPosition = amountInputField.text.Length;
            ValidateInput(amountInputField.text);
        }
    }

    void OnAllClicked() {
        double amount = System.Math.Floor(PlayerManager.Instance.satoshiBankCash);

        amountInputField.text = $"{amount:N0}";
        amountInputField.caretPosition = amountInputField.text.Length;
        ValidateInput(amountInputField.text);
    }

    public void PrepareForBullbitDeposit() {
        if (platformDropdown != null) {
            platformDropdown.value = 0;
            platformDropdown.RefreshShownValue();
        }

        amountInputField.text = "";
        RefreshUI();
        confirmButton.interactable = false;
    }
    public void PrepareForBullbit() {
        platformDropdown.value = 0;
        platformDropdown.RefreshShownValue();
        amountInputField.text = "";
        confirmButton.interactable = false;
    }

}
