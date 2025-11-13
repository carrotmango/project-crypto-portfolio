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
    }

    public void GambleStart() {
        selectedBetButton = gambleManager.selectedBetButton;

        playerChoice = "";
        resultText.text = "";

        frontButton.interactable = true;
        backButton.interactable = true;

        coinFlipPanel.SetActive(true);
    }

    void OnChoose(string choice) {
        playerChoice = choice;
        frontButton.interactable = false;
        backButton.interactable = false;

        StartCoroutine(RunFlip());
    }

    IEnumerator RunFlip() {

        if (audioSource != null && coinFlipSound != null)
            audioSource.PlayOneShot(coinFlipSound);

        resultText.text = "동전 던지는 중...";


        yield return new WaitForSeconds(1f);

        string[] outcomes = { "앞", "뒤" };
        string result = outcomes[Random.Range(0, 2)];

        bool isWin = (playerChoice == result);
        resultText.text = $"결과: {result}";

        yield return new WaitForSeconds(1f);

        if (isWin) {
            double baseAmount = 0;
            if (selectedBetButton == gambleManager.bet1Button) baseAmount = 100000;
            else if (selectedBetButton == gambleManager.bet2Button) baseAmount = 1000000;
            else if (selectedBetButton == gambleManager.bet3Button) baseAmount = 10000000;

            double reward = baseAmount * 2;
            PlayerManager.Instance.mangoCasinoCash += reward;
            Debug.Log($"성공! +{reward:N0}원 지급");
        } else {
            Debug.Log("실패! 보상 없음");
        }

        CoinManager.Instance.UpdateCashText();
        yield return new WaitForSeconds(1f);

        // UI 정리 및 베팅 버튼 상태 갱신
        coinFlipPanel.SetActive(false);
        gambleManager.UpdateBetButtonStates();
    }
}
