using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class DemoTerminationManager : MonoBehaviour {
    [Header("UI References")]
    public GameObject terminationPanel;     // 종료 알림 패널
    public TextMeshProUGUI messageText;    // 메시지 텍스트
    public Button lobbyButton;             // 로비로 이동 버튼

    private bool isTerminated = false;
    private System.DateTime terminationDate = new System.DateTime(2020, 4, 2, 0, 0, 0);

    void Start() {
        if (terminationPanel != null) terminationPanel.SetActive(false);

        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(OnClickReturnToLobby);
    }

    void Update() {
        if (isTerminated) return;

        // CoinManager의 CurrentDateTime(DateTime 객체)을 직접 비교
        if (CoinManager.Instance != null) {
            if (CoinManager.Instance.CurrentDateTime >= terminationDate) {
                TriggerTermination();
            }
        }
    }

    private void TriggerTermination() {
        isTerminated = true;

        // 시간 즉시 정지
        Time.timeScale = 0f;

        // 패널 활성화 및 딱 지정된 텍스트만 출력
        if (terminationPanel != null) {
            terminationPanel.SetActive(true);

            if (messageText != null) {
                messageText.text = "데모 버전이 종료되었습니다! (벌써?)\n플레이 해주셔서 감사합니다!";
            }

            // DOTween 연출 (선택사항, 필요 없으면 지우셔도 됩니다)
            terminationPanel.transform.localScale = Vector3.one * 0.8f;
            terminationPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    private void OnClickReturnToLobby() {
        // 시간을 다시 흐르게 한 뒤 로비로 리스타트
        Time.timeScale = 1f;

        var lobbyManager = FindAnyObjectByType<LobbyManager>();
        if (lobbyManager != null) {
            lobbyManager.RestartGame();
        } else {
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }
    }
}