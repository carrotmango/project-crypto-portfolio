using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class FourNanceWithdrawManager : MonoBehaviour {

    [Header("패널 제어")]
    public GameObject withdrawPanel;
    public Button closeButton;

    [Header("입력 및 표시 UI")]
    public TMP_InputField amountInput;
    public TextMeshProUGUI availableBalanceText;
    public TextMeshProUGUI noticeText; // 경고문구 출력용 UI
    public Button withdrawButton;
    public Button maxAmountButton;

    [Header("옵션 선택 UI")]
    public TMP_Dropdown coinDropdown;
    public TMP_Dropdown platformDropdown;
    public TMP_Dropdown networkDropdown;
    public TMP_InputField addressInput;

    [Header("KRW Transfer Settings")]
    public TextMeshProUGUI expectedReceiveText; // 예상 수령액 출력용 별도 UI

    private double currentFee = 1;
    private double minWithdraw = 10;
    private double currentValidAmount = 0;

    void Start() {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (withdrawButton != null) withdrawButton.onClick.AddListener(OnClickWithdraw);
        if (maxAmountButton != null) maxAmountButton.onClick.AddListener(OnClickAll);

        if (amountInput != null) amountInput.onValueChanged.AddListener(OnAmountChanged);
        if (coinDropdown != null) coinDropdown.onValueChanged.AddListener(delegate { RefreshOptions(); });

        SetupDropdowns();
    }

    void Update() {
        if (expectedReceiveText != null && expectedReceiveText.gameObject.activeInHierarchy) {
            if (currentValidAmount > 0) {
                UpdateExpectedReceiveText(currentValidAmount);
            }
        }
    }

    public void OpenPanel() {
        withdrawPanel.SetActive(true);
        currentValidAmount = 0;
        if (amountInput != null) amountInput.text = "";
        UpdateExpectedReceiveText(0);
        SetupDropdowns();
        RefreshUI();
    }

    public void ClosePanel() {
        withdrawPanel.SetActive(false);
    }

    void SetupDropdowns() {
        coinDropdown.ClearOptions();
        coinDropdown.AddOptions(new List<string> { "USTT", "USCC", "KRW" });
        RefreshOptions();
    }

    void RefreshOptions() {
        if (coinDropdown.options.Count == 0) return;
        string selectedCoin = coinDropdown.options[coinDropdown.value].text;

        networkDropdown.ClearOptions();
        platformDropdown.ClearOptions();

        if (selectedCoin == "USTT") {
            platformDropdown.AddOptions(new List<string> { "불비트 거래소" });
            networkDropdown.AddOptions(new List<string> { "TRON (TRC-20)" });
            addressInput.text = "T-Bullbit-HotWallet-Deposit";
            currentFee = 1;
        } else if (selectedCoin == "USCC") {
            platformDropdown.AddOptions(new List<string> { "불비트 거래소" });
            networkDropdown.AddOptions(new List<string> { "Supatrum One" });
            addressInput.text = "0x-Bullbit-HotWallet-Deposit";
            currentFee = 1;
        } else if (selectedCoin == "KRW") {
            platformDropdown.AddOptions(new List<string> { "사토시 은행" });
            networkDropdown.AddOptions(new List<string> { "Bank Transfer" });
            addressInput.text = "Satoshi-Bank-Account";
            currentFee = 0;
        }

        platformDropdown.interactable = false;
        addressInput.interactable = false;

        RefreshUI();
        UpdateExpectedReceiveText(currentValidAmount);
    }

    private double GetWithdrawableAmount() {
        double safeCash = PlayerManager.Instance.fournanceCash;

        if (FutureChartRenderer.Instance != null) {
            double totalPnL = FutureChartRenderer.Instance.CalculateTotalUnrealizedPnL();
            double floatingLoss = Math.Min(0, totalPnL);
            return Math.Max(0, safeCash + floatingLoss);
        }
        return safeCash;
    }

    void RefreshUI() {
        if (PlayerManager.Instance == null) return;

        double withdrawableAmount = GetWithdrawableAmount();

        if (availableBalanceText != null) {
            double finalWithdrawable = Math.Max(0, withdrawableAmount - currentFee);
            // [수정] 출금 가능 금액 현지화
            availableBalanceText.text = string.Format(LocalizationManager.GetText("LBL_AVAILABLE_BALANCE"), finalWithdrawable.ToString("N2"));
        }

        if (noticeText != null) {
            string selectedCoin = coinDropdown.options[coinDropdown.value].text;
            if (selectedCoin == "KRW") {
                // [수정] 환전 수수료 안내 문구 현지화
                noticeText.text = LocalizationManager.GetText("MSG_OVERSEAS_TRANSFER_FEE_NOTICE");
                noticeText.color = new Color32(230, 60, 60, 255);
            } else {
                // [수정] 코인 출금 수수료 안내 문구 현지화
                noticeText.text = string.Format(LocalizationManager.GetText("LBL_WITHDRAW_CRYPTO_FEE"), currentFee, selectedCoin);
                noticeText.color = new Color32(230, 60, 60, 255);
            }
        }
    }

    void OnClickAll() {
        double withdrawableAmount = GetWithdrawableAmount();
        double rawMax = withdrawableAmount - currentFee;
        double safeMax = FloorToTwoDecimal(rawMax);
        safeMax = Math.Max(0, safeMax);

        amountInput.text = safeMax.ToString("F2");
    }

    void OnAmountChanged(string val) {
        if (!double.TryParse(val, out double inputAmount)) {
            withdrawButton.interactable = false;
            currentValidAmount = 0;
            UpdateExpectedReceiveText(0);
            return;
        }

        double withdrawableAmount = GetWithdrawableAmount();

        inputAmount = Math.Round(inputAmount, 2);
        double totalCost = Math.Round(inputAmount + currentFee, 2);
        withdrawableAmount = Math.Round(withdrawableAmount, 2);

        bool isValid = inputAmount >= minWithdraw && totalCost <= withdrawableAmount;

        withdrawButton.interactable = isValid;
        currentValidAmount = inputAmount;
        UpdateExpectedReceiveText(currentValidAmount);
    }

    void UpdateExpectedReceiveText(double amount) {
        if (expectedReceiveText == null) return;

        string selectedCoin = coinDropdown.options[coinDropdown.value].text;

        if (selectedCoin != "KRW" || amount <= 0) {
            expectedReceiveText.text = "";
            return;
        }

        double netAmountUsd = amount * 0.90;
        double currentRate = GlobalEconomyManager.UsdToKrw;
        double receiveKrw = netAmountUsd * currentRate;

        // [수정] 예상 수령액 및 환율 안내 문구 현지화
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");
        expectedReceiveText.text = string.Format(LocalizationManager.GetText("LBL_EXPECTED_RECEIPT_USD"), receiveKrw.ToString("N0"), currentRate.ToString("N2"), unit);
        expectedReceiveText.color = new Color32(50, 214, 149, 255);
    }

    void OnClickWithdraw() {
        if (!double.TryParse(amountInput.text, out double amount)) return;

        double withdrawableAmount = GetWithdrawableAmount();
        double totalCost = amount + currentFee;

        if (totalCost > withdrawableAmount + 0.00001) return;

        if (totalCost > withdrawableAmount) {
            totalCost = withdrawableAmount;
        }

        PlayerManager.Instance.fournanceCash -= totalCost;
        if (PlayerManager.Instance.fournanceCash < 0) PlayerManager.Instance.fournanceCash = 0;

        string coinSymbol = coinDropdown.options[coinDropdown.value].text;

        if (coinSymbol == "KRW") {
            double feeUsd = amount * 0.10;
            double netUsd = amount - feeUsd;
            double receiveKrw = netUsd * GlobalEconomyManager.UsdToKrw;

            PlayerManager.Instance.satoshiBankCash += receiveKrw;

            // ★ [핵심 수정] 기록은 무조건 한글 원본 고정! (입금)
            // 출처를 "포넨스"로, 상세 내역에 수수료 명시
            string assetDetail = $"해외송금 (수수료 10%: -${feeUsd:N2})";
            TransactionManager.Instance.AddRecord("포넨스", receiveKrw, "입금", assetDetail);

        } else {
            CoinData coinData = CoinManager.Instance.coins.Find(c => c.Symbol == coinSymbol);
            double currentPriceKrw = coinData != null ? coinData.CurrentPrice : GlobalEconomyManager.UsdToKrw;
            PlayerManager.Instance.RegisterTransferIn(coinSymbol, currentPriceKrw, amount);
        }

        currentValidAmount = 0;
        amountInput.text = "";
        UpdateExpectedReceiveText(0);
        ClosePanel();
    }

    private double FloorToTwoDecimal(double value) {
        return Math.Floor(value * 100.0) / 100.0;
    }
}