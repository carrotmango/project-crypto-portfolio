using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FuturePositionUI : MonoBehaviour {
    [Header("UI Refs - Info")]
    public TextMeshProUGUI symbolText;      // 예: "BTCUSD"
    public TextMeshProUGUI leverageText;    // 예: "Long x5" (색상 적용)

    [Header("UI Refs - Data")]
    public TextMeshProUGUI qtyText;
    public TextMeshProUGUI valueText;
    public TextMeshProUGUI entryPriceText;
    public TextMeshProUGUI breakEvenText;
    public TextMeshProUGUI liqPriceText;
    public TextMeshProUGUI pnlText;
    public TextMeshProUGUI marginText;
    public Button closeBtn;

    private FutureChartRenderer.FuturePosition data;
    private FutureChartRenderer manager;

    public void Setup(FutureChartRenderer.FuturePosition posData, FutureChartRenderer renderer) {
        data = posData;
        manager = renderer;

        // 1. 심볼 텍스트 설정 (예: BTCUSD)
        if (symbolText != null) {
            symbolText.text = $"{data.Symbol}USD";
        }

        // 2. 포지션 & 레버리지 텍스트 설정 (예: Long x5)
        if (leverageText != null) {
            string sideColor = data.IsLong ? "#32D695" : "#E63C3C"; // 초록/빨강
            string sideText = data.IsLong ? "Long" : "Short";
            // RichText를 사용하여 색상 적용
            leverageText.text = $"<color={sideColor}>{sideText} x{data.Leverage:F0}</color>";
        }

        // 3. 종료 버튼 연결
        if (closeBtn != null) {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(() => manager.ClosePositionMarket(data, isLiquidated: false));
        }

        UpdateRealtime();
    }

    public void UpdateRealtime() {
        if (data == null || manager == null) return;

        double currentPriceUsd = manager.GetCurrentTargetPriceUSD(data.Symbol);

        // [수량 / 가치 / 마진 / 손익분기] -> 100k 단위 변환
        if (qtyText != null) qtyText.text = FormatValue(data.Quantity);

        if (valueText != null) {
            double currentValueUsd = data.Quantity * currentPriceUsd;
            valueText.text = FormatValue(currentValueUsd);
        }

        if (marginText != null) marginText.text = FormatValue(data.MarginUSD);
        if (breakEvenText != null) breakEvenText.text = FormatValue(data.EntryPriceUSD);

        // [진입가 / 청산가] -> 원문 달러 포맷 ($12,345.67)
        if (entryPriceText != null) entryPriceText.text = manager.FormatPriceUSD(data.EntryPriceUSD);
        if (liqPriceText != null) liqPriceText.text = manager.FormatPriceUSD(data.LiquidationPriceUSD);

        // [PNL] -> 색상 및 단위 변환
        if (pnlText != null) {
            double priceDiff = data.IsLong ? (currentPriceUsd - data.EntryPriceUSD) : (data.EntryPriceUSD - currentPriceUsd);
            double pnlAmount = priceDiff * data.Quantity;
            double pnlPct = (data.MarginUSD > 0) ? (pnlAmount / data.MarginUSD) * 100.0 : 0;

            string color = pnlAmount >= 0 ? "#32D695" : "#E63C3C";
            string sign = pnlAmount >= 0 ? "+" : "";

            // 예: +5.4k (+12.50%)
            pnlText.text = $"<color={color}>{sign}{FormatValue(pnlAmount)}\n({sign}{pnlPct:F2}%)</color>";
        }
    }

    // [수정] k, M, B 단위 변환 포맷터
    private string FormatValue(double val) {
        double absVal = System.Math.Abs(val);

        // 1. Billions (10억 이상) -> B
        if (absVal >= 1000000000.0) {
            return (val / 1000000000.0).ToString("N2") + "B";
        }
        // 2. Millions (100만 이상) -> M
        else if (absVal >= 1000000.0) {
            return (val / 1000000.0).ToString("N2") + "M";
        }
        // 3. Thousands (1,000 이상) -> k
        // (기존 100k 조건보다 1k부터 줄이는 게 UI상 훨씬 깔끔합니다)
        else if (absVal >= 1000.0) {
            return (val / 1000.0).ToString("N2") + "k";
        }

        // 그 외 (1,000 미만)
        return val.ToString("N2");
    }
}