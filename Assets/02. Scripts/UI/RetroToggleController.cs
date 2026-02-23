using UnityEngine;
using UnityEngine.UI;

public class RetroToggleController : MonoBehaviour {
    [Header("UI Components")]
    public RectTransform handle;
    public Image background;

    [Header("Position Settings")]
    public float offX = -10f;
    public float onX = 10f;

    [Header("Color Settings")]
    public Color32 offColor = new Color32(70, 70, 70, 255); // 핵심 모드 (회색)
    public Color32 onColor = new Color32(0, 191, 255, 255); // 전체 모드 (하늘색)

    private Toggle toggle;

    // [추가] 외부에서 현재 알림 노출 여부를 확인할 수 있는 창구
    public bool IsCommunityVisible => toggle != null ? toggle.isOn : true;

    private void Awake() {
        toggle = GetComponent<Toggle>();
        if (toggle != null) {
            toggle.onValueChanged.AddListener(OnToggleChanged);
            OnToggleChanged(toggle.isOn);
        }
    }

    public void OnToggleChanged(bool isOn) {
        // 1. UI 연출 로직 (손잡이 위치, 배경색 변경)
        if (handle != null) {
            float targetX = isOn ? onX : offX;
            handle.anchoredPosition = new Vector2(targetX, handle.anchoredPosition.y);
        }
        if (background != null) {
            background.color = isOn ? onColor : offColor;
        }

        // 2. [핵심] NewsPanel 리프레시 호출
        // 인스펙터에서 newsPanel을 연결하거나, FindObjectOfType으로 찾습니다.
        var newsPanel = FindFirstObjectByType<NewsPanel>();
        if (newsPanel != null) {
            newsPanel.OnToggleRefresh();
        }

        Debug.Log($"[필터 변경] 커뮤니티 알림 및 노출 {(isOn ? "활성화" : "차단")}");
    }
}