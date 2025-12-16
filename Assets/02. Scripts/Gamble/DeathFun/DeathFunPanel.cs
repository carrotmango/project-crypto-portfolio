using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class DeathFunPanel : MonoBehaviour {
    [Header("UI 참조")]
    public GameObject deathfunPanel;
    public TextMeshProUGUI valueText;
    public Button takeProfitButton;

    [Header("외부 매니저")]
    public StageManager stageManager;
    public GambleManager gambleManager;

    [Header("사운드")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip failClip;

    private double baseBetAmount = 0;
    private double currentProfit = 0;
    private bool gameOver = false;
    private bool hasStarted = false;

    public void GambleStart() {
        deathfunPanel.SetActive(true);

        // 베팅 금액 설정
        if (gambleManager.bet1Button == gambleManager.selectedBetButton)
            baseBetAmount = 100000;
        else if (gambleManager.bet2Button == gambleManager.selectedBetButton)
            baseBetAmount = 1000000;
        else if (gambleManager.bet3Button == gambleManager.selectedBetButton)
            baseBetAmount = 10000000;
        else
            baseBetAmount = 0;

        currentProfit = 0; // 시작 시 수익은 0원
        hasStarted = false;
        gameOver = false;

        valueText.text = "0";
        takeProfitButton.GetComponentInChildren<TextMeshProUGUI>().text = "수익 확정";
        takeProfitButton.interactable = true;

        stageManager.RandomizeBombsOnly(); // 해골 재배치 + 셀 초기화
        ScrollToBottom();
    }

    // 층 통과 시 수익 계산 + 성공 사운드 재생
    public void OnPassFloor() {
        if (gameOver) return;

        hasStarted = true;

        int floorIndex = Mathf.Max(0, stageManager.CurrentFloor - 1); // 통과한 층
        double multiplier = stageManager.GetMultiplier(floorIndex);

        currentProfit = (double)Mathf.RoundToInt((float)(baseBetAmount * multiplier));
        valueText.text = $"{currentProfit:N0}";

        Debug.Log($"[수익 계산] {floorIndex + 1}층 통과 → {currentProfit}원");

        if (successClip != null && audioSource != null)
            audioSource.PlayOneShot(successClip);
    }

    // 실패 시 처리 + 실패 사운드 재생
    public void OnFail() {
        if (gameOver) return;

        gameOver = true;
        currentProfit = 0;

        valueText.text = "0";
        takeProfitButton.GetComponentInChildren<TextMeshProUGUI>().text = "게임 종료";
        takeProfitButton.interactable = true;

        if (failClip != null && audioSource != null)
            audioSource.PlayOneShot(failClip);

        stageManager.DisableAllCells(); // 셀 클릭 차단
    }

    // 수익 확정 버튼 클릭 시
    public void OnClickTakeProfit() {
        if (!hasStarted) {
            Debug.LogWarning("[DeathFun] 한 층도 통과하지 않음. 전액 손실 처리.");
            currentProfit = 0;
            valueText.text = "0";
        }

        if (gameOver) {
            CloseAndReset();
            return;
        }

        PlayerManager.Instance.satoshiBankCash += currentProfit;
        CoinManager.Instance.UpdateCashText();

        Debug.Log($"[DeathFun] 수익 확정: {currentProfit:N0}원 지급 완료");

        CloseAndReset();
    }

    // 게임 종료 및 초기화
    private void CloseAndReset() {
        baseBetAmount = 0;
        currentProfit = 0;
        gameOver = false;
        hasStarted = false;

        valueText.text = "0";

        deathfunPanel.SetActive(false);
        gambleManager.UpdateBetButtonStates();
    }

    // 스크롤 맨 아래로 이동
    private void ScrollToBottom() {
        ScrollRect scrollRect = stageManager.GetComponentInChildren<ScrollRect>();
        if (scrollRect != null) {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
