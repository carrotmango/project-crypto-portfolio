using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BankTransferUIController : MonoBehaviour {
    public TMP_Dropdown platformDropdown;
    public TMP_InputField amountInputField;
    public Button confirmButton;
    public Button all;
    public WithdrawPanelController withdrawPanelController;

    void Start() {
        InitializePlatformDropdown();
        amountInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
        amountInputField.onValueChanged.AddListener(ValidateInput);
        confirmButton.onClick.AddListener(OnConfirmTransfer);
        ValidateInput(""); // 초기 상태 확인
        amountInputField.onEndEdit.AddListener(FormatInputAsCurrency);
        all.onClick.AddListener(OnAllClicked);
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

        double available = PlayerManager.Instance.satoshiBankCash;
        confirmButton.interactable = amount <= available;
    }

    void OnConfirmTransfer() {
        string raw = amountInputField.text.Replace(",", "");
        double.TryParse(raw, out double amount);
        double available = PlayerManager.Instance.satoshiBankCash;

        if (amount > available) return;

        string platform = platformDropdown.options[platformDropdown.value].text;

        if (platform == "불비트") {
            PlayerManager.Instance.satoshiBankCash -= amount;
            PlayerManager.Instance.ChangeBullbitCash(amount);
            Debug.Log($"사토시 → 불비트 {amount}원 이체 완료");
        }
        //else if (platform == "망고카지노") {
        //    PlayerManager.Instance.satoshiBankCash -= amount;
        //    PlayerManager.Instance.mangoCasinoCash += amount;
        //    Debug.Log($"사토시 → 망고카지노 {amount}원 이체 완료");
        //}

        amountInputField.text = "";
        confirmButton.interactable = false;
        CoinManager.Instance.UpdateCashText();

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
        double amount = PlayerManager.Instance.satoshiBankCash;
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
        confirmButton.interactable = false;
    }
    public void PrepareForBullbit() {
        platformDropdown.value = 0;
        platformDropdown.RefreshShownValue();
        amountInputField.text = "";
        confirmButton.interactable = false;
    }

}
