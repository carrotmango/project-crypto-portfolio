using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class HorizontalLineView : MonoBehaviour, IPointerClickHandler {
    public double TargetPrice { get; private set; }
    private LiveChartRenderer parentRenderer;
    private RectTransform myRect;

    [Header("UI Refs")]
    public TextMeshProUGUI priceLabel;
    public Image lineImage;

    // [중요] 터치 영역 확장을 위한 투명 이미지 (선이 얇아서 클릭 안 될 때 대비)
    public Image hitAreaImage;

    public void Setup(LiveChartRenderer renderer, double price, Color color) {
        parentRenderer = renderer;
        TargetPrice = price;
        myRect = GetComponent<RectTransform>();

        if (lineImage != null) lineImage.color = color;
        if (priceLabel != null) priceLabel.text = price.ToString("N0");

        // [핵심] Overlay에 붙일 거니까, 가로로 꽉 채우기 (Stretch-Stretch 느낌)
        // AnchorMin X=0, AnchorMax X=1 -> 가로 꽉 채움
        // Y는 0.5 (중앙) 기준으로 위아래로 이동
        myRect.anchorMin = new Vector2(0f, 0.5f);
        myRect.anchorMax = new Vector2(1f, 0.5f);
        myRect.pivot = new Vector2(0.5f, 0.5f);

        // 좌우 여백 없이 0으로 설정
        myRect.offsetMin = new Vector2(0f, myRect.offsetMin.y);
        myRect.offsetMax = new Vector2(0f, myRect.offsetMax.y);

        // 높이는 2px (또는 설정된 높이 유지)
        myRect.sizeDelta = new Vector2(0f, 2f);
    }

    // [수정] 이제 Y좌표만 받으면 됩니다. 너비는 알아서 꽉 찹니다.
    public void UpdatePosition(float yPos) {
        // Overlay 기준 Y좌표 설정
        myRect.anchoredPosition = new Vector2(0f, yPos);

        // 화면 밖으로 나갔는지 체크해서 꺼주면 성능에 좋음 (선택 사항)
        // bool isVisible = (Mathf.Abs(yPos) < 1000f); // 예시
        // gameObject.SetActive(isVisible);
    }

    public void OnPointerClick(PointerEventData eventData) {
        // [디버깅] 클릭 로그
        Debug.Log($"수평선 클릭됨! 클릭 수: {eventData.clickCount}");

        if (eventData.clickCount >= 2) { // 2번 이상 클릭이면 삭제
            parentRenderer.RemoveHorizontalLine(this);
        }
    }
}