using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CoinFlip : MonoBehaviour {

    // UI
    public GameObject coinFlipPanel;
    public Button frontButton;
    public Button backButton;
    public TextMeshProUGUI resultText;

    // 애니메이션 / 스프라이트
    public Animator coinAnimator;
    public SpriteRenderer coinSprite;
    public Sprite frontSprite;
    public Sprite backSprite;

    // 외부 매니저
    public GambleManager_renewal gambleManager;

    // 사운드
    public AudioSource audioSource;
    public AudioClip coinFlipSound;

    private string playerChoice = "";

    private void Awake() {
        frontButton.onClick.AddListener(() => OnChoose("앞"));
        backButton.onClick.AddListener(() => OnChoose("뒤"));
        resultText.text = "앞 또는 뒤를 선택하세요";
    }

    public void GambleStart() {
        playerChoice = "";

        resultText.text = "앞 또는 뒤를 선택하세요";
        frontButton.interactable = true;
        backButton.interactable = true;

        coinAnimator.gameObject.SetActive(false);
        coinSprite.enabled = false;

        coinFlipPanel.SetActive(true);
    }

    void OnChoose(string choice) {
        playerChoice = choice;

        frontButton.interactable = false;
        backButton.interactable = false;

        coinAnimator.gameObject.SetActive(true);
        coinAnimator.speed = 1f;
        coinAnimator.Play("CoinSpin", -1, 0f);

        StartCoroutine(RunFlip());
    }

    IEnumerator RunFlip() {

        if (audioSource != null && coinFlipSound != null) {
            // 클릭 소리 교체
            if (coinFlipSound != null) SfxPlayer.Instance.Play(coinFlipSound);
        }

        resultText.text = "동전 던지는 중...";
        yield return new WaitForSecondsRealtime(2f);

        string[] outcomes = { "앞", "뒤" };
        string result = outcomes[Random.Range(0, 2)];
        bool isWin = (playerChoice == result);

        // 결과 이미지 처리
        if (coinAnimator != null) {
            coinAnimator.speed = 0f;
            coinAnimator.gameObject.SetActive(false);
        }

        coinSprite.enabled = true;
        coinSprite.sprite = (result == "앞") ? frontSprite : backSprite;

        resultText.text = $"결과: {result}";
        yield return new WaitForSecondsRealtime(1f);

        // UI 표시용 계산 
        long displayReward = 0;
        if (isWin) {
            displayReward = gambleManager.currentBetAmount * 2;
            resultText.text = $"성공! +{displayReward:N0}원";
        } else {
            resultText.text = "실패...";
        }

        // 실제 정산 
        gambleManager.GambleFinish(isWin ? 1 : 0);

        yield return new WaitForSecondsRealtime(1f);

        coinFlipPanel.SetActive(false);
        gambleManager.OnReturnFromGame();
        gambleManager.turnOnDim();
    }



}
