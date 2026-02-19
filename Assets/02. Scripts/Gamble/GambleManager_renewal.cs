using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;
using static UnityEditor.AddressableAssets.Build.Layout.BuildLayout;

public class GambleManager_renewal : MonoBehaviour {

    // 기본 UI
    public TextMeshProUGUI alert;
    public Button goButton;
    public GameObject outingPanel;

    // 베팅 UI
    public Slider betSlider;
    public TextMeshProUGUI betAmountText;
    public TextMeshProUGUI bankCashText;
    public TextMeshProUGUI payoutText;

    public TextMeshProUGUI limitText;

    // 외부 게임 매니저
    public GambleMonster gambleMonster;
    public CoinFlip coinFlip;
    public DeathFunPanel deathFun;

    // 게임 모드
    private int currentGameIndex = 0;

    private readonly string[] gameModes = {
        "GAMBLE MONSTER",
        "COIN FLIP",
        "DEATH FUN"
    };

    [Header("Game Panels")]
    public GameObject gambleMonsterPanel;
    public GameObject coinFlipPanel;
    public GameObject deathFunPanel;


    // 게임 라벨 스크롤
    public TextMeshProUGUI gameLabel;
    public RectTransform gameLabelRect;
    private Coroutine scrollCoroutine;

    // 게임 설명
    public TextMeshProUGUI FixedLabel;
    public TextMeshProUGUI gameDescription;

    // 스크롤 설정
    public float pixelStep = 2f;
    public float stepInterval = 0.02f;

    // 배팅금액
    public long currentBetAmount { get; private set; }

    private const int GAMBLE_MONSTER_RATE_NUM = 45;   // 분자
    private const int GAMBLE_MONSTER_RATE_DEN = 10000; // 분모

    [Header("Top UI Control")]
    public GameObject menuButton;
    public GameObject speedButtons;


    // 패널 열릴 때 초기화
    public void OpenPanel() {
        if (menuButton != null) menuButton.SetActive(true);
        if (speedButtons != null) speedButtons.SetActive(true);
        InitSlider();
        UpdateGameLabel();
    }

    // 슬라이더 초기화
    void InitSlider() {
        currentBetAmount = 0;

        betSlider.onValueChanged.RemoveAllListeners();

        betSlider.minValue = 0;
        betSlider.maxValue = 100;
        betSlider.wholeNumbers = true;
        betSlider.value = 0;

       betSlider.interactable = PlayerManager.Instance.satoshiBankCash > 0;

        betSlider.onValueChanged.AddListener(_ => UpdateBetTexts());
        UpdateBetTexts();
    }

    long GetBetAmount() {
        double bankCash = PlayerManager.Instance.satoshiBankCash;
        int percent = (int)betSlider.value;

        long calculatedBet = (long)Math.Floor(bankCash * percent / 100.0);

        // 직급별 한도 적용
        long limit = OfficeManager.Instance.GetGambleLimit();

        if (calculatedBet > limit) {
            return limit;
        }

        return calculatedBet;
    }

    // 베팅 관련 텍스트 갱신
    void UpdateBetTexts() {
        long betAmount = GetBetAmount();
        double bankCash = PlayerManager.Instance.satoshiBankCash;
        long limit = OfficeManager.Instance.GetGambleLimit();

        // 1. 배팅 금액 텍스트
        bool isLimited = (betAmount >= limit) && (bankCash > limit);
        if (isLimited) {
            betAmountText.text = $"배팅 금액: {betAmount:N0} (MAX)";
            betAmountText.color = Color.red; // 한도 도달 시 빨간색 강조
        } else {
            betAmountText.text = $"배팅 금액: {betAmount:N0}";
            betAmountText.color = Color.white;
        }

        // 2. 은행 잔고 텍스트
        bankCashText.text = $"은행 잔고: {bankCash:N0}";

        // 3. [추가] 배팅 한도 텍스트 갱신
        if (limitText != null) {
            // 예: "한도: 1,000만"
            limitText.text = $"최대 배팅 한도: {limit:N0}원";
        }

        UpdatePayoutText(betAmount);
    }

    // 배당률 표시
    void UpdatePayoutText(double betAmount) {
        if (payoutText == null) return;

        string mode = gameModes[currentGameIndex];

        if (mode == "GAMBLE MONSTER") {
            long unit = (long)(betAmount * GAMBLE_MONSTER_RATE_NUM / GAMBLE_MONSTER_RATE_DEN);
            payoutText.text = $"배당률: 1코인 = {unit:N0}원";
        } else if (mode == "COIN FLIP") {
            payoutText.text = "배당률: 성공 시 2배";
        } else if (mode == "DEATH FUN") {
            payoutText.text = "배당률: 성공한 타일당 Multiple";
        }
    }

    // 게임 라벨 및 스크롤 갱신
    void UpdateGameLabel() {
        if (gameLabel != null) {
            gameLabel.text = gameModes[currentGameIndex];
        }

        UpdateGameDescription();

        if (scrollCoroutine != null) {
            StopCoroutine(scrollCoroutine);
        }
        scrollCoroutine = StartCoroutine(ScrollGameLabelProcess());

        UpdateBetTexts();
    }

