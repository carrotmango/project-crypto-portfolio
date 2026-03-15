using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class SpotPositionUI : MonoBehaviour {
    [Header("UI Refs - Info")]
    public TextMeshProUGUI nameText;           // 코인 한글/영문 이름 (예: 비트코인)
    public TextMeshProUGUI currencyText;       // 심볼/KRW (예: BTC/KRW)

    [Header("UI Refs - Data")]
    public TextMeshProUGUI qtyText;            // 보유 수량
    public TextMeshProUGUI buyAverageText;     // 매수 평균가
    public TextMeshProUGUI buyAmountText;      // 매수 금액 (원금)
    public TextMeshProUGUI evalAmountText;     // 평가 금액 (현재 가치)
    public TextMeshProUGUI profitText;         // 평가 손익 & 수익률

    private string symbol;
    private CoinData coinData;

    // [신규] 차트 렌더러 참조 변수
    private SpotChartRenderer parentRenderer;

    /// <summary>
    /// 포지션 UI 초기 세팅 (코인이 바뀔 때마다 호출)
    /// </summary>
    // [수정] Setup 함수가 SpotChartRenderer를 인자로 받도록 변경
    public void Setup(string targetSymbol, SpotChartRenderer renderer) {
        symbol = targetSymbol;
        parentRenderer = renderer;
        coinData = CoinManager.Instance.coins.Find(c => c.Symbol == symbol);

        if (coinData == null) {
            gameObject.SetActive(false); // 코인 데이터가 없으면 패널 숨김
            return;
        }

        // 이름 & 심볼 세팅
        var meta = Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);
        if (nameText != null) nameText.text = meta != null ? meta.Name : symbol;
        if (currencyText != null) currencyText.text = $"{symbol}/KRW";

        // [신규] 프리팹에 붙어있는 Button을 가져와서 클릭 이벤트 연결
        Button btn = GetComponent<Button>();
        if (btn != null) {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClickPosition);
        }

        UpdateRealtime();
    }

    // [신규] 프리팹을 클릭했을 때 실행될 함수
    private void OnClickPosition() {
        if (parentRenderer != null && coinData != null) {
            parentRenderer.SelectCoin(coinData);
            Debug.Log($"[{symbol}] 차트로 이동합니다.");
        }
    }

    /// <summary>
    /// 매 프레임 실시간 가격 연동 (차트 Update나 매니저에서 틱마다 호출해주면 됨)
    /// </summary>
    public void UpdateRealtime() {
        if (coinData == null || !PlayerManager.Instance.holdings.ContainsKey(symbol)) {
            // 해당 코인을 안 들고 있으면 전부 0으로 처리하거나 숨기기
            SetEmptyState();
            return;
        }

        double amount = PlayerManager.Instance.holdings[symbol];
        if (amount <= 0) {
            SetEmptyState();
            return;
        }

        // 1. 수량 & 평단가
        double avgPrice = PlayerManager.Instance.GetAvgPrice(symbol);
        if (qtyText != null) qtyText.text = amount.ToString("N4"); // 수량은 소수점 4자리까지
        if (buyAverageText != null) buyAverageText.text = FormatPriceKRW(avgPrice);

        // 2. 매수 금액 (원금)
        double buyTotal = avgPrice * amount;
        if (buyAmountText != null) buyAmountText.text = FormatPriceKRW(buyTotal);

        // 3. 평가 금액 (현재 시세 반영)
        double currentPrice = coinData.CurrentPrice;
        double evalTotal = currentPrice * amount;
        if (evalAmountText != null) evalAmountText.text = FormatPriceKRW(evalTotal);

        // 4. 평가 손익 & 수익률 계산
        double profitLoss = evalTotal - buyTotal;
        double returnRate = buyTotal > 0 ? (profitLoss / buyTotal) * 100.0 : 0;

        if (profitText != null) {
            // 양수면 초록색(+), 음수면 빨간색(-), 0이면 하얀색
            string colorHex = profitLoss > 0 ? "#32D695" : (profitLoss < 0 ? "#E63C3C" : "#FFFFFF");
            string sign = profitLoss > 0 ? "+" : "";

            // 예시: <color=#32D695>+1,500,000 ( +15.20% )</color>
            profitText.text = $"<color={colorHex}>{sign}{FormatPriceKRW(profitLoss) + " KRW"} ({sign}{returnRate:F2}%)</color>";
        }
    }

    // 보유 수량이 없을 때 보여줄 빈 텍스트 처리
    private void SetEmptyState() {
        if (qtyText != null) qtyText.text = "0.0000";
        if (buyAverageText != null) buyAverageText.text = "0";
        if (buyAmountText != null) buyAmountText.text = "0";
        if (evalAmountText != null) evalAmountText.text = "0";
        if (profitText != null) profitText.text = "<color=#808080>0 KRW(0.00%)</color>"; // 회색
    }

    // 원화 콤마 포맷터 (기존 불비트 방식 가져옴)
    private string FormatPriceKRW(double price) {
        if (price == 0) return "0";

        double absPrice = Math.Abs(price);
        string sign = price < 0 ? "-" : "";

        if (absPrice >= 1000)
            return sign + absPrice.ToString("N0");
        else if (absPrice >= 100)
            return sign + absPrice.ToString("N2");
        else if (absPrice >= 10)
            return sign + absPrice.ToString("N3");
        else
            return sign + absPrice.ToString("N4");
    }
}