using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : MonoBehaviour {
    [Header("Game Start Options")]
    public GameObject startOptionPanel;
    public Button newGameButton;
    public Button loadGameButton;
    public Toggle tutorialToggle; // 튜토리얼 실행 여부 선택

    [Header("Panels")]
    public GameObject lobbyPanel;
    public GameObject mainPanel;
    public GameObject gameStartPanel;
    public GameObject CharacterPanel;
    public StatusPanelController statusPanelController;

    [Header("UI Elements")]
    public TMP_InputField nameInput;
    public Button startButton;
    public Button gameStartButton;
    public Button leftArrow;
    public Button rightArrow;

    [Header("Character Selection")]
    public Image displayImage;
    public Sprite[] characterSprites;
    private int currentCharacterIndex = 0;

    [Header("Birthday Selection")]
    public TextMeshProUGUI monthText;
    public TextMeshProUGUI dayText;
    public Button monthLeftButton;
    public Button monthRightButton;
    public Button dayLeftButton;
    public Button dayRightButton;

    private int month = 1;
    private int day = 1;

    public BgmPlayer bgmPlayer;

    void Start() {
        Time.timeScale = 0f;

        // 리스너 등록
        startButton.onClick.AddListener(OnStartClicked);
        gameStartButton.onClick.AddListener(GameStart);
        newGameButton.onClick.AddListener(OnNewGameClicked);
        loadGameButton.onClick.AddListener(OnLoadGameClicked);

        leftArrow.onClick.AddListener(PrevCharacter);
        rightArrow.onClick.AddListener(NextCharacter);

        monthLeftButton.onClick.AddListener(PrevMonth);
        monthRightButton.onClick.AddListener(NextMonth);
        dayLeftButton.onClick.AddListener(PrevDay);
        dayRightButton.onClick.AddListener(NextDay);

        ShowCharacter(currentCharacterIndex);
        UpdateBirthdayDisplay();
    }

    // -------------------------------
    // 캐릭터 선택
    // -------------------------------
    void ShowCharacter(int index) {
        if (characterSprites.Length == 0) return;
        displayImage.sprite = characterSprites[index];
    }

    void PrevCharacter() {
        currentCharacterIndex--;
        if (currentCharacterIndex < 0)
            currentCharacterIndex = characterSprites.Length - 1;
        ShowCharacter(currentCharacterIndex);
    }

    void NextCharacter() {
        currentCharacterIndex++;
        if (currentCharacterIndex >= characterSprites.Length)
            currentCharacterIndex = 0;
        ShowCharacter(currentCharacterIndex);
    }

    // -------------------------------
    // 생일 설정
    // -------------------------------
    void PrevMonth() {
        month--;
        if (month < 1) month = 12;
        ClampDayToMonth();
        UpdateBirthdayDisplay();
    }

    void NextMonth() {
        month++;
        if (month > 12) month = 1;
        ClampDayToMonth();
        UpdateBirthdayDisplay();
    }

    void PrevDay() {
        day--;
        if (day < 1) day = GetDaysInMonth(month);
        UpdateBirthdayDisplay();
    }

    void NextDay() {
        day++;
        if (day > GetDaysInMonth(month)) day = 1;
        UpdateBirthdayDisplay();
    }

    void UpdateBirthdayDisplay() {
        monthText.text = $"{month}월";
        dayText.text = $"{day}일";
    }

    void ClampDayToMonth() {
        int maxDay = GetDaysInMonth(month);
        if (day > maxDay) day = maxDay;
    }

    int GetDaysInMonth(int month) {
        switch (month) {
            case 2: return 28;
            case 4:
            case 6:
            case 9:
            case 11: return 30;
            default: return 31;
        }
    }

    // -------------------------------
    // 시작 버튼 동작
    // -------------------------------
    void OnStartClicked() {
        if (string.IsNullOrWhiteSpace(nameInput.text)) {
            Debug.LogWarning("이름을 입력해주세요.");
            return;
        }

        string playerName = nameInput.text;
        string birthday = $"{month}월 {day}일";

        Debug.Log($"플레이어 이름: {playerName}, 생일: {birthday}, 캐릭터 인덱스: {currentCharacterIndex}");

        lobbyPanel.SetActive(false);
        mainPanel.SetActive(true);

        statusPanelController.SetPlayerInfo(playerName, currentCharacterIndex, birthday);

        WebMessageSender sender = FindAnyObjectByType<WebMessageSender>();
        if (sender != null) {
            sender.playerName = playerName;
            sender.totalAsset = 2100000; // 초기 자산
            sender.SendPlayerDataToWeb(); // 게임 시작 시 Web으로 전송
        } else {
            Debug.LogWarning("[LobbyManager] WebMessageSender가 씬에 없습니다.");
        }


        // 튜토리얼 실행 여부 체크
        if (TutorialManager.Instance != null && tutorialToggle != null && tutorialToggle.isOn) {
            TutorialManager.Instance.StartTutorial();
        } else {
            Debug.Log("[LobbyManager] 튜토리얼 스킵됨");

            Time.timeScale = 1f;
            if (CoinManager.Instance != null)
                CoinManager.Instance.SetTimeSpeed(TimeSpeed.Normal);
        }

    }

    void GameStart() {
        gameStartButton.gameObject.SetActive(false);
        startOptionPanel.SetActive(true);
    }

    void OnNewGameClicked() {
        Debug.Log("새 게임 시작");
        SaveManager.DeleteSave();

        CharacterPanel.SetActive(true);
        gameStartPanel.SetActive(false);
        startOptionPanel.SetActive(false);

    }

    void OnLoadGameClicked() {
        Debug.Log("불러오기 시도 중...");
        GameData data = SaveManager.Load();
        if (data != null) {
            Debug.Log("게임 불러오기 성공!");
            nameInput.text = data.playerName;
            monthText.text = data.birthday.Split('월')[0] + "월";
            dayText.text = data.birthday.Split('월')[1];
            currentCharacterIndex = data.characterIndex;
            ShowCharacter(currentCharacterIndex);
        } else {
            Debug.LogWarning("저장된 데이터가 없습니다.");
        }
    }
}