    IEnumerator ScrollGameLabelProcess() {
        yield return null;

        Canvas.ForceUpdateCanvases();
        gameLabel.ForceMeshUpdate(true);

        float textWidth = gameLabel.preferredWidth;
        float areaWidth = gameLabelRect.rect.width;

        if (textWidth <= 0) textWidth = 200f;
        if (areaWidth <= 0) areaWidth = 800f;

        float startX = areaWidth;
        float endX = -textWidth;

        RectTransform textRect = gameLabel.rectTransform;
        Vector2 pos = textRect.anchoredPosition;

        if (pos.x < endX || pos.x > startX) {
            pos.x = startX;
        }

        pos.y = 0f;
        textRect.anchoredPosition = pos;

        while (true) {
            pos.x -= pixelStep;

            if (pos.x <= endX) {
                pos.x = startX;
            }

            textRect.anchoredPosition = pos;
            yield return new WaitForSecondsRealtime(stepInterval);
        }
    }

    // 게임 설명
    void UpdateGameDescription() {
        if (gameDescription == null) return;

        string mode = gameModes[currentGameIndex];

        if (mode == "GAMBLE MONSTER") {
            FixedLabel.text = "Gamble Monster";
            gameDescription.text = "30초 동안 고블린을 최대한 많이 처치하여 금화를 모으세요.";
        } else if (mode == "COIN FLIP") {
            FixedLabel.text = "Coin Flip";
            gameDescription.text = "앞 또는 뒤를 골라 한방을 쟁취하세요.";
        } else if (mode == "DEATH FUN") {
            FixedLabel.text = "Death Fun";
            gameDescription.text = "폭탄 타일을 피해 정상까지 도달하세요.";
        }
    }

    // 게임 시작
    public void OnClickGo() {
        long betAmount = GetBetAmount();

        if (betAmount <= 0) {
            StartCoroutine(ShowAlert("배팅 금액을 설정하세요"));
            return;
        }

        if (PlayerManager.Instance.satoshiBankCash < betAmount) {
            StartCoroutine(ShowAlert("은행 잔고가 부족합니다"));
            return;
        }
        if (menuButton != null) menuButton.SetActive(false);
        if (speedButtons != null) speedButtons.SetActive(false);

        currentBetAmount = betAmount;
        PlayerManager.Instance.satoshiBankCash -= betAmount;
        PlayerManager.Instance.AddGambleSpend(betAmount);
        CoinManager.Instance.UpdateCashText();

        string mode = gameModes[currentGameIndex];
        if (mode == "GAMBLE MONSTER") gambleMonster.GambleStart();
        else if (mode == "COIN FLIP") coinFlip.GambleStart();
        else if (mode == "DEATH FUN") deathFun.GambleStart();

        TransactionManager.Instance.AddRecord("오락실", betAmount, "출금", "사토시 현금");
    }



    public void OnClickExit() {
        if (outingPanel != null) {
            outingPanel.SetActive(true);
        }

        gameObject.SetActive(false);
    }

    IEnumerator ShowAlert(string message) {
        alert.text = message;
        alert.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(3f);
        alert.gameObject.SetActive(false);
    }

    // 게임 종료 보상
    public void GambleFinish(long value) {
        string mode = gameModes[currentGameIndex];
        long reward = 0;

        if (mode == "GAMBLE MONSTER") {
            long unit = currentBetAmount * GAMBLE_MONSTER_RATE_NUM / GAMBLE_MONSTER_RATE_DEN;
            reward = value * unit;
        } else if (mode == "COIN FLIP") {
            // value = 성공 여부 (1 = 성공, 0 = 실패) 같은 구조면
            reward = value > 0 ? currentBetAmount * 2 : 0;
        } else if (mode == "DEATH FUN") {
            // value = profit (이미 계산된 최종 수익)
            reward = value;
        }

        if (reward > 0) {
            PlayerManager.Instance.satoshiBankCash += reward;
            TransactionManager.Instance.AddRecord("오락실", reward, "입금", "사토시 현금");
            QuestManager.Instance.ProcessAction(QuestType.ArcadeWinCash, reward);
            CoinManager.Instance.UpdateCashText();
            PlayerManager.Instance.AddGambleEarn(reward);
        }
        
        currentBetAmount = 0;
        InitSlider();
        UpdateGameLabel();

    }
    public void turnOnDim() {
        if (menuButton != null) menuButton.SetActive(true);
        if (speedButtons != null) speedButtons.SetActive(true);
    }

    // 게임 모드 이동
    public void OnClickGameNext() {
        currentGameIndex = (currentGameIndex + 1) % gameModes.Length;
        UpdateGameLabel();
        ShowPreviewPanel();
    }

    public void OnClickGamePrev() {
        currentGameIndex = (currentGameIndex - 1 + gameModes.Length) % gameModes.Length;
        UpdateGameLabel();
        ShowPreviewPanel();
    }


    public void OnReturnFromGame() {
        InitSlider();
        UpdateGameLabel();
        UpdateBetTexts();

        gameObject.SetActive(true);
    }

    public long GetGambleMonsterUnit(long betAmount) {
        return betAmount * GAMBLE_MONSTER_RATE_NUM / GAMBLE_MONSTER_RATE_DEN;
    }

    void ShowPreviewPanel() {
        HideAllGamePanels();

        string mode = gameModes[currentGameIndex];

        if (mode == "GAMBLE MONSTER") {
            gambleMonsterPanel.SetActive(true);
        } else if (mode == "COIN FLIP") {
            coinFlipPanel.SetActive(true);
        } else if (mode == "DEATH FUN") {
            deathFunPanel.SetActive(true);
        }
    }

    void HideAllGamePanels() {
        if (gambleMonsterPanel != null) gambleMonsterPanel.SetActive(false);
        if (coinFlipPanel != null) coinFlipPanel.SetActive(false);
        if (deathFunPanel != null) deathFunPanel.SetActive(false);
    }


}
