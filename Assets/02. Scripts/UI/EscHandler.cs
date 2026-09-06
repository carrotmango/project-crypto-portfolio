using UnityEngine;

public class EscHandler : MonoBehaviour {
    private float escDownTime;
    public float longPressThreshold = 0.25f; // 0.25초 이상이면 긴누름 처리

    public GameObject menuPanel;

    void Update() {
        // Key Down
        if (Input.GetKeyDown(KeyCode.Escape)) {
            escDownTime = Time.time;
        }

        // Key Up
        if (Input.GetKeyUp(KeyCode.Escape)) {
            float pressDuration = Time.time - escDownTime;

            if (pressDuration < longPressThreshold) {
                // 짧게 누름 = 메뉴 닫기
                if (menuPanel.activeSelf) {
                    menuPanel.SetActive(false);
                } else {
                    // 이미 닫힌 상태라면 다른 ESC 이벤트 처리 가능
                }
            } else {
                // 길게 누름 = 전체화면 해제 대응
#if UNITY_WEBGL
                // 이 경우는 브라우저가 강제로 풀스크린을 해제함
                // 여기서는 풀스크린 해제 후 UI 리셋 등을 넣으면 됨
                Debug.Log("ESC long press detected (WebGL)");
#else
                // PC 버전에서는 길게 누름을 다른 기능에 사용 가능
#endif
            }
        }
    }
}
