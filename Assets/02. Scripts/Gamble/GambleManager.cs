using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GambleManager : MonoBehaviour {

    [Header("UI 연결")]
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

        odds.text = "";
        selectedBetButton = null;

        UpdateBetButtonStates();
        UpdateGameLabel();
    }



    public void UpdateBetButtonStates() {
        double cash = PlayerManager.Instance.satoshiBankCash;

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

        if (PlayerManager.Instance.satoshiBankCash < betAmount) {
            StartCoroutine(ShowAlert("잔액이 부족합니다"));
            return;
        }

        PlayerManager.Instance.satoshiBankCash -= betAmount;
        CoinManager.Instance.UpdateCashText();
        UpdateBetButtonStates();

        string mode = gameModes[currentGameIndex];

        if (mode == "GAMBLE MONSTER") { 
            gambleMonster.GambleStart();
        } else if (mode == "COIN FLIP") {
            coinFlip.GambleStart();
        } else if (mode == "DEATH FUN") {
            deathFun.GambleStart();
        }
    }


    IEnumerator ShowAlert(string message) {
        alert.text = message;
        alert.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        alert.gameObject.SetActive(false);
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
        PlayerManager.Instance.satoshiBankCash += reward;
        CoinManager.Instance.UpdateCashText();

        UpdateBetButtonStates();

        Debug.Log($"[보상 지급 완료] 코인 {coinCount}개 × {unit:N0}원 = {reward:N0}원");
    }

}
