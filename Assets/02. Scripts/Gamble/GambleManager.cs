using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GambleManager : MonoBehaviour {
    public GameObject withdrawPanel;

    [Header("UI 연결")]
    public TMP_InputField withdrawInput;
    public Button withdrawButton;
    public Button allButton;
    public TextMeshProUGUI alert;
    public Button goButton;
    public TextMeshProUGUI odds;
    public TextMeshProUGUI gameLabel;

    [Header("외부 매니저")]
    public GambleMonster gambleMonster;
    public CoinFlip coinFlip;
    public DeathFunPanel deathFun;

    [Header("베팅 버튼")]
    public Button bet1Button;
    public Button bet2Button;
    public Button bet3Button;

    public Button selectedBetButton = null;

    private readonly Color normalColor = new Color32(255, 255, 255, 255);
    private readonly Color pressedColor = new Color32(0, 255, 0, 255);
    private readonly Color disabledColor = new Color32(100, 100, 100, 128);

    private int currentGameIndex = 0;
    private readonly string[] gameModes = {
        "GAMBLE MONSTER",
        "COIN FLIP",
        "DEATH FUN"
    };

    void Start() {
        withdrawInput.onValueChanged.AddListener(OnValueChanged);
        withdrawButton.onClick.AddListener(OnClickWithdraw);
        allButton.onClick.AddListener(OnClickAll);

        withdrawInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        withdrawInput.text = "";
        withdrawButton.interactable = false;

        odds.text = "";
        selectedBetButton = null;

        UpdateBetButtonStates();
        UpdateGameLabel();
    }

    public void OpenPanel() {
        withdrawPanel.SetActive(true);
        withdrawInput.text = "";
        withdrawButton.interactable = false;
        selectedBetButton = null;

        UpdateBetButtonStates();
        UpdateGameLabel();
    }

    public void ClosePanel() {
        withdrawPanel.SetActive(false);
    }

    void OnValueChanged(string input) {
        string raw = input.Replace(",", "");
        if (double.TryParse(raw, out double value)) {
            double currentPoint = PlayerManager.Instance.mangoCasinoCash;
            withdrawButton.interactable = value >= 10000 && value <= currentPoint;
        } else {
            withdrawButton.interactable = false;
        }
    }

    public void OnClickWithdraw() {
        double fee = 1000;
        string raw = withdrawInput.text.Replace(",", "");
        if (double.TryParse(raw, out double value)) {
            double currentPoint = PlayerManager.Instance.mangoCasinoCash;
            if (value < 10000 || value > currentPoint) return;

            PlayerManager.Instance.mangoCasinoCash -= value;
            PlayerManager.Instance.satoshiBankCash += value - fee;

            CoinManager.Instance.UpdateCashText();
            withdrawInput.text = "";
            withdrawButton.interactable = false;

            Debug.Log($"망고카지노 포인트 {value:N0}원 출금 완료 → 사토시은행");

            UpdateBetButtonStates();
            ClosePanel();
        }
    }

    public void OnClickAll() {
        double point = PlayerManager.Instance.mangoCasinoCash;
        if (point >= 10000) {
            withdrawInput.text = point.ToString("N0");
            withdrawButton.interactable = true;
        } else {
            withdrawInput.text = "";
            withdrawButton.interactable = false;
        }
    }

    public void UpdateBetButtonStates() {
        double cash = PlayerManager.Instance.mangoCasinoCash;

        SetBetButton(bet1Button, cash >= 100000);
        SetBetButton(bet2Button, cash >= 1000000);
        SetBetButton(bet3Button, cash >= 10000000);
    }

    void SetBetButton(Button button, bool isAvailable) {
        var image = button.GetComponent<Image>();

        if (!isAvailable) {
            button.interactable = false;
            image.color = disabledColor;
        } else {
            button.interactable = true;
            image.color = (selectedBetButton == button) ? pressedColor : normalColor;
        }
    }

    public void OnBet1Clicked() => SelectBet(bet1Button);
    public void OnBet2Clicked() => SelectBet(bet2Button);
    public void OnBet3Clicked() => SelectBet(bet3Button);

    void SelectBet(Button clickedButton) {
        if (!clickedButton.interactable) return;

        selectedBetButton = clickedButton;
        UpdateBetButtonStates();

        string mode = gameModes[currentGameIndex];

        if (odds != null && mode == "GAMBLE MONSTER") {
            if (selectedBetButton == bet1Button)
                odds.text = "배당률: 1코인 = 2,000원";
            else if (selectedBetButton == bet2Button)
                odds.text = "배당률: 1코인 = 25,000원";
            else if (selectedBetButton == bet3Button)
                odds.text = "배당률: 1코인 = 300,000원";
        } else {
            odds.text = "";
        }

        Debug.Log($"선택된 베팅 버튼: {clickedButton.name}");
    }

    public void OnClickGo() {
        if (selectedBetButton == null) {
            StartCoroutine(ShowAlert("배팅금을 선택하지 않았습니다"));
            return;
        }

        double betAmount = 0;

        if (selectedBetButton == bet1Button) betAmount = 100000;
        else if (selectedBetButton == bet2Button) betAmount = 1000000;
        else if (selectedBetButton == bet3Button) betAmount = 10000000;

        if (PlayerManager.Instance.mangoCasinoCash < betAmount) {
            StartCoroutine(ShowAlert("잔액이 부족합니다"));
            return;
        }

        PlayerManager.Instance.mangoCasinoCash -= betAmount;
        CoinManager.Instance.UpdateCashText();
        UpdateBetButtonStates();

        string mode = gameModes[currentGameIndex];

        if (mode == "GAMBLE MONSTER") {
            withdrawPanel.SetActive(false);
            gambleMonster.GambleStart();
        } else if (mode == "COIN FLIP") {
            withdrawPanel.SetActive(false);
            coinFlip.GambleStart();
        } else if (mode == "DEATH FUN") {
            withdrawPanel.SetActive(false);
            deathFun.GambleStart();
        }
    }


    IEnumerator ShowAlert(string message) {
        alert.text = message;
        alert.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        alert.gameObject.SetActive(false);
    }

    public void GambleFinish(int coinCount) {
        double unit = 0;

        if (selectedBetButton == bet1Button) unit = 2000;
        else if (selectedBetButton == bet2Button) unit = 25000;
        else if (selectedBetButton == bet3Button) unit = 300000;

        double reward = coinCount * unit;

        PlayerManager.Instance.mangoCasinoCash += reward;
        CoinManager.Instance.UpdateCashText();

        selectedBetButton = null;
        if (odds != null) odds.text = "";
        UpdateBetButtonStates();

        Debug.Log($"코인 {coinCount}개 × {unit:N0}원 → {reward:N0}원 환급 완료!");
    }

    public void OnClickGameNext() {
        currentGameIndex = (currentGameIndex + 1) % gameModes.Length;
        UpdateGameLabel();
    }

    public void OnClickGamePrev() {
        currentGameIndex = (currentGameIndex - 1 + gameModes.Length) % gameModes.Length;
        UpdateGameLabel();
    }

    void UpdateGameLabel() {
        if (gameLabel != null) {
            gameLabel.text = $"{gameModes[currentGameIndex]}";
        }

        if (selectedBetButton != null) {
            SelectBet(selectedBetButton);
        } else {
            odds.text = "";
        }
    }
    public void GambleFinish(int coinCount, Button betButton) {
        double unit = 0;

        if (betButton == bet1Button) unit = 2000;
        else if (betButton == bet2Button) unit = 25000;
        else if (betButton == bet3Button) unit = 300000;

        double reward = coinCount * unit;
        PlayerManager.Instance.mangoCasinoCash += reward;
        CoinManager.Instance.UpdateCashText();

        UpdateBetButtonStates();

        Debug.Log($"[보상 지급 완료] 코인 {coinCount}개 × {unit:N0}원 = {reward:N0}원");
    }

}
