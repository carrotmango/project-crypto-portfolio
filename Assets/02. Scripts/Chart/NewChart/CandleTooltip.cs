using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class CandleTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public GameObject tooltipPanel;
    public TextMeshProUGUI infoText;

    public Vector2 offset = new Vector2(20f, -20f);

    private string candleInfo = "";
    private bool isHovering = false;

    // [신규] 좌표 변환을 위해 필요한 변수들
    private RectTransform panelRect;
    private RectTransform parentRect;
    private Camera uiCamera;

    void Awake() {
        // 패널과 패널 부모의 RectTransform 가져오기
        if (tooltipPanel != null) {
            panelRect = tooltipPanel.GetComponent<RectTransform>();
            parentRect = tooltipPanel.transform.parent.GetComponent<RectTransform>();
        }

        // 현재 속해있는 캔버스를 찾아서 카메라 정보 가져오기 (Screen Space - Camera 대응)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) {
            uiCamera = canvas.worldCamera;
        }
    }

    public void SetCandleData(string dateStr, string highStr, string lowStr) {
        candleInfo = $"{dateStr}\nHigh: {highStr}\nLow: {lowStr}";
        if (isHovering && infoText != null) infoText.text = candleInfo;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (infoText != null) infoText.text = candleInfo;
        if (tooltipPanel != null) tooltipPanel.SetActive(true);
        isHovering = true;
        UpdateTooltipPosition();
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
        isHovering = false;
    }

    void Update() {
        if (isHovering) UpdateTooltipPosition();
    }

    private void UpdateTooltipPosition() {
        if (panelRect == null || parentRect == null) return;

        // [핵심 해결] 마우스 스크린 좌표를 캔버스(부모) 기준의 로컬 좌표로 완벽하게 변환!
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,             // 기준이 될 부모 UI
            Input.mousePosition,    // 현재 마우스 픽셀 좌표
            uiCamera,               // 캔버스를 비추는 카메라 (Overlay면 null이 들어감)
            out Vector2 localPoint  // 변환된 좌표가 여기로 나옴
        );

        // 변환된 좌표에 오프셋(간격) 더해서 위치 적용
        panelRect.anchoredPosition = localPoint + offset;
    }
}