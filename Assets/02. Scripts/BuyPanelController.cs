using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class BuyPanelController : MonoBehaviour {
    [Header("UI 연결")]
    public GameObject panel;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI availableCashText;
    public TMP_InputField orderAmountInput;
    public TMP_InputField totalCostInput;
    public Button confirmButton;
    public Button resetButton;
    public TextMeshProUGUI feeText;
    public TextMeshProUGUI amountUnitLabel;
    public TextMeshProUGUI totalCostUnitLabel;
    private CoinData currentCoinData;

    [Header("외부 연결")]
    public LiveChartRenderer chartRenderer;

    [Header("설정")]
    [Range(0f, 0.05f)]
    public double feeRate = 0.0005; // 기본 수수료율 0.05%, 이벤트로 변경 가능

    private double price;
    private double availableCash;
    private bool isUpdating = false;
    private string lastSelectedSymbol = "";

    public void OpenPanel(CoinData coin) {
        if (coin.IsTradingSuspended) {
            UIManager.Instance.ShowConfirm("현재 거래가 정지된 종목입니다.");
            return; // 패널을 열지 않고 즉시 종료
        }
        currentCoinData = coin;
        price = coin.CurrentPrice;
        availableCash = PlayerManager.Instance.bullbitCash;
        lastSelectedSymbol = coin.Symbol;

        priceText.text = FormatKRW(price);
        availableCashText.text = FormatKRW(availableCash);
        feeText.text = $"{(feeRate * 100):F2}%";

        orderAmountInput.text = "";
        totalCostInput.text = "";

        amountUnitLabel.text = $"({coin.Symbol})";
        totalCostUnitLabel.text = "(KRW)";

        confirmButton.interactable = false;
        panel.SetActive(true);
        Time.timeScale = 0;
    }

    public void ClosePanel() {
        panel.SetActive(false);
        Time.timeScale = 1;
    }

    public void OnOrderAmountChanged(string input) {
        if (isUpdating) return;
        isUpdating = true;

        if (double.TryParse(input, out double amount)) {
            double baseCost = price * amount;
            double fee = baseCost * feeRate;
            double total = baseCost + fee;

            totalCostInput.text = FormatKRW(baseCost);
            confirmButton.interactable = total >= 5000 && total <= availableCash + 0.01;
        } else {
            totalCostInput.text = "";
            confirmButton.interactable = false;
        }

        isUpdating = false;
    }

    public void OnTotalCostChanged(string input) {
        if (isUpdating) return;
        isUpdating = true;

        string sanitized = input.Replace(",", "").Replace("₩", "");
        if (double.TryParse(sanitized, out double total)) {
            double amount = total / price;
            double fee = total * feeRate;
            orderAmountInput.text = amount.ToString("0.####");
            confirmButton.interactable = total + fee >= 5000 && total + fee <= availableCash + 0.01;
        } else {
            orderAmountInput.text = "";
            confirmButton.interactable = false;
        }

        isUpdating = false;
    }

    public void OnPercentButtonClicked(float percent) {
        // 예: percent = 10, 25, 50, 100
        double targetSpend = availableCash * (percent / 100.0);

        // 수수료 포함 계산 → baseCost + fee ≤ targetSpend
        double maxBase = targetSpend / (1.0 + feeRate);
        double amount = maxBase / price;

        double baseCost = price * amount;
        double fee = baseCost * feeRate;
        double totalCost = baseCost + fee;

        int tries = 20;
        while (totalCost > targetSpend + 0.0001 && tries-- > 0) {
            amount *= 0.999;
            baseCost = price * amount;
            fee = baseCost * feeRate;
            totalCost = baseCost + fee;
        }

        amount = Math.Floor(amount * 10000.0) / 10000.0;

        isUpdating = true;
        orderAmountInput.text = amount.ToString("0.####");
        totalCostInput.text = FormatKRW(baseCost);
        confirmButton.interactable = totalCost >= 5000 && totalCost <= availableCash + 0.01;
        isUpdating = false;
    }



    public void OnResetClicked() {
        orderAmountInput.text = "";
        totalCostInput.text = "";
        confirmButton.interactable = false;
    }

    public void OnConfirmClicked() {
        if (!double.TryParse(orderAmountInput.text, out double amount)) return;

        double baseCost = price * amount;
        double fee = baseCost * feeRate;
        double totalCost = baseCost + fee;

        // 자동 조정 루프
        int tries = 20;
        while (totalCost > PlayerManager.Instance.bullbitCash + 0.0001 && tries-- > 0) {
            amount *= 0.999;
            baseCost = price * amount;
            fee = baseCost * feeRate;
            totalCost = baseCost + fee;
        }

        amount = Math.Floor(amount * 10000.0) / 10000.0;
        baseCost = price * amount;
        fee = baseCost * feeRate;
        totalCost = baseCost + fee;

        if (totalCost < 5000) {
            Debug.LogWarning("최소 주문 금액은 5,000원 이상이어야 합니다.");
            return;
        }

        PlayerManager.Instance.bullbitCash -= totalCost;
        PlayerManager.Instance.RegisterBuy(lastSelectedSymbol, price, amount, fee);

        Debug.Log($"[매수 체결] {lastSelectedSymbol} {price} x {amount} = {FormatKRW(baseCost)} + 수수료 {FormatKRW(fee)} → 총 {FormatKRW(totalCost)} (잔액: {FormatKRW(PlayerManager.Instance.bullbitCash)})");

        // [수정] 클래스 이름(LiveChartRenderer) 대신 연결된 변수(chartRenderer)를 사용해야 합니다.
        if (currentCoinData != null) {
            currentCoinData.MarkTradeOnCurrentCandle(TradeType.SpotBuy);
        }

        ClosePanel();
    }

    void Start() {
        orderAmountInput.onValueChanged.AddListener(OnOrderAmountChanged);
        totalCostInput.onValueChanged.AddListener(OnTotalCostChanged);
        confirmButton.onClick.AddListener(OnConfirmClicked);
        resetButton.onClick.AddListener(OnResetClicked);
    }

    private string FormatKRW(double value) {
        if (value >= 1000) return $"₩{value:N0}";
        else if (value >= 100) return $"₩{value:N2}";
        else if (value >= 10) return $"₩{value:N3}";
        else return $"₩{value:N4}";
    }
}
