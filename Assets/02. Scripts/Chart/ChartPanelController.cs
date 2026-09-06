using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChartPanelController : MonoBehaviour {
    [Header("UI Components")]
    public GameObject chartPanel;
    public LiveChartRenderer liveChartRenderer;
    public TextMeshProUGUI chartTitleText;

    [Header("Trade Interaction")]
    public TradeOptionPanelController tradeOptionPanelController;

    // [변경] 매수/매도 버튼 대신 '퀵 오더' 버튼 하나로 통합
    public Button quickOrderButton;

    public CoinData currentCoin { get; private set; }

    public void ShowChartPanel(CoinData coin) {
        currentCoin = coin;
        chartPanel.SetActive(true);

        if (chartTitleText != null)
            chartTitleText.text = $"{coin.Name} ({coin.Symbol})";

        if (liveChartRenderer != null) {
            liveChartRenderer.Initialize(coin);
        }

        // 버튼 기능 연결
        SetupQuickOrderButton(coin.Symbol);
    }

    private void SetupQuickOrderButton(string symbol) {
        if (tradeOptionPanelController == null || quickOrderButton == null) return;

        quickOrderButton.onClick.RemoveAllListeners();
        quickOrderButton.onClick.AddListener(() => {
            // 버튼을 누르면 거래 옵션 패널을 띄움
            tradeOptionPanelController.ShowPanel(symbol);
        });
    }
    public void HideChartPanel() {
        chartPanel.SetActive(false);
    }
}