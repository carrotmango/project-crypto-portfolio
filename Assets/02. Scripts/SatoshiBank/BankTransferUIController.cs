using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class BankTransferUIController : MonoBehaviour {
    public TMP_Dropdown platformDropdown;
    public TMP_InputField amountInputField;
    public Button confirmButton;
    public Button all;
    public WithdrawPanelController withdrawPanelController;
    public TextMeshProUGUI possibleAmount;

    [Header("Fournance Settings")]
    public GameObject fournanceWarningUI;
    public TextMeshProUGUI expectedReceiveText;

    private const long FOURNANCE_UNLOCK_CAPITAL = 100_000_000;
    private double currentValidAmount = 0;

    private void OnEnable() {
        InitializePlatformDropdown();
        RefreshUI();
    }

    void Start() {
        amountInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
        amountInputField.onValueChanged.AddListener(ValidateInput);
        amountInputField.onEndEdit.AddListener(FormatInputAsCurrency);
        confirmButton.onClick.AddListener(OnConfirmTransfer);
        all.onClick.AddListener(OnAllClicked);
        platformDropdown.onValueChanged.AddListener(OnPlatformChanged);

        ValidateInput("");
    }

    void Update() {
        // 매 프레임 실시간 환율 반영 (포넨스 거래소 선택 시에만 갱신)
        if (expectedReceiveText != null && expectedReceiveText.gameObject.activeInHierarchy) {
            if (currentValidAmount > 0) {
                UpdateExpectedReceiveText(currentValidAmount);
            }
        }
    }

    void InitializePlatformDropdown() {
        platformDropdown.ClearOptions();

        // [수정] 드롭다운 메뉴 이름 현지화
        string bullbitName = LocalizationManager.GetText("PLATFORM_BULLBIT");
        List<string> options = new List<string> { bullbitName };

        if (OfficeManager.Instance != null && OfficeManager.Instance.companyCapital >= FOURNANCE_UNLOCK_CAPITAL) {
            options.Add(LocalizationManager.GetText("EXCHANGE_FOURNANCE"));
        }

        platformDropdown.AddOptions(options);
        OnPlatformChanged(platformDropdown.value);
    }

    void OnPlatformChanged(int index) {
        if (platformDropdown.options.Count <= index) return;

        string selectedPlatform = platformDropdown.options[index].text;
        string fournanceName = LocalizationManager.GetText("EXCHANGE_FOURNANCE");

        if (fournanceWarningUI != null) {
            // [수정] 현지화된 이름으로 비교
            fournanceWarningUI.SetActive(selectedPlatform == fournanceName);
        }

        UpdateExpectedReceiveText(currentValidAmount);
    }

    void ValidateInput(string input) {
        if (string.IsNullOrEmpty(input)) {
            confirmButton.interactable = false;
            currentValidAmount = 0;
            UpdateExpectedReceiveText(0);
            return;
        }

        string raw = input.Replace(",", "");
        if (!double.TryParse(raw, out double enteredAmount)) {
            confirmButton.interactable = false;
            currentValidAmount = 0;
            UpdateExpectedReceiveText(0);
            return;
        }

        double available = System.Math.Floor(PlayerManager.Instance.satoshiBankCash);

        if (enteredAmount > available) {
            enteredAmount = available;
            amountInputField.text = enteredAmount.ToString("N0");
            amountInputField.caretPosition = amountInputField.text.Length;
        }

        confirmButton.interactable = (enteredAmount > 0 && enteredAmount <= available);
        currentValidAmount = enteredAmount;
        UpdateExpectedReceiveText(currentValidAmount);
    }

    void UpdateExpectedReceiveText(double amount) {
        if (expectedReceiveText == null) return;

        if (amount <= 0) {
            expectedReceiveText.text = "-";
            return;
        }

        if (platformDropdown.options.Count == 0) return;
        string selectedPlatform = platformDropdown.options[platformDropdown.value].text;
        string bullbitName = LocalizationManager.GetText("PLATFORM_BULLBIT");
        string fournanceName = LocalizationManager.GetText("EXCHANGE_FOURNANCE");
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");

        if (selectedPlatform == bullbitName) {
            // [수정] 불비트 예상 수령액 현지화
            expectedReceiveText.text = string.Format(LocalizationManager.GetText("LBL_EXPECTED_RECEIPT_KRW"), amount.ToString("N0"), unit);
            expectedReceiveText.color = Color.white;
        } else if (selectedPlatform == fournanceName) {
            double netAmountKrw = amount * 0.90; // 수수료 10% 차감
            double currentRate = GlobalEconomyManager.UsdToKrw; // 실시간 환율 호출
            double expectedUsd = netAmountKrw / currentRate;

            // [수정] 포넨스 예상 수령액 및 환율 안내 현지화
            expectedReceiveText.text = string.Format(LocalizationManager.GetText("LBL_EXPECTED_RECEIPT_USD"), expectedUsd.ToString("N2"), currentRate.ToString("N2"), unit);
            expectedReceiveText.color = new Color32(50, 214, 149, 255);
        }
    }

    public void RefreshUI() {
        showPossibleAmount();
        ValidateInput(amountInputField.text);
    }

    void showPossibleAmount() {
        double amount = PlayerManager.Instance.satoshiBankCash;
        if (possibleAmount != null) {
            // [수정] 가능 금액 현지화
            string unit = LocalizationManager.GetText("UNIT_CURRENCY");
            possibleAmount.text = string.Format(LocalizationManager.GetText("LBL_POSSIBLE_AMOUNT"), amount.ToString("N0"), unit);
        }
    }

    void OnConfirmTransfer() {
        string raw = amountInputField.text.Replace(",", "");
        double.TryParse(raw, out double amount);

        double available = System.Math.Floor(PlayerManager.Instance.satoshiBankCash);
        if (amount > available || amount <= 0) return;

        string platform = platformDropdown.options[platformDropdown.value].text;
        string bullbitName = LocalizationManager.GetText("PLATFORM_BULLBIT");
        string fournanceName = LocalizationManager.GetText("EXCHANGE_FOURNANCE");

        string logType = LocalizationManager.GetText("LOG_WITHDRAW"); // 출금
        string logAsset = LocalizationManager.GetText("LOG_SATOSHI_CASH"); // 사토시 현금

        if (platform == bullbitName) {
            PlayerManager.Instance.satoshiBankCash -= amount;
            PlayerManager.Instance.ChangeBullbitCash(amount);

            // [수정] 불비트 송금 기록 현지화
            string logDesc = LocalizationManager.GetText("LOG_BULLBIT");
            TransactionManager.Instance.AddRecord(logDesc, amount, logType, logAsset);
        } else if (platform == fournanceName) {
            PlayerManager.Instance.satoshiBankCash -= amount;

            double feeKrw = amount * 0.10;
            double netAmountKrw = amount - feeKrw;
            double currentExchangeRate = GlobalEconomyManager.UsdToKrw;
            double convertedUsd = netAmountKrw / currentExchangeRate;

            PlayerManager.Instance.fournanceCash += convertedUsd;

            // [수정] 포넨스 송금 기록 현지화 (수수료 내역 포함)
            string logDesc = LocalizationManager.GetText("LOG_FOURNANCE");
            string unit = LocalizationManager.GetText("UNIT_CURRENCY");
            string logDetail = string.Format(LocalizationManager.GetText("LOG_OVERSEAS_REMITTANCE"), feeKrw.ToString("N0"), unit);
            TransactionManager.Instance.AddRecord(logDesc, amount, logType, logDetail);
        }

        amountInputField.text = "";
        currentValidAmount = 0;
        UpdateExpectedReceiveText(0);
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
        currentValidAmount = 0;
        UpdateExpectedReceiveText(0);
        RefreshUI();
        confirmButton.interactable = false;
    }

    public void PrepareForBullbit() {
        platformDropdown.value = 0;
        platformDropdown.RefreshShownValue();
        amountInputField.text = "";
        currentValidAmount = 0;
        UpdateExpectedReceiveText(0);
        confirmButton.interactable = false;
    }
}