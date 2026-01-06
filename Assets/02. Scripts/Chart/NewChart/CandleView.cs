using UnityEngine;
using UnityEngine.UI;

public class CandleView : MonoBehaviour {
    public RectTransform body;
    public RectTransform wick;
    public Image bodyImage;
    public Image wickImage;

    // [삭제] 고정값 사용 안 함
    // public float bodyWidth = 5f; 
    // public float wickWidth = 0.5f;

    public System.Func<double, float> PriceToY;

    private static readonly Color BearRed = new Color32(255, 76, 97, 255);
    private static readonly Color BearGreen = new Color32(50, 214, 149, 255);

    // [수정] 너비(currentBodyWidth)를 인자로 받음
    public void UpdateView(RuntimeCandle c, float currentBodyWidth) {
        if (PriceToY == null || c == null)
            return;

        Draw(c.Open, c.Close, c.High, c.Low, currentBodyWidth);
    }

    // [수정] 그리기 로직
    void Draw(double open, double close, double high, double low, float width) {
        float openY = PriceToY(open);
        float closeY = PriceToY(close);
        float highY = PriceToY(high);
        float lowY = PriceToY(low);

        float bodyHeight = Mathf.Max(1f, Mathf.Abs(closeY - openY));
        float bodyMidY = (openY + closeY) * 0.5f;

        // 1. 몸통 너비 적용
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

        // 2. 꼬리(Wick) 너비 계산 (몸통의 10% ~ 20% 정도, 최소 1px)
        // 형님 요청대로 확대하면 꼬리도 두꺼워지게 설정
        float dynamicWickWidth = Mathf.Max(1f, width * 0.15f);

        wick.sizeDelta = new Vector2(dynamicWickWidth, wickTop - wickBottom);
        wick.anchoredPosition = new Vector2(0f, (wickTop + wickBottom) * 0.5f);
    }
}