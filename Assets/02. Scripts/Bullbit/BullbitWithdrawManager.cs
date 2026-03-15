using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class BullbitWithdrawManager : MonoBehaviour {
    // ... (기존 변수 및 리스너 로직 동일) ...

    public enum WithdrawMode { Bank, Crypto }
    private WithdrawMode currentMode = WithdrawMode.Bank;

    [Header("패널 관련")]
    public GameObject withdrawParent;
    public GameObject bankSubPanel;
    public GameObject cryptoSubPanel;

    [Header("버튼 리스너")]
    public Button openButton;
    public Button closeButtonInPanel;
    public Button leftArrow;
    public Button rightArrow;

    [Header("입력 UI 분리 연결")]
    public TMP_InputField bankWithdrawInput;
    public TMP_InputField cryptoWithdrawInput;
    public Button withdrawButton;
    public TextMeshProUGUI feeLabelText;
    public TextMeshProUGUI noticeLabelText;
    public TextMeshProUGUI convertedKrwText;

    [Header("크립토 전용 UI")]
    public TMP_Dropdown coinDropdown;
    public TMP_Dropdown networkDropdown;
    public TMP_Dropdown exchangeDropdown;
    public TMP_InputField addressInputField;

    [Header("전액 버튼 분리")]
    public Button bankAllButton;
    public Button cryptoAllButton;

    private double currentFee = 1000;
    private double currentMinWithdraw = 5000;
    private const double epsilon = 0.0000001;

    private TMP_InputField CurrentInput => (currentMode == WithdrawMode.Bank) ? bankWithdrawInput : cryptoWithdrawInput;

    void Start() {
        if (openButton != null) openButton.onClick.AddListener(OpenWithdrawPanel);
        if (closeButtonInPanel != null) closeButtonInPanel.onClick.AddListener(CloseBullbitWithdrawPanel);
        leftArrow.onClick.AddListener(() => ChangeMode(-1));
        rightArrow.onClick.AddListener(() => ChangeMode(1));

        if (bankWithdrawInput != null) bankWithdrawInput.onValueChanged.AddListener(OnValueChanged);
        if (cryptoWithdrawInput != null) cryptoWithdrawInput.onValueChanged.AddListener(OnValueChanged);

        withdrawButton.onClick.AddListener(OnClickWithdraw);
        if (bankAllButton != null) bankAllButton.onClick.AddListener(OnClickAll);
        if (cryptoAllButton != null) cryptoAllButton.onClick.AddListener(OnClickCryptoAll);

        coinDropdown.onValueChanged.AddListener((_) => RefreshCryptoSettings());
        exchangeDropdown.onValueChanged.AddListener((_) => OnExchangeChanged());

        SetupCryptoOptions();
        withdrawButton.interactable = false;
    }

    // --- (Open, Close, ChangeMode 생략) ---
    public void OpenWithdrawPanel() {
        withdrawParent.SetActive(true);
        currentMode = WithdrawMode.Bank;
        RefreshUI();
    }

    public void CloseBullbitWithdrawPanel() {
        withdrawParent.SetActive(false);
    }

    void ChangeMode(int dir) {
        int count = Enum.GetNames(typeof(WithdrawMode)).Length;
        currentMode = (WithdrawMode)(((int)currentMode + dir + count) % count);
        RefreshUI();
    }

    void SetupCryptoOptions() {
        coinDropdown.ClearOptions();
        // ETH 제거: 스테이블 코인만 출금 가능
        coinDropdown.AddOptions(new List<string> { "USDT", "USDC" });

        exchangeDropdown.ClearOptions();

        // [수정] 포넨스 거래소 이름 현지화
        string fournanceName = LocalizationManager.GetText("EXCHANGE_FOURNANCE");
        exchangeDropdown.AddOptions(new List<string> { fournanceName });
        exchangeDropdown.interactable = false; // 선택 변경 불가하게 차단

        networkDropdown.interactable = false;
        addressInputField.interactable = false; // 주소 입력 불가
    }

    void RefreshUI() {
        if (bankWithdrawInput != null) bankWithdrawInput.text = "";
        if (cryptoWithdrawInput != null) cryptoWithdrawInput.text = "";

        // [수정] 출금 가치 0원 현지화
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");
        if (convertedKrwText != null)
            convertedKrwText.text = string.Format(LocalizationManager.GetText("LBL_WITHDRAW_VALUE"), "0", unit);

        withdrawButton.interactable = false;

        bankSubPanel.SetActive(currentMode == WithdrawMode.Bank);
        cryptoSubPanel.SetActive(currentMode == WithdrawMode.Crypto);

        if (currentMode == WithdrawMode.Bank) {
            currentFee = 1000;
            currentMinWithdraw = 5000;

            // [수정] 은행 수수료 안내 문구 현지화
            feeLabelText.text = string.Format(LocalizationManager.GetText("LBL_WITHDRAW_BANK_FEE"), currentFee.ToString("N0"), unit);
        } else {
            RefreshCryptoSettings();
        }
    }
    void RefreshCryptoSettings() {
        if (currentMode == WithdrawMode.Bank) return;

        string coin = coinDropdown.options[coinDropdown.value].text;
        networkDropdown.ClearOptions();

        switch (coin) {
            case "USDT":
                networkDropdown.AddOptions(new List<string> { "TRON (TRC-20)" });
                currentFee = 1.0;
                currentMinWithdraw = 100;
                break;
            case "USDC":
                networkDropdown.AddOptions(new List<string> { "Arbitrum One" });
                currentFee = 1.0;
                currentMinWithdraw = 100;
                break;
        }

        // [수정] 크립토 수수료 및 알림 문구 현지화 (USDT/USDC 공통 적용)
        feeLabelText.text = string.Format(LocalizationManager.GetText("LBL_WITHDRAW_CRYPTO_FEE"), currentFee, coin);
        noticeLabelText.text = string.Format(LocalizationManager.GetText("LBL_WITHDRAW_CRYPTO_NOTICE"), currentMinWithdraw, coin);

        UpdateAddressUI();
        OnValueChanged(cryptoWithdrawInput.text);
    }

    void UpdateAddressUI() {
        if (currentMode != WithdrawMode.Crypto) return;
        string coin = coinDropdown.options[coinDropdown.value].text;

        // 포넨스 전용 주소 자동 입력 (다른 옵션이 없으므로 고정)
        addressInputField.text = (coin == "USDT") ? "THgrtiK99pOnAnCe" : "0xHgrtiK77vIeW3F";
    }

    // --- (OnClick 전액 버튼, OnValueChanged, OnClickWithdraw 로직 기존과 동일) ---
    public void OnClickCryptoAll() {
        if (currentMode != WithdrawMode.Crypto) return;
        string symbol = coinDropdown.options[coinDropdown.value].text.Trim().ToUpper();
        double holding = PlayerManager.Instance.GetHoldingAmount(symbol);
        double maxAmount = holding - currentFee;

        if (maxAmount < currentMinWithdraw) {
            cryptoWithdrawInput.text = "0";
            return;
        }
        cryptoWithdrawInput.text = maxAmount.ToString("F4");
    }

    public void OnClickAll() {
        if (currentMode != WithdrawMode.Bank) return;
        double holding = PlayerManager.Instance.bullbitCash;
        double maxAmount = holding - currentFee;

        if (maxAmount < currentMinWithdraw) {
            bankWithdrawInput.text = "0";
            return;
        }
        bankWithdrawInput.text = Math.Floor(maxAmount).ToString("N0");
    }

    void OnValueChanged(string input) {
        string raw = input.Replace(",", "");
        if (double.TryParse(raw, out double value)) {
            double price = 1.0;
            string symbol = "KRW";

            if (currentMode == WithdrawMode.Crypto) {
                symbol = coinDropdown.options[coinDropdown.value].text.Trim().ToUpper();
                var coinData = CoinManager.Instance.coins.Find(c => c.Symbol == symbol);
                price = (coinData != null) ? coinData.CurrentPrice : (symbol == "ETH" ? 3500000 : GlobalEconomyManager.UsdToKrw);
            }

            // [수정] 출금 가치 텍스트 현지화
            string unit = LocalizationManager.GetText("UNIT_CURRENCY");
            if (convertedKrwText != null) {
                double krwValue = value * price;
                convertedKrwText.text = string.Format(LocalizationManager.GetText("LBL_WITHDRAW_VALUE"), krwValue.ToString("N0"), unit);
            }

            double holding = (currentMode == WithdrawMode.Bank) ? PlayerManager.Instance.bullbitCash : PlayerManager.Instance.GetHoldingAmount(symbol);
            withdrawButton.interactable = value >= currentMinWithdraw && (value + currentFee) <= holding + epsilon;
        } else {
            // [수정] 파싱 실패 시 0원으로 현지화
            string unit = LocalizationManager.GetText("UNIT_CURRENCY");
            if (convertedKrwText != null)
                convertedKrwText.text = string.Format(LocalizationManager.GetText("LBL_WITHDRAW_VALUE"), "0", unit);

            withdrawButton.interactable = false;
        }
    }

    public void OnClickWithdraw() {
        string raw = CurrentInput.text.Replace(",", "");
        if (!double.TryParse(raw, out double amount)) return;

        if (currentMode == WithdrawMode.Bank) {
            double totalDeduct = amount + currentFee;

            // 마이너스 금액을 넣어서 출금 처리 (원금도 같이 차감됨)
            PlayerManager.Instance.ChangeBullbitCash(-totalDeduct);
            PlayerManager.Instance.satoshiBankCash += amount;

            // [수정] 거래 기록 하드코딩 제거
            string logDesc = LocalizationManager.GetText("LOG_BULLBIT");
            string logType = LocalizationManager.GetText("LOG_DEPOSIT");
            string logAsset = LocalizationManager.GetText("LOG_SATOSHI_CASH");
            TransactionManager.Instance.AddRecord(logDesc, amount, logType, logAsset);

        } else {
            string symbol = coinDropdown.options[coinDropdown.value].text.Trim().ToUpper();
            string destination = exchangeDropdown.options[exchangeDropdown.value].text;

            // [핵심 보완] 한글/영문 상관없이 "포넨스 거래소"인지 판별할 수 있도록 현지화 키값과 대조합니다!
            string fournanceName = LocalizationManager.GetText("EXCHANGE_FOURNANCE");

            if (destination.Contains(fournanceName) && symbol == "ETH") return;

            // 1. 현물 지갑에서 코인 차감 (테더/USDC 수량 그대로)
            PlayerManager.Instance.ChangeCoin(symbol, -(amount + currentFee));

            if (destination.Contains(fournanceName)) {
                if (symbol == "USDT" || symbol == "USDC") {
                    PlayerManager.Instance.fournanceCash += amount;
                    Debug.Log($"[포넨스 입금] 스테이블 코인 {symbol} 수량 그대로 ${amount:F2} 입금 완료");
                } else {
                    var coinData = CoinManager.Instance.coins.Find(c => c.Symbol == symbol);
                    double priceInKrw = (coinData != null) ? coinData.CurrentPrice : 0;
                    double amountInUsd = (amount * priceInKrw) / GlobalEconomyManager.UsdToKrw;
                    PlayerManager.Instance.fournanceCash += amountInUsd;
                }
            }
        }
        CoinManager.Instance.UpdateCashText();
        CloseBullbitWithdrawPanel();
    }

    void OnExchangeChanged() {
        UpdateAddressUI();
    }
    //void FilterExchangeOptions(string selectedCoin) {
    //    string currentSelection = exchangeDropdown.options[exchangeDropdown.value].text;
    //    exchangeDropdown.ClearOptions();

    //    List<string> options = new List<string>();

    //    // ETH가 아닐 때만 포넨스 거래소 추가
    //    if (selectedCoin != "ETH") {
    //        options.Add("포넨스 거래소");
    //    }

    //    options.Add("고스트 월렛");
    //    options.Add("외부 직접 입력");

    //    exchangeDropdown.AddOptions(options);

    //    // 이전에 선택했던 게 리스트에 여전히 있으면 유지, 없으면 첫 번째로 초기화
    //    int newIndex = options.FindIndex(x => x == currentSelection);
    //    exchangeDropdown.value = (newIndex != -1) ? newIndex : 0;
    //    exchangeDropdown.RefreshShownValue();
    //}

}