using UnityEngine;
using TMPro;

public class ChartPanelController : MonoBehaviour {
    public GameObject chartPanel;
    public ChartRenderer chartRenderer; // Inspector에서 연결
    public TextMeshProUGUI chartTitleText; // 차트 제목 텍스트 (옵션)

    public CoinData currentCoin { get; private set; }

    public void ShowChartPanel(CoinData coin) {
        currentCoin = coin;
        chartPanel.SetActive(true);

        // 타이틀 텍스트 (옵션)
        if (chartTitleText != null)
            chartTitleText.text = $"{coin.Name} ({coin.Symbol}) 차트";

        // 차트 렌더링
        if (chartRenderer != null) {
            chartRenderer.SetDataAndRender(coin, coin.CandleHistory);
        } else {
            Debug.LogError("ChartRenderer가 연결되지 않았습니다.");
        }
    }

    public void HideChartPanel() {
        chartPanel.SetActive(false);
    }
}