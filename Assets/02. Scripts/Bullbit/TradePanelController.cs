using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class TradePanelController : MonoBehaviour {
    [Header("UI 연결 - 상단 탭")]
    public GameObject panel;
    public Button buyTabButton;
    public Button sellTabButton;
    public GameObject stopPanel;

    [Header("UI 연결 - 정보 텍스트 (실시간 연동)")]
    public TextMeshProUGUI userAmountText;
    public TextMeshProUGUI priceText;

    [Header("UI 연결 - 입력창 & 라벨")]
    public TextMeshProUGUI orderAmountTitleText;
    public TMP_InputField inputField_OrderAmount;
    public TMP_InputField orderCashInput;

    [Header("UI 연결 - 하단 버튼")]
    public Button buyConfirmButton;
    public Button sellConfirmButton;
    public Button resetButton;

    [Header("설정")]
    [Range(0f, 0.05f)]
    public double feeRate = 0.0005;

    private CoinData currentCoinData;
    private bool isBuyMode = true;
    private string symbol = "";
    private double price;
    private double availableCash;
    private double ownedAmount;
    private bool isUpdating = false;

    //   [스마트 기능 핵심] 유저가 선택한 방식을 기억합니다  
    private enum InputAnchor { None, Amount, Cost, Percent }
    private InputAnchor currentAnchor = InputAnchor.None;
    private double lastSyncPrice = 0;
    private float selectedPercent = 0f; // 퍼센트 버튼을 눌렀을 때 그 값을 기억해둠

    private Color buyActiveColor = new Color32(36, 168, 115, 255);
    private Color sellActiveColor = new Color32(200, 100, 100, 255);
    private Color inactiveColor = new Color32(74, 74, 74, 255);

    void Awake() {
        if (inputField_OrderAmount != null) inputField_OrderAmount.onValueChanged.AddListener(OnOrderAmountChanged);
        if (orderCashInput != null) orderCashInput.onValueChanged.AddListener(OnTotalCostChanged);

        if (buyTabButton != null) buyTabButton.onClick.AddListener(() => SetTradeMode(true));
        if (sellTabButton != null) sellTabButton.onClick.AddListener(() => SetTradeMode(false));
        if (buyConfirmButton != null) buyConfirmButton.onClick.AddListener(ExecuteBuy);
        if (sellConfirmButton != null) sellConfirmButton.onClick.AddListener(ExecuteSell);
        if (resetButton != null) resetButton.onClick.AddListener(OnResetClicked);
    }

    void OnEnable() {
        SetTradeMode(true);
    }

void Update() {
        if (currentCoinData != null) {
            bool isSuspended = currentCoinData.IsTradingSuspended;
            if (stopPanel != null) {
                if (isSuspended && !stopPanel.activeSelf) stopPanel.SetActive(true);
                else if (!isSuspended && stopPanel.activeSelf) stopPanel.SetActive(false);
            }

            if (panel != null && panel.activeSelf) {
                price = currentCoinData.CurrentPrice;

                if (PlayerManager.Instance != null) {
                    availableCash = PlayerManager.Instance.bullbitCash;
                    ownedAmount = PlayerManager.Instance.GetHoldingAmount(symbol);
                }

                // [수정] 텍스트 현지화 적용
                if (isBuyMode) {
                    if (priceText != null) 
                        priceText.text = string.Format(LocalizationManager.GetText("LBL_TRADE_BUY_PRICE"), symbol, FormatCurrency(price));
                    if (userAmountText != null) 
                        userAmountText.text = string.Format(LocalizationManager.GetText("LBL_TRADE_AVAILABLE_CASH"), FormatCurrency(availableCash));
                } else {
                    if (priceText != null) 
                        priceText.text = string.Format(LocalizationManager.GetText("LBL_TRADE_SELL_PRICE"), symbol, FormatCurrency(price));
                    if (userAmountText != null) 
                        userAmountText.text = string.Format(LocalizationManager.GetText("LBL_TRADE_AVAILABLE_COIN"), ownedAmount.ToString("N4"), symbol);
                }

                // 가격 변동 시 자동 동기화 (정지 상태가 아닐 때만 작동)
                if (!isSuspended && Math.Abs(price - lastSyncPrice) > 0.000001 && !isUpdating) {
                    SyncSmartInputsWithRealtimePrice();
                    lastSyncPrice = price;
                }

                if (!isUpdating) UpdateConfirmButtonState();
            }
        }
    }

    private void SyncSmartInputsWithRealtimePrice() {
        if (currentAnchor == InputAnchor.None || price <= 0) return;

        isUpdating = true;

        try {
            if (currentAnchor == InputAnchor.Amount) {
                // 수량 고정
                if (double.TryParse(inputField_OrderAmount.text, out double amount) && amount > 0) {
                    double baseCost = price * amount;
                    if (orderCashInput != null) orderCashInput.text = Math.Floor(baseCost).ToString("F0");
                }
            } else if (currentAnchor == InputAnchor.Cost) {
                // 금액 고정
                string sanitized = orderCashInput.text.Replace(",", "").Replace("\u20A9", "").Replace(" ", "");
                if (double.TryParse(sanitized, out double total) && total > 0) {
                    double amount = total / price;
                    if (inputField_OrderAmount != null) inputField_OrderAmount.text = amount.ToString("0.####");
                }
            } else if (currentAnchor == InputAnchor.Percent) {
                //   [핵심 보완] 퍼센트 고정: 가격이 올라도 100%에 맞춰서 '수량'을 다시 조절해 줌!  
                CalculatePercent(selectedPercent);
            }
        } finally {
            isUpdating = false;
        }
    }

    public void OpenPanel(CoinData coin) {
        if (coin == null) return;
        currentCoinData = coin;
        symbol = coin.Symbol;
        if (stopPanel != null) {
            stopPanel.SetActive(coin.IsTradingSuspended);
        }

        OnResetClicked();
        SetTradeMode(true);
    }

    private void SetTradeMode(bool isBuy) {
        isBuyMode = isBuy;

        if (buyTabButton != null) buyTabButton.image.color = isBuyMode ? buyActiveColor : inactiveColor;
        if (sellTabButton != null) sellTabButton.image.color = !isBuyMode ? sellActiveColor : inactiveColor;

        if (buyConfirmButton != null) buyConfirmButton.gameObject.SetActive(isBuyMode);
        if (sellConfirmButton != null) sellConfirmButton.gameObject.SetActive(!isBuyMode);

        // [수정] 주문수량 / 매도수량 타이틀 현지화 적용
        if (orderAmountTitleText != null) {
            string titleKey = isBuyMode ? "LBL_TRADE_ORDER_QTY" : "LBL_TRADE_SELL_QTY";
            orderAmountTitleText.text = string.Format(LocalizationManager.GetText(titleKey), symbol);
        }

        OnResetClicked();
    }

    public void OnOrderAmountChanged(string input) {
        if (isUpdating || price <= 0) return;

        currentAnchor = InputAnchor.Amount;

        isUpdating = true;
        if (double.TryParse(input, out double amount) && amount > 0) {
            if (orderCashInput != null) orderCashInput.text = Math.Floor(price * amount).ToString("F0");
        } else {
            if (orderCashInput != null) orderCashInput.text = "";
        }
        isUpdating = false;
        UpdateConfirmButtonState();
    }

    public void OnTotalCostChanged(string input) {
        if (isUpdating || price <= 0) return;

        currentAnchor = InputAnchor.Cost;

        isUpdating = true;
        string sanitized = input.Replace(",", "").Replace("\u20A9", "").Replace(" ", "");
        if (double.TryParse(sanitized, out double total) && total > 0) {
            if (inputField_OrderAmount != null) inputField_OrderAmount.text = (total / price).ToString("0.####");
        } else {
            if (inputField_OrderAmount != null) inputField_OrderAmount.text = "";
        }
        isUpdating = false;
        UpdateConfirmButtonState();
    }

    // 인스펙터 버튼 연결용 (10, 25, 50, 100)
    public void OnPercentButtonClicked(float percent) {
        if (price <= 0) return;

        // 기준을 퍼센트로 잡고, 선택한 퍼센트를 저장해 둠
        currentAnchor = InputAnchor.Percent;
        selectedPercent = percent;

        isUpdating = true;
        CalculatePercent(percent);
        isUpdating = false;

        UpdateConfirmButtonState();
    }

    // 퍼센트 계산 로직을 함수로 분리해서 Update에서도 쓸 수 있게 만듦
    private void CalculatePercent(float percent) {
        if (isBuyMode) {
            double targetSpend = availableCash * (percent / 100.0);
            double maxBase = targetSpend / (1.0 + feeRate);
            double amount = maxBase / price;

            int tries = 20;
            while ((price * amount * (1.0 + feeRate)) > targetSpend + 0.01 && tries-- > 0) {
                amount *= 0.999;
            }

            amount = Math.Floor(amount * 10000.0) / 10000.0;
            double baseCost = price * amount;

            if (inputField_OrderAmount != null) inputField_OrderAmount.text = amount.ToString("0.####");
            if (orderCashInput != null) orderCashInput.text = Math.Floor(baseCost).ToString("F0");
        } else {
            double amount = ownedAmount * (percent / 100.0);
            double baseCost = price * amount;

            if (inputField_OrderAmount != null) inputField_OrderAmount.text = amount.ToString("0.####");
            if (orderCashInput != null) orderCashInput.text = Math.Floor(baseCost).ToString("F0");
        }
    }

    private void UpdateConfirmButtonState() {
        if (isUpdating) return;
        bool isValid = false;
        if (double.TryParse(inputField_OrderAmount?.text, out double amount) && amount > 0) {
            double baseCost = price * amount;
            if (isBuyMode) {
                double total = baseCost * (1.0 + feeRate);
                isValid = total >= 5000 && total <= availableCash + 0.1;
            } else {
                isValid = baseCost >= 5000 && amount <= ownedAmount + 0.00001;
            }
        }
        if (buyConfirmButton != null) buyConfirmButton.interactable = isValid;
        if (sellConfirmButton != null) sellConfirmButton.interactable = isValid;
    }

    public void OnResetClicked() {
        isUpdating = true;
        currentAnchor = InputAnchor.None;
        selectedPercent = 0f;

        if (inputField_OrderAmount != null) inputField_OrderAmount.text = "";
        if (orderCashInput != null) orderCashInput.text = "";
        if (buyConfirmButton != null) buyConfirmButton.interactable = false;
        if (sellConfirmButton != null) sellConfirmButton.interactable = false;

        isUpdating = false;
    }

    private void ExecuteBuy() {
        if (!double.TryParse(inputField_OrderAmount.text, out double amount)) return;
        double totalCost = (price * amount) * (1.0 + feeRate);
        if (totalCost > availableCash || (price * amount) < 5000) return;

        PlayerManager.Instance.bullbitCash -= totalCost;
        PlayerManager.Instance.RegisterBuy(symbol, price, amount, (price * amount) * feeRate);

        if (currentCoinData != null) currentCoinData.MarkTradeOnCurrentCandle(TradeType.SpotBuy);
        OnResetClicked();
    }

    private void ExecuteSell() {
        if (!double.TryParse(inputField_OrderAmount.text, out double amount)) return;
        if (amount > ownedAmount + 0.00001 || (price * amount) < 5000) return;

        double fee = (price * amount) * feeRate;
        double net = (price * amount) - fee;
        double sellQty = amount;

        if (PlayerManager.Instance.RegisterSell(symbol, price, ref sellQty, fee)) {
            PlayerManager.Instance.bullbitCash += net;
            if (currentCoinData != null) currentCoinData.MarkTradeOnCurrentCandle(TradeType.SpotSell);
            OnResetClicked();
        }
    }

    private string FormatCurrency(double value) {
        if (value < 0.0001) return "\u20A90";
        else if (value >= 1000) return $"\u20A9{value:N0}";
        else if (value >= 100) return $"\u20A9{value:N2}";
        else if (value >= 10) return $"\u20A9{value:N3}";
        else return $"\u20A9{value:N4}";
    }
}