using UnityEngine;
using TMPro;

public class ChartPanelController : MonoBehaviour {
    public GameObject chartPanel;
    public LiveChartRenderer liveChartRenderer;
    public TextMeshProUGUI chartTitleText;

    public CoinData currentCoin { get; private set; }

    public void ShowChartPanel(CoinData coin) {
        currentCoin = coin;
        chartPanel.SetActive(true);

        if (chartTitleText != null)
            chartTitleText.text = $"{coin.Name} ({coin.Symbol})";

        if (liveChartRenderer != null) {
            liveChartRenderer.Initialize(coin);
        } else {
            Debug.LogError("LiveChartRenderer가 연결되지 않았습니다.");
        }
    }

    public void HideChartPanel() {
        chartPanel.SetActive(false);
    }
}
