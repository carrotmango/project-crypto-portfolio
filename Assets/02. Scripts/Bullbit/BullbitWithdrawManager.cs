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
        coinDropdown.AddOptions(new List<string> { "USDT", "USDC", "ETH" });
        exchangeDropdown.ClearOptions();
        exchangeDropdown.AddOptions(new List<string> { "포넨스 (Fournace)", "고스트 월렛", "외부 직접 입력" });
        networkDropdown.interactable = false;
    }

    void RefreshUI() {
        if (bankWithdrawInput != null) bankWithdrawInput.text = "";
        if (cryptoWithdrawInput != null) cryptoWithdrawInput.text = "";
        if (convertedKrwText != null) convertedKrwText.text = "출금 가치: 0원";
        withdrawButton.interactable = false;

        bankSubPanel.SetActive(currentMode == WithdrawMode.Bank);
        cryptoSubPanel.SetActive(currentMode == WithdrawMode.Crypto);

        if (currentMode == WithdrawMode.Bank) {
            currentFee = 1000;
            currentMinWithdraw = 5000;
            feeLabelText.text = $"수수료: {currentFee:N0}원";
            noticeLabelText.text = "최소 이체 금액: 5,000원\n사토시 은행으로 즉시 전송됩니다.";
        } else {
            RefreshCryptoSettings();
        }
    }

    // [핵심 업데이트] 코인별 상세 경고문 및 수수료 설정
    void RefreshCryptoSettings() {
        if (currentMode == WithdrawMode.Bank) return;
        string coin = coinDropdown.options[coinDropdown.value].text;
        networkDropdown.ClearOptions();

        switch (coin) {
            case "USDT":
                networkDropdown.AddOptions(new List<string> { "TRON (TRC-20)" });
                currentFee = 1.0;      // 1 USDT
                currentMinWithdraw = 100; // 100 USDT
                feeLabelText.text = $"수수료: {currentFee} USDT";
                noticeLabelText.text = $"<color=red>⚠ 최소 출금: {currentMinWithdraw} USDT</color>\n네트워크 수수료 {currentFee} USDT가 별도로 차감됩니다.";
                break;
            case "USDC":
                networkDropdown.AddOptions(new List<string> { "Arbitrum One" });
                currentFee = 1.0;
                currentMinWithdraw = 100;
                feeLabelText.text = $"수수료: {currentFee} USDC";
                noticeLabelText.text = $"<color=red>⚠ 최소 출금: {currentMinWithdraw} USDC</color>\n네트워크 수수료 {currentFee} USDC가 별도로 차감됩니다.";
                break;
            case "ETH":
                networkDropdown.AddOptions(new List<string> { "Ethereum (ERC-20)" });
                currentFee = 0.005;
                currentMinWithdraw = 0.05;
                feeLabelText.text = $"수수료: {currentFee:F3} ETH";
                noticeLabelText.text = $"<color=red>⚠ 최소 출금: {currentMinWithdraw} ETH</color>\n메인넷 가스비 {currentFee} ETH가 발생합니다.";
                break;
        }
        UpdateAddressUI();
        OnValueChanged(cryptoWithdrawInput.text);
    }

    void UpdateAddressUI() {
        if (currentMode != WithdrawMode.Crypto) return;
        string coin = coinDropdown.options[coinDropdown.value].text;
        string exchange = exchangeDropdown.options[exchangeDropdown.value].text;

        if (exchange.Contains("외부")) {
            addressInputField.interactable = true;
            addressInputField.text = "";
            var placeholder = addressInputField.placeholder.GetComponent<TextMeshProUGUI>();
            placeholder.text = (coin == "USDT") ? "T... 트론 주소를 입력하세요" : "0x... 주소를 입력하세요";
        } else {
            addressInputField.interactable = false;
            if (exchange.Contains("포넨스")) {
                addressInputField.text = (coin == "USDT") ? "THgrtiK99pOnAnCe" : "0xHgrtiK77vIeW3F";
            } else {
                addressInputField.text = (coin == "USDT") ? "T0ed81b55gHoStWl" : "0x0ed81b22eVmWaLt";
            }
        }
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

            if (convertedKrwText != null) {
                double krwValue = value * price;
                convertedKrwText.text = $"출금 가치: {krwValue:N0}원";
            }

            double holding = (currentMode == WithdrawMode.Bank) ? PlayerManager.Instance.bullbitCash : PlayerManager.Instance.GetHoldingAmount(symbol);
            withdrawButton.interactable = value >= currentMinWithdraw && (value + currentFee) <= holding + epsilon;
        } else {
            if (convertedKrwText != null) convertedKrwText.text = "출금 가치: 0원";
            withdrawButton.interactable = false;
        }
    }

    public void OnClickWithdraw() {
        string raw = CurrentInput.text.Replace(",", "");
        if (!double.TryParse(raw, out double amount)) return;

        if (currentMode == WithdrawMode.Bank) {
            PlayerManager.Instance.bullbitCash -= (amount + currentFee);
            PlayerManager.Instance.satoshiBankCash += amount;
            Debug.Log($"[은행] {amount:N0}원 이체 완료");
        } else {
            string symbol = coinDropdown.options[coinDropdown.value].text.Trim().ToUpper();
            PlayerManager.Instance.ChangeCoin(symbol, -(amount + currentFee));

            if (exchangeDropdown.options[exchangeDropdown.value].text.Contains("포넨스")) {
                var coinData = CoinManager.Instance.coins.Find(c => c.Symbol == symbol);
                double price = (coinData != null) ? coinData.CurrentPrice : (symbol == "ETH" ? 3500000 : GlobalEconomyManager.UsdToKrw);
                PlayerManager.Instance.fournanceCash += (amount * price);
                Debug.Log($"[포넨스] {amount} {symbol} 입금 완료");
            }
        }
        CoinManager.Instance.UpdateCashText();
        CloseBullbitWithdrawPanel();
    }

    void OnExchangeChanged() {
        UpdateAddressUI();
    }
}