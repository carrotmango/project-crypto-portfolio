using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SellPanelController : MonoBehaviour {
    public GameObject panel;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI availableAmountText;
    public TextMeshProUGUI availableCashText;
    public TMP_InputField orderAmountInput;
    public TMP_InputField totalCostInput;
    public Button confirmButton;
    public Button resetButton;
    public TextMeshProUGUI amountUnitLabel;
    public TextMeshProUGUI totalCostUnitLabel;
    private CoinData currentCoinData;

    // [추가] 차트 스크립트를 연결할 변수
    [Header("외부 연결")]
    public LiveChartRenderer chartRenderer;

    private string lastSelectedSymbol;
    private double price;
    private double feeRate = 0.0005;
    private bool isUpdating = false;
    private double owned;

    public void OpenPanel(CoinData coin) {
        currentCoinData = coin;
        panel.SetActive(true);
        Time.timeScale = 0;

        lastSelectedSymbol = coin.Symbol;
        price = coin.CurrentPrice;
        owned = PlayerManager.Instance.GetHoldingAmount(coin.Symbol);

        priceText.text = FormatKRW(price);
        availableAmountText.text = $"{owned:N4}";
        availableCashText.text = FormatKRW(owned * price);

        amountUnitLabel.text = $"({coin.Symbol})";
        totalCostUnitLabel.text = "(KRW)";

        orderAmountInput.text = "";
        totalCostInput.text = "";
        confirmButton.interactable = false;
    }

    public void ClosePanel() {
        panel.SetActive(false);
        Time.timeScale = 1;
    }

    // ... (OnOrderAmountChanged, OnTotalCostChanged, OnPercentButtonClicked, OnResetClicked 등 중간 생략 - 기존과 동일) ...

    public void OnOrderAmountChanged(string input) {
        if (isUpdating) return;
        isUpdating = true;
        if (double.TryParse(input, out double amount)) {
            double raw = price * amount;
            totalCostInput.text = Math.Floor(raw).ToString("F0");
            confirmButton.interactable = amount > 0 && raw >= 5000 && amount <= owned + 0.00001;
        } else {
            totalCostInput.text = "";
            confirmButton.interactable = false;
        }
        isUpdating = false;
    }

    public void OnTotalCostChanged(string input) {
        if (isUpdating) return;
        isUpdating = true;
        string sanitized = input.Replace(",", "").Replace("\u20A9", "").Replace("\uFFE6", "").Replace("₩", "");
        if (double.TryParse(sanitized, out double total)) {
            double amount = total / price;
            orderAmountInput.text = amount.ToString("0.####");
            double raw = price * amount;
            bool valid = amount > 0 && raw >= 5000 && amount <= owned + 0.00001;
            confirmButton.interactable = valid;
        } else {
            orderAmountInput.text = "";
            confirmButton.interactable = false;
        }
        isUpdating = false;
    }

    public void OnPercentButtonClicked(float percent) {
        double amount = owned * (percent / 100.0);
        isUpdating = true;
        orderAmountInput.text = amount.ToString("0.####");
        isUpdating = false;
        OnOrderAmountChanged(orderAmountInput.text);
    }

    public void OnResetClicked() {
        orderAmountInput.text = "";
        totalCostInput.text = "";
        confirmButton.interactable = false;
    }

    public void OnConfirmClicked() {
        if (!double.TryParse(orderAmountInput.text, out double amount)) {
            Debug.LogWarning("잘못된 수량을 입력했습니다.");
            return;
        }

        if (amount <= 0) return;

        // UI상의 보유량 체크
        if (amount > owned + 0.00001) {
            Debug.LogWarning("보유 수량을 초과하여 매도할 수 없습니다.");
            return;
        }

        double rawPrice = price * amount;
        if (rawPrice < 5000) {
            Debug.LogWarning("최소 주문 금액은 5,000원 이상이어야 합니다.");
            return;
        }

        // PlayerManager의 RegisterSell을 호출하여 실제 매도 처리 시도
        if (PlayerManager.Instance.RegisterSell(lastSelectedSymbol, price, ref amount)) {
            double raw = price * amount;
            double fee = raw * feeRate;
            double net = raw - fee;

            PlayerManager.Instance.bullbitCash += net;

            Debug.Log($"[매도 체결] {lastSelectedSymbol} {price} x {amount} = {FormatKRW(raw)} - 수수료 {FormatKRW(fee)} → {FormatKRW(net)}");

            // [추가] 차트에 매도(S) 마크 찍기 (false = Sell)
            if (chartRenderer != null) {
                chartRenderer.RegisterTrade(currentCoinData, false);
            }

            ClosePanel();
        } else {
            Debug.LogWarning("매도 처리에 실패했습니다. 보유 수량을 다시 확인해주세요.");
        }
    }

    void Start() {
        orderAmountInput.onValueChanged.AddListener(OnOrderAmountChanged);
        totalCostInput.onValueChanged.AddListener(OnTotalCostChanged);
        confirmButton.onClick.AddListener(OnConfirmClicked);
        resetButton.onClick.AddListener(OnResetClicked);
    }

    private string FormatKRW(double value) {
        if (value < 0.0001) return "₩0";
        else if (value >= 1000) return $"₩{value:N0}";
        else if (value >= 100) return $"₩{value:N2}";
        else if (value >= 10) return $"₩{value:N3}";
        else return $"₩{value:N4}";
    }
}