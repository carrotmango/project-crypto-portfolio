using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CoinFlip : MonoBehaviour {
    [Header("UI")]
    public GameObject coinFlipPanel;
    public Button frontButton;
    public Button backButton;
    public TextMeshProUGUI resultText;

    [Header("Animation / Sprite")]
    public Animator coinAnimator;    // CoinSpin 오브젝트
    public SpriteRenderer coinSprite; // CoinResult 오브젝트
    public Sprite frontSprite;
    public Sprite backSprite;

    [Header("외부 매니저")]
    public GambleManager gambleManager;

    [Header("사운드")]
    public AudioSource audioSource;
    public AudioClip coinFlipSound;

    private Button selectedBetButton;
    private string playerChoice = "";

    private void Awake() {
        frontButton.onClick.AddListener(() => OnChoose("앞"));
        backButton.onClick.AddListener(() => OnChoose("뒤"));
        resultText.text = "앞 또는 뒤를 선택하세요";
    }

    public void GambleStart() {
        selectedBetButton = gambleManager.selectedBetButton;

        // 초기 상태
        playerChoice = "";
        resultText.text = "앞 또는 뒤를 선택하세요";

        frontButton.interactable = true;
        backButton.interactable = true;

        // 처음엔 코인 완전 숨김
        coinAnimator.gameObject.SetActive(false);
        coinSprite.enabled = false;

        coinFlipPanel.SetActive(true);
    }

    void OnChoose(string choice) {
        playerChoice = choice;
        frontButton.interactable = false;
        backButton.interactable = false;

        // 유저 선택 순간 스핀 애니메이션 시작됨
        coinAnimator.gameObject.SetActive(true);
        coinAnimator.speed = 1f;
        coinAnimator.Play("CoinSpin", -1, 0f);

        StartCoroutine(RunFlip());
    }

    IEnumerator RunFlip() {

        if (audioSource != null && coinFlipSound != null)
            audioSource.PlayOneShot(coinFlipSound);

        resultText.text = "동전 던지는 중...";

        // 2.5 초간 스핀
        yield return new WaitForSeconds(2f);

        // 결과 결정
        string[] outcomes = { "앞", "뒤" };
        string result = outcomes[Random.Range(0, 2)];
        bool isWin = (playerChoice == result);

        //  스핀 끄기
        coinAnimator.speed = 0f;
        coinAnimator.gameObject.SetActive(false);

        //  결과 스프라이트 표시
        coinSprite.enabled = true;
        coinSprite.sprite = (result == "앞") ? frontSprite : backSprite;

        resultText.text = $"결과: {result}";

        yield return new WaitForSeconds(1f);

        // 보상 처리
        double baseAmount = 0;
        if (isWin) {
            if (selectedBetButton == gambleManager.bet1Button) baseAmount = 100000;
            else if (selectedBetButton == gambleManager.bet2Button) baseAmount = 1000000;
            else if (selectedBetButton == gambleManager.bet3Button) baseAmount = 10000000;

            double reward = baseAmount * 2;
            PlayerManager.Instance.satoshiBankCash += reward;
        }

        CoinManager.Instance.UpdateCashText();

        yield return new WaitForSeconds(1f);

        // 패널 닫기
        coinFlipPanel.SetActive(false);
        gambleManager.UpdateBetButtonStates();
    }
}
