using UnityEngine;
using UnityEngine.UI;
using System;

public class CandleView : MonoBehaviour {
    public RectTransform body;
    public RectTransform wick;
    public Image bodyImage;
    public Image wickImage;

    // [신규 1] 투명 판넬과 툴팁 스크립트를 연결할 변수
    public RectTransform hitBox;
    public CandleTooltip candleTooltip;

    public System.Func<double, float> PriceToY;

    public System.Func<double, string> FormatPriceFunc;

    private static readonly Color BearRed = new Color32(255, 76, 97, 255);
    private static readonly Color BearGreen = new Color32(50, 214, 149, 255);

    public void UpdateView(RuntimeCandle c, float currentBodyWidth, bool isDaily) {
        if (PriceToY == null || c == null) return;
        Draw(c.Open, c.Close, c.High, c.Low, currentBodyWidth);

        if (candleTooltip != null) {
            DateTime time = c.Timestamp;
            string dateStr = isDaily ? time.ToString("yyyy-MM-dd") : time.ToString("yyyy-MM-dd HH:mm");

            // 포맷 함수가 있으면 거쳐서 문자열로 만들고, 없으면 그냥 출력
            string highStr = FormatPriceFunc != null ? FormatPriceFunc(c.High) : c.High.ToString("N0");
            string lowStr = FormatPriceFunc != null ? FormatPriceFunc(c.Low) : c.Low.ToString("N0");

            candleTooltip.SetCandleData(dateStr, highStr, lowStr);
        }
    }

    void Draw(double open, double close, double high, double low, float width) {
        float openY = PriceToY(open);
        float closeY = PriceToY(close);
        float highY = PriceToY(high);
        float lowY = PriceToY(low);

        float bodyHeight = Mathf.Max(1f, Mathf.Abs(closeY - openY));
        float bodyMidY = (openY + closeY) * 0.5f;

        body.sizeDelta = new Vector2(width, bodyHeight);
        body.anchoredPosition = new Vector2(0f, bodyMidY);

        bool isBull = close >= open;
        Color color = isBull ? BearGreen : BearRed;

        bodyImage.color = color;
        wickImage.color = color;

        float bodyTop = Mathf.Max(openY, closeY);
        float bodyBottom = Mathf.Min(openY, closeY);

        float wickTop = Mathf.Max(bodyTop, highY);
        float wickBottom = Mathf.Min(bodyBottom, lowY);

        float dynamicWickWidth = Mathf.Max(1f, width * 0.15f);

        wick.sizeDelta = new Vector2(dynamicWickWidth, wickTop - wickBottom);
        wick.anchoredPosition = new Vector2(0f, (wickTop + wickBottom) * 0.5f);

        // 캔들이 그려질 때, 투명 판넬(HitBox)의 크기도 캔들 고가~저가 높이만큼 맞춰서 덮어버림!
        if (hitBox != null) {
            float totalHeight = Mathf.Max(15f, wickTop - wickBottom); // 너무 얇으면 클릭 안되니 최소 15px 보장
            hitBox.sizeDelta = new Vector2(width, totalHeight);
            hitBox.anchoredPosition = new Vector2(0f, (wickTop + wickBottom) * 0.5f);
        }
    }
}