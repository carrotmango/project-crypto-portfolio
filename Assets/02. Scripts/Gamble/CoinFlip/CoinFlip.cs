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
        // [핵심] 내부 로직용 데이터는 언어에 영향받지 않도록 영어 상수로 고정
        frontButton.onClick.AddListener(() => OnChoose("FRONT"));
        backButton.onClick.AddListener(() => OnChoose("BACK"));
    }

    public void GambleStart() {
        playerChoice = "";

        // [수정] 안내 문구 현지화
        resultText.text = LocalizationManager.GetText("MSG_COIN_CHOOSE");
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
            SfxPlayer.Instance.Play(coinFlipSound);
        }

        // [수정] 대기 문구 현지화
        resultText.text = LocalizationManager.GetText("MSG_COIN_FLIPPING");
        yield return new WaitForSecondsRealtime(2f);

        // 내부 로직은 영어 상수로 처리
        string[] outcomes = { "FRONT", "BACK" };
        string result = outcomes[Random.Range(0, 2)];
        bool isWin = (playerChoice == result);

        // 결과 이미지 처리
        if (coinAnimator != null) {
            coinAnimator.speed = 0f;
            coinAnimator.gameObject.SetActive(false);
        }

        coinSprite.enabled = true;
        coinSprite.sprite = (result == "FRONT") ? frontSprite : backSprite;

        // [수정] 결과 텍스트 현지화 (FRONT/BACK을 다시 앞/뒤, 혹은 Heads/Tails로 번역)
        string localizedResult = LocalizationManager.GetText(result == "FRONT" ? "LBL_COIN_FRONT" : "LBL_COIN_BACK");
        resultText.text = string.Format(LocalizationManager.GetText("MSG_COIN_RESULT"), localizedResult);
        yield return new WaitForSecondsRealtime(1f);

        // UI 표시용 계산 
        long displayReward = 0;
        if (isWin) {
            displayReward = gambleManager.currentBetAmount * 2;
            string unit = LocalizationManager.GetText("UNIT_CURRENCY");
            // [수정] 성공 텍스트 현지화
            resultText.text = string.Format(LocalizationManager.GetText("MSG_COIN_SUCCESS"), displayReward.ToString("N0"), unit);
        } else {
            // [수정] 실패 텍스트 현지화
            resultText.text = LocalizationManager.GetText("MSG_COIN_FAIL");
        }

        // 실제 정산 
        gambleManager.GambleFinish(isWin ? 1 : 0);

        yield return new WaitForSecondsRealtime(1f);

        coinFlipPanel.SetActive(false);
        gambleManager.OnReturnFromGame();
        gambleManager.turnOnDim();
    }
}