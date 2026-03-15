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
    private CoinData coinData; // 클릭 이동을 위해 코인 데이터 저장

    public void Setup(FutureChartRenderer.FuturePosition posData, FutureChartRenderer renderer) {
        data = posData;
        manager = renderer;

        // 심볼을 바탕으로 코인 데이터를 찾아 저장해둡니다.
        coinData = CoinManager.Instance.coins.Find(c => c.Symbol == data.Symbol);

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

        //  4. 프리팹 클릭 이벤트 연결
        Button btn = GetComponent<Button>();
        if (btn != null) {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClickPosition);
        }

        UpdateRealtime();
    }

    // 프리팹 클릭 시 실행되는 함수
    private void OnClickPosition() {
        // manager와 coinData가 둘 다 있어야 차트를 이동시킬 수 있음
        if (manager != null && coinData != null) {
            manager.SelectCoin(coinData); // 부모 렌더러에게 차트 열기 지시
            Debug.Log($"[{data.Symbol}USD] 선물 차트로 이동합니다.");
        }
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
        if (breakEvenText != null) {
            // 수수료율 (하드코딩 혹은 manager에서 가져오기)
            double feeRate = FutureChartRenderer.Instance.TradingFeeRate;

            double bePrice = 0;

            if (data.IsLong) {
                // 롱: (1 + R) / (1 - R) 만큼 올라야 본전
                bePrice = data.EntryPriceUSD * (1.0 + feeRate) / (1.0 - feeRate);
            } else {
                // 숏: (1 - R) / (1 + R) 만큼 내려야 본전
                bePrice = data.EntryPriceUSD * (1.0 - feeRate) / (1.0 + feeRate);
            }

            breakEvenText.text = manager.FormatPriceUSD(bePrice);
        }

        // [진입가 / 청산가] -> 원문 달러 포맷 ($12,345.67)
        if (entryPriceText != null) entryPriceText.text = manager.FormatPriceUSD(data.EntryPriceUSD);
        if (liqPriceText != null) liqPriceText.text = manager.FormatPriceUSD(data.LiquidationPriceUSD);

        // ------------------------------------------------------------------
        // [수정] PNL 계산 시 수수료 차감 (Net PnL)
        // ------------------------------------------------------------------
        if (pnlText != null) {
            // 1. 차트상 순수 손익 (Gross)
            double priceDiff = data.IsLong
                ? (currentPriceUsd - data.EntryPriceUSD)
                : (data.EntryPriceUSD - currentPriceUsd);
            double grossPnL = priceDiff * data.Quantity;

            // 2. [핵심] 예상 종료 수수료 (0.036%)
            // 현재 포지션 가치 기준 (currentPriceUsd * Qty)
            double estimatedExitFee = (currentPriceUsd * data.Quantity) * 0.00036;

            // 3. 최종 보여줄 PnL (Net)
            double netPnL = grossPnL - estimatedExitFee;

            // 수익률 계산 (내 증거금 대비 찐 수익률)
            double pnlPct = (data.MarginUSD > 0) ? (netPnL / data.MarginUSD) * 100.0 : 0;

            string color = netPnL >= 0 ? "#32D695" : "#E63C3C";
            string sign = netPnL >= 0 ? "+" : "";

            // 예: +5.4k (+12.50%)
            pnlText.text = $"<color={color}>{sign}{FormatValue(netPnL)}\n({sign}{pnlPct:F2}%)</color>";
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