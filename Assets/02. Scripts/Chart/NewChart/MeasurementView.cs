using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MeasurementView : MonoBehaviour {
    [Header("Container")]
    public RectTransform boxRect;       // 박스 전체 영역 (Image)

    [Header("Internal Visuals")]
    public Image boxImage;              // 박스 배경색 (반투명)
    public RectTransform crossHairH;    // 박스 내 가로선
    public RectTransform crossHairV;    // 박스 내 세로선
    public Image[] crossHairImages;     // 십자선 색상 변경용

    [Header("Info Label")]
    public RectTransform infoRect;      // 텍스트 배경 박스
    public TextMeshProUGUI infoText;    // 정보 텍스트
    public Image infoBgImage;           // 텍스트 배경 이미지

    [Header("Settings")]
    public Color bullColor = new Color32(50, 214, 149, 100);
    public Color bearColor = new Color32(230, 60, 60, 100);
    public Color bullTextBg = new Color32(50, 214, 149, 255);
    public Color bearTextBg = new Color32(230, 60, 60, 255);

    public void Setup(Vector2 startLocalPos) {
        // [핵심 해결책] 이 도구(Root)의 앵커를 부모(ChartContent)와 동일하게 '왼쪽 중앙'으로 강제합니다.
        RectTransform myRect = GetComponent<RectTransform>();

        // 앵커: Middle Left (0, 0.5)
        myRect.anchorMin = new Vector2(0f, 0.5f);
        myRect.anchorMax = new Vector2(0f, 0.5f);
        myRect.pivot = new Vector2(0f, 0.5f);

        // 위치 초기화: 부모의 원점(왼쪽 끝)에 딱 붙임
        // 이제 자식인 boxRect의 좌표가 곧 차트의 좌표가 됩니다.
        myRect.anchoredPosition = Vector2.zero;
        myRect.sizeDelta = Vector2.zero; // 크기는 의미 없음

        // 박스 초기화
        boxRect.anchoredPosition = startLocalPos;
        boxRect.sizeDelta = Vector2.zero;
        // 박스 앵커는 중앙(0.5, 0.5)이어야 계산이 편함
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);

        // 십자선 초기화
        if (crossHairH != null) {
            crossHairH.anchorMin = new Vector2(0, 0.5f);
            crossHairH.anchorMax = new Vector2(1, 0.5f);
            crossHairH.offsetMin = Vector2.zero;
            crossHairH.offsetMax = Vector2.zero;
            crossHairH.sizeDelta = new Vector2(0, 1f);
        }
        if (crossHairV != null) {
            crossHairV.anchorMin = new Vector2(0.5f, 0);
            crossHairV.anchorMax = new Vector2(0.5f, 1);
            crossHairV.offsetMin = Vector2.zero;
            crossHairV.offsetMax = Vector2.zero;
            crossHairV.sizeDelta = new Vector2(1f, 0);
        }
    }

    public void UpdateView(Vector2 startPos, Vector2 endPos, double startPrice, double endPrice) {
        // 1. 박스 크기 및 위치 계산
        // startPos와 endPos는 이미 chartContent의 왼쪽 기준 로컬 좌표입니다.
        // boxRect는 Root(0,0)를 기준으로 움직이므로 좌표를 그대로 쓰면 됩니다.

        float minX = Mathf.Min(startPos.x, endPos.x);
        float maxX = Mathf.Max(startPos.x, endPos.x);
        float minY = Mathf.Min(startPos.y, endPos.y);
        float maxY = Mathf.Max(startPos.y, endPos.y);

        float width = Mathf.Abs(maxX - minX);
        float height = Mathf.Abs(maxY - minY);

        // 중심점 계산
        Vector2 centerPos = new Vector2(minX + width * 0.5f, minY + height * 0.5f);

        boxRect.sizeDelta = new Vector2(width, height);
        boxRect.anchoredPosition = centerPos;

        // 2. 데이터 계산
        double diff = endPrice - startPrice;
        double percent = (startPrice == 0) ? 0 : (diff / startPrice) * 100.0;
        bool isBull = percent >= 0;

        // 3. 색상
        Color boxCol = isBull ? bullColor : bearColor;
        Color textBgCol = isBull ? bullTextBg : bearTextBg;

        if (boxImage != null) boxImage.color = boxCol;
        if (infoBgImage != null) infoBgImage.color = textBgCol;

        foreach (var img in crossHairImages) {
            if (img != null) img.color = new Color(textBgCol.r, textBgCol.g, textBgCol.b, 0.5f);
        }

        // 4. 텍스트
        string sign = isBull ? "+" : "";
        string formattedDiff = FormatPrice(diff);
        infoText.text = $"{sign}{percent:F2}%\n({formattedDiff})";

        // 5. 텍스트 위치 (마우스 커서 따라가기)
        if (infoRect != null) {
            // 마우스 위치(endPos)에 띄우기
            // Root가 (0,0)에 있으므로 endPos를 그대로 쓰면 됨
            infoRect.anchoredPosition = endPos + new Vector2(0f, 35f);
        }
    }

    private string FormatPrice(double price) {
        double absPrice = System.Math.Abs(price);
        if (absPrice >= 1000) return price.ToString("N0");
        else if (absPrice >= 100) return price.ToString("N2");
        else if (absPrice >= 10) return price.ToString("N3");
        else return price.ToString("N4");
    }
}