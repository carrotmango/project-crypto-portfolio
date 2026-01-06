using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler {
    private RectTransform targetRect;
    private Canvas canvas;

    void Awake() {
        targetRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData) {
        // 드래그 시작 시 맨 앞으로 가져오기 (가려짐 방지)
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData) {
        if (canvas == null) return;
        // 마우스 이동량만큼 패널 이동 (캔버스 스케일 고려)
        targetRect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
}