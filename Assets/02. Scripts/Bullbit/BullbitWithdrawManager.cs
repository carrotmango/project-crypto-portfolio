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
        exchangeDropdown.AddOptions(new List<string> { "포넨스 거래소", "고스트 월렛", "외부 직접 입력" });
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
            feeLabelText.text = $"출금액은 은행계좌에 입금되며,\n 수수료  {currentFee:N0}원이 부과됩니다. 최소 출금 5,000원";
            //noticeLabelText.text = "최소 이체 금액: 5,000원\n사토시 은행으로 즉시 전송됩니다.";
        } else {
            RefreshCryptoSettings();
        }
    }

    // [핵심 업데이트] 코인별 상세 경고문 및 수수료 설정
    void RefreshCryptoSettings() {
        if (currentMode == WithdrawMode.Bank) return;

        string coin = coinDropdown.options[coinDropdown.value].text;
        networkDropdown.ClearOptions();

        // 1. 거래소 드롭다운 필터링 로직 추가
        FilterExchangeOptions(coin);

        switch (coin) {
            case "USDT":
                networkDropdown.AddOptions(new List<string> { "TRON (TRC-20)" });
                currentFee = 1.0;
                currentMinWithdraw = 100;
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
            // [수정] 은행으로 출금
            double totalDeduct = amount + currentFee;

            // 마이너스 금액을 넣어서 출금 처리 (원금도 같이 차감됨)
            PlayerManager.Instance.ChangeBullbitCash(-totalDeduct);

            PlayerManager.Instance.satoshiBankCash += amount;
        } else {
            string symbol = coinDropdown.options[coinDropdown.value].text.Trim().ToUpper();
            string destination = exchangeDropdown.options[exchangeDropdown.value].text;

            // 포넨스 입금 시 ETH 차단 (Filter에서 이미 막았지만 이중 방어)
            if (destination.Contains("포넨스") && symbol == "ETH") return;

            // 1. 현물 지갑에서 코인 차감 (테더/USDC 수량 그대로)
            PlayerManager.Instance.ChangeCoin(symbol, -(amount + currentFee));

            if (destination.Contains("포넨스")) {
                // [수정된 고증] 스테이블 코인은 1:1로 바로 입금
                if (symbol == "USDT" || symbol == "USDC") {
                    // 환율 안 따지고 테더 수량만큼 달러 잔고 업!
                    PlayerManager.Instance.fournanceCash += amount;
                    Debug.Log($"[포넨스 입금] 스테이블 코인 {symbol} 수량 그대로 ${amount:F2} 입금 완료");
                } else {
                    // 혹시 나중에 다른 코인 추가될 때를 위한 기존 환율 로직 (ETH 등)
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
    void FilterExchangeOptions(string selectedCoin) {
        string currentSelection = exchangeDropdown.options[exchangeDropdown.value].text;
        exchangeDropdown.ClearOptions();

        List<string> options = new List<string>();

        // ETH가 아닐 때만 포넨스 거래소 추가
        if (selectedCoin != "ETH") {
            options.Add("포넨스 거래소");
        }

        options.Add("고스트 월렛");
        options.Add("외부 직접 입력");

        exchangeDropdown.AddOptions(options);

        // 이전에 선택했던 게 리스트에 여전히 있으면 유지, 없으면 첫 번째로 초기화
        int newIndex = options.FindIndex(x => x == currentSelection);
        exchangeDropdown.value = (newIndex != -1) ? newIndex : 0;
        exchangeDropdown.RefreshShownValue();
    }

}