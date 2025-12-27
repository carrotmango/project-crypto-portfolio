using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class DeathFunPanel : MonoBehaviour {

    [Header("UI")]
    public GameObject deathfunPanel;
    public TextMeshProUGUI valueText;
    public Button takeProfitButton;

    [Header("Manager")]
    public StageManager stageManager;
    public GambleManager_renewal gambleManager;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip failClip;

    private long baseBetAmount = 0;
    private long currentProfit = 0;
    private bool gameOver = false;
    private bool hasStarted = false;

    // 게임시작
    public void GambleStart() {
        deathfunPanel.SetActive(true);

        // 베팅 금액은 GambleManager에서만
        baseBetAmount = gambleManager.currentBetAmount;

        currentProfit = 0;
        hasStarted = false;
        gameOver = false;

        valueText.text = "0";
        takeProfitButton.interactable = true;

        stageManager.RandomizeBombsOnly();
        ScrollToBottom();
    }

    // 층 통과 
    public void OnPassFloor() {
        if (gameOver) return;

        hasStarted = true;

        int floorIndex = Mathf.Max(0, stageManager.CurrentFloor - 1);
        double multiplier = stageManager.GetMultiplier(floorIndex);

        currentProfit = (long)(baseBetAmount * multiplier);
        valueText.text = $"{currentProfit:N0}";

        if (audioSource && successClip)
            audioSource.PlayOneShot(successClip);
    }

    // 실패
    public void OnFail() {
        if (gameOver) return;

        gameOver = true;
        currentProfit = 0;

        valueText.text = "0";
        takeProfitButton.interactable = true;

        if (audioSource && failClip)
            audioSource.PlayOneShot(failClip);

        stageManager.DisableAllCells();
    }

    // 수익 확정
    public void OnClickTakeProfit() {
        if (!hasStarted) {
            currentProfit = 0;
        }

        gambleManager.GambleFinish(currentProfit);
        CloseAndReset();
    }

    // 종료
    private void CloseAndReset() {
        baseBetAmount = 0;
        currentProfit = 0;
        gameOver = false;
        hasStarted = false;

        valueText.text = "0";
        deathfunPanel.SetActive(false);
    }

    private void ScrollToBottom() {
        ScrollRect scrollRect = stageManager.GetComponentInChildren<ScrollRect>();
        if (scrollRect != null) {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
