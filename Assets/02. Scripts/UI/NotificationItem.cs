using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NotificationItem : MonoBehaviour {
    [Header("UI 연결")]
    public Image logoImage;
    public TextMeshProUGUI senderText;
    public TextMeshProUGUI messageText;
    public Image bgImage;       // [추가] 배경 색상 변경용 (Panel Image 연결)
    public Button clickButton;  // [추가] 클릭 처리를 위한 버튼 (전체 덮는 투명 버튼 or 배경 자체)

    private CanvasGroup cg;
    private System.Action onClickCallback; // 클릭 시 실행할 함수 저장소

    private void Awake() {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        // 버튼이 연결되어 있다면 리스너 등록
        if (clickButton != null) {
            clickButton.onClick.AddListener(OnClick);
        }
    }

    // [수정] 색상(bgColor)과 클릭행동(onClick)을 추가로 받음
    public void Setup(string sender, string message, Sprite icon, Color bgColor, System.Action onClick) {
        if (senderText != null) senderText.text = sender;
        if (messageText != null) messageText.text = message;
        if (logoImage != null && icon != null) logoImage.sprite = icon;

        // 배경색 적용
        if (bgImage != null) bgImage.color = bgColor;

        // 클릭 행동 저장
        this.onClickCallback = onClick;

        StartCoroutine(LifeCycleRoutine());
    }

    private void OnClick() {
        // 등록된 행동이 있으면 실행 (예: Xbird 앱 열기)
        onClickCallback?.Invoke();

        // 클릭하면 알림 즉시 삭제 (선택사항)
        Destroy(gameObject);
    }

    private IEnumerator LifeCycleRoutine() {
        // 등장
        float t = 0f;
        while (t < 0.2f) {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, t / 0.2f);
            yield return null;
        }
        cg.alpha = 1f;

        // 대기 (3초)
        yield return new WaitForSecondsRealtime(3.0f);

        // 퇴장
        t = 0f;
        while (t < 0.5f) {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
            yield return null;
        }

        Destroy(gameObject);
    }
}