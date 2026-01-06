using UnityEngine;
using UnityEngine.UI;

public class CandleView : MonoBehaviour {
    public RectTransform body;
    public RectTransform wick;
    public Image bodyImage;
    public Image wickImage;

    public float bodyWidth = 5f;
    public float wickWidth = 0.5f;

    public System.Func<double, float> PriceToY;

    private static readonly Color BearRed = new Color32(255, 76, 97, 255);
    private static readonly Color BearGreen = new Color32(50, 214, 149, 255);


    public void UpdateView(ChartRenderer.CandleData d) {
        if (PriceToY == null)
            return;

        Draw(d.open, d.close, d.high, d.low);
    }

    public void UpdateView(RuntimeCandle c) {
        if (PriceToY == null || c == null)
            return;

        Draw(c.Open, c.Close, c.High, c.Low);
    }

    void Draw(double open, double close, double high, double low) {
        float openY = PriceToY(open);
        float closeY = PriceToY(close);
        float highY = PriceToY(high);
        float lowY = PriceToY(low);

        float bodyHeight = Mathf.Max(1f, Mathf.Abs(closeY - openY));
        float bodyMidY = (openY + closeY) * 0.5f;

        body.sizeDelta = new Vector2(bodyWidth, bodyHeight);
        body.anchoredPosition = new Vector2(0f, bodyMidY);


        bool isBull = close >= open;
        Color color = isBull ? BearGreen : BearRed;

        bodyImage.color = color;
        wickImage.color = color;

        float bodyTop = Mathf.Max(openY, closeY);
        float bodyBottom = Mathf.Min(openY, closeY);

        float wickTop = Mathf.Max(bodyTop, highY);
        float wickBottom = Mathf.Min(bodyBottom, lowY);

        wick.sizeDelta = new Vector2(wickWidth, wickTop - wickBottom);
        wick.anchoredPosition = new Vector2(0f, (wickTop + wickBottom) * 0.5f);
    }
}
