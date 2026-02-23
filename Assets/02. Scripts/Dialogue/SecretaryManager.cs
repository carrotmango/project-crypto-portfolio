using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SecretaryManager : MonoBehaviour {
    public static SecretaryManager Instance;

    [Header("UI 연결")]
    public GameObject dialoguePanel;      // 비서 대화창 전체 패널
    public TextMeshProUGUI dialogueText;  // 대사가 출력될 텍스트
    public Button dialoguePanelButton;    // 패널 전체를 버튼으로 사용 (클릭 시 닫기용)

    [Header("설정")]
    public float typingSpeed = 0.03f;     // 글자 써지는 속도
    [SerializeField] private AudioClip typingClip; // 타이핑 소리 (선택사항)

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    // 원래의 게임 속도를 저장해두기 위한 변수
    private TimeSpeed savedSpeed;

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (dialoguePanelButton != null) {
            dialoguePanelButton.onClick.RemoveAllListeners();
            dialoguePanelButton.onClick.AddListener(OnPanelClick);
        }
    }

    // 외부에서 비서 버튼을 눌렀을 때 호출
    public void CallSecretary() {
        if (dialoguePanel == null) return;

        // ★ [핵심] 호출 시 시간 정지
        PauseGame();

        dialoguePanel.SetActive(true);
        ShowMessage("무슨 일로 호출하셨나요? (개발 중인 컨텐츠입니다.)");
    }

    private void ShowMessage(string message) {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(message));
    }

    private IEnumerator TypeText(string message) {
        dialogueText.text = "";
        isTyping = true;

        foreach (char c in message) {
            dialogueText.text += c;
            if (!char.IsWhiteSpace(c) && typingClip != null) {
                SfxPlayer.Instance?.Play(typingClip);
            }
            // Time.timeScale이 0이어도 돌아가게 WaitForSecondsRealtime 사용
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    public void OnPanelClick() {
        if (isTyping) {
            StopCoroutine(typingCoroutine);
            dialogueText.text = "무슨 일로 호출하셨나요? (개발 중인 컨텐츠입니다.)";
            isTyping = false;
        } else {
            dialoguePanel.SetActive(false);

            // ★ [핵심] 대화창 닫을 때 시간 재개
            ResumeGame();
        }
    }

    // --- 시간 제어 헬퍼 함수 ---
    private void PauseGame() {
        if (CoinManager.Instance != null) {
            // 현재 속도를 저장해두고 일시정지 (Normal이었는지 Double이었는지 기억)
            savedSpeed = CoinManager.Instance.currentSpeed;
            CoinManager.Instance.SetTimeSpeed(TimeSpeed.Paused);
        }
        // 유니티 시스템 시간 정지
        Time.timeScale = 0f;
    }

    private void ResumeGame() {
        if (CoinManager.Instance != null) {
            // 저장했던 원래 속도로 복구
            CoinManager.Instance.SetTimeSpeed(savedSpeed);
        }
        // 유니티 시스템 시간 재개
        Time.timeScale = 1f;
    }
}