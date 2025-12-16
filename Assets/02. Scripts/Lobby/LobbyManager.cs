using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using static ChartRenderer;

public class LobbyManager : MonoBehaviour {
    [Header("Game Start Options")] public GameObject startOptionPanel;
    public Button newGameButton;
    public Button loadGameButton;
    public Toggle tutorialToggle; // 튜토리얼 실행 여부 선택

    [Header("Panels")] public GameObject lobbyPanel;
    public GameObject mainPanel;
    public GameObject gameStartPanel;
    public GameObject CharacterPanel;
    public StatusPanelController statusPanelController;

    [Header("UI Elements")] public TMP_InputField nameInput;
    public Button startButton;
    public Button gameStartButton;
    public Button leftArrow;
    public Button rightArrow;

    [Header("Character Selection")] public Image displayImage;
    public Sprite[] characterSprites;
    private int currentCharacterIndex = 0;

    [Header("Birthday Selection")] public TextMeshProUGUI monthText;
    public TextMeshProUGUI dayText;
    public Button monthLeftButton;
    public Button monthRightButton;
    public Button dayLeftButton;
    public Button dayRightButton;

    private int month = 1;
    private int day = 1;

    public BgmPlayer bgmPlayer;

    [Header("로딩")] public LoadingController loadingController;
    


    void Start() {
        Time.timeScale = 0f;

        // 로딩관련
        GameBootState.Reset();

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

        PlayerManager.Instance.playerName = playerName;
        PlayerManager.Instance.birthday = birthday;
        PlayerManager.Instance.characterIndex = currentCharacterIndex;

        GameBootState.playerReady = true;

        lobbyPanel.SetActive(false);
        mainPanel.SetActive(true);
        BgmPlayer.Instance.PlayIntroBgm();

        statusPanelController.SetPlayerInfo(playerName, currentCharacterIndex, birthday);

        WebMessageSender sender = FindAnyObjectByType<WebMessageSender>();
        if (sender != null) {
            sender.totalAsset = 2100000; // 초기 자산
            sender.SendPlayerDataToWeb(); // 게임 시작 시 Web으로 전송
        } else {
            Debug.LogWarning("[LobbyManager] WebMessageSender가 씬에 없습니다.");
        }


        // 튜토리얼 실행 여부 체크
        if (TutorialManager.Instance != null && tutorialToggle != null && tutorialToggle.isOn) {

            // 로딩 완료 선언
            GameBootState.playerReady = true;

            TutorialManager.Instance.StartTutorial();

            return;
        } else {
            Debug.Log("[LobbyManager] 튜토리얼 스킵됨");

            Time.timeScale = 1f;
            if (CoinManager.Instance != null)
                CoinManager.Instance.SetTimeSpeed(TimeSpeed.Normal);
        }

        GameBootState.saveLoaded = true;

        loadingController.BeginLoading();
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

    public void OnReceiveSaveData(string json) {
        GameData data = JsonUtility.FromJson<GameData>(json);
        LoadGameWithData(data);
    }

    public void LoadGameWithData(GameData data) {
        if (data == null) {
            Debug.LogWarning("저장된 데이터 없음!");
            return;
        }
        if (data != null) {
            Debug.Log("불러오기 성공!");

            // PlayerManager에 반영
            PlayerManager.Instance.playerName = data.playerName;
            PlayerManager.Instance.birthday = data.birthday;
            PlayerManager.Instance.characterIndex = data.characterIndex;

            PlayerManager.Instance.bullbitCash = data.bullbitCash;
            PlayerManager.Instance.satoshiBankCash = data.satoshiBankCash;
            PlayerManager.Instance.mangoCasinoCash = data.mangoCasinoCash;

            PlayerManager.Instance.holdings.Clear();
            foreach (var item in data.holdings) {
                PlayerManager.Instance.holdings[item.symbol] = item.value;
            }

            PlayerManager.Instance.totalBuyAmount.Clear();
            foreach (var item in data.totalBuyAmount) {
                PlayerManager.Instance.totalBuyAmount[item.symbol] = item.value;
            }

            PlayerManager.Instance.totalBuyQuantity.Clear();
            foreach (var item in data.totalBuyQuantity) {
                PlayerManager.Instance.totalBuyQuantity[item.symbol] = item.value;
            }

            // CoinManager
            CoinManager.Instance.survivalDays = data.survivalDays;

            PartTimeJobController.Instance.lastWorkedDay = data.lastPartTimeWorkDay;


            // 날짜 복원
            if (DateTime.TryParse(data.savedDateTime,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out DateTime parsed)) {
                CoinManager.Instance.SetDateTime(parsed);
            }

            // tickCount 복원
            CoinManager.Instance.SetTickCount(data.tickCount);

            CoinManager.Instance.coins.Clear();


            // --- Step 1) saved.isListed 를 MetaDatabase 에 먼저 반영 ---
            foreach (var saved in data.savedCoins) {
                var meta = Array.Find(CoinMetaDatabase.AllCoins, m => m.Symbol == saved.symbol);
                if (meta != null)
                    meta.BullbitListed = saved.isListed;
            }

            // --- Step 2) 반영된 MetaDatabase 를 기준으로 CoinData 리스트 재생성 ---
            CoinManager.Instance.coins.Clear();

            foreach (var meta in CoinMetaDatabase.AllCoins) {
                if (meta.BullbitListed) {
                    var coin = new CoinData(meta.Name, meta.Symbol, meta.InitialPrice, meta.MaxSupply);
                    CoinManager.Instance.coins.Add(coin);
                }
            }

            // --- Step 3) CoinData 에 저장된 세부 정보 덮어쓰기 ---
            foreach (var saved in data.savedCoins) {
                var coin = CoinManager.Instance.coins.Find(c => c.Symbol == saved.symbol);
                if (coin == null)
                    continue;

                coin.CurrentPrice = saved.currentPrice;
                coin.InitialPrice = saved.initialPrice;

                coin.PriceHistory = new List<double>(saved.priceHistory);
                coin.CandleHistory = new List<CandleData>(saved.candles);

                coin.CurrentPhaseOverride = saved.phaseOverride;
                coin.PhaseOverrideEndTime = new DateTime(saved.phaseOverrideEndTicks);
            }



            // UI에 표시
            nameInput.text = data.playerName;

            string[] temp = data.birthday.Replace("일", "").Split('월');
            month = int.Parse(temp[0]);
            day = int.Parse(temp[1]);

            UpdateBirthdayDisplay();

            currentCharacterIndex = data.characterIndex;
            ShowCharacter(currentCharacterIndex);

            // StatusPanel 업데이트
            statusPanelController.SetPlayerInfo(
                PlayerManager.Instance.playerName,
                PlayerManager.Instance.characterIndex,
                PlayerManager.Instance.birthday
            );

            // 게임 시작 공통 처리 (TimeScale 등)
            Time.timeScale = 1f;
            if (CoinManager.Instance != null)
                CoinManager.Instance.SetTimeSpeed(TimeSpeed.Normal);

            var ui = FindAnyObjectByType<MainUIManager>();
            if (ui != null) {
                ui.RefreshCoinRows();
            }

            RestoreXFeed(data);
            RestoreActiveEffects(data);

            // ------------------------------
            // MarketPhase 복원
            // ------------------------------
            if (!string.IsNullOrEmpty(data.savedMarketPhase)) {
                MarketPhase loadedPhase;

                if (Enum.TryParse(data.savedMarketPhase, out loadedPhase)) {
                    CoinManager.Instance.CurrentMarket = loadedPhase;
                    Debug.Log("[LOAD] 전체 MarketPhase 복원됨: " + loadedPhase);
                } else {
                    Debug.LogWarning("[LOAD] MarketPhase 파싱 실패: " + data.savedMarketPhase);
                }
            } else {
                Debug.Log("[LOAD] savedMarketPhase 없음. 기본 MarketPhase 유지");
            }

            // ==========================
            // 구독 정보 복원
            // ==========================
            var xn = XNotificationManager.Instance;

            xn.isSubscribed = data.isSubscribed;
            xn.isCancelRequested = data.isCancelRequested;

            if (!string.IsNullOrEmpty(data.nextBillingDate)) {
                if (DateTime.TryParse(data.nextBillingDate,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out DateTime parsedBilling)) {
                    xn.nextBillingDate = parsedBilling;
                    Debug.Log("[LOAD] nextBillingDate 복원됨: " + parsedBilling);
                } else {
                    Debug.LogWarning("[LOAD] nextBillingDate 파싱 실패: " + data.nextBillingDate);
                }
            } else {
                // nextBillingDate 없으면 기본값
                xn.nextBillingDate = CoinManager.Instance.CurrentDateTime.AddDays(30);
            }

            // 로비  메인 패널 전환
            lobbyPanel.SetActive(false);
            mainPanel.SetActive(true);
            BgmPlayer.Instance.PlayIntroBgm();

            // Load 관련
            GameBootState.saveLoaded = true;
            GameBootState.playerReady = true;
        } else {
            Debug.LogWarning("저장된 데이터 없음!");
        }
    }

    void OnLoadGameClicked() {
#if UNITY_WEBGL && !UNITY_EDITOR
    loadingController.BeginLoading();
    Application.ExternalCall("UnityToReact_RequestSaveData");
    return;
#else
        var data = SaveManager.Load();

        if (data == null) {
            Debug.LogWarning("저장된 데이터가 없어 로드할 수 없습니다.");
            return; 
        }

        loadingController.BeginLoading();
        LoadGameWithData(data);
#endif
    }


    private void RestoreXFeed(GameData data) {
        if (data == null || data.xFeedPosts == null)
            return;

        var spawner = XFeedSpawner.Instance;
        if (spawner == null)
            return;

        // XFeedSpawner가 저장된 포스트 전체를 UI로 복원
        spawner.LoadFeed(data.xFeedPosts);
    }

    private void RestoreActiveEffects(GameData data) {
        var orch = EventOrchestrator.Instance;
        var repo = FindAnyObjectByType<XPostRepository>();
        var now = CoinManager.Instance.CurrentDateTime;

        if (orch == null || repo == null) return;

        foreach (var eff in data.activeEffects) {
            if (!DateTime.TryParse(eff.startTime, null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out DateTime start))
                continue;

            if (!repo.TryGet(eff.authorId, eff.eventKey, out var xdata)) {
                Debug.LogWarning("[Restore] Repo에서 찾지 못함: " + eff.authorId + "/" + eff.eventKey);
                continue;
            }

            DateTime end = start.AddHours(xdata.durationHours);

            Debug.Log("[Restore] event=" + eff.eventKey +
                      " start=" + start +
                      " end=" + end +
                      " now=" + now);

            if (now < end) {
                orch.RestoreEffect(xdata, start);
                Debug.Log("[Restore] 효과 복원됨");
            } else {
                if (!string.IsNullOrEmpty(xdata.marketPhaseAfter)) {
                    CoinManager.Instance.CurrentMarket =
                        orch.ParsePhase(xdata.marketPhaseAfter);

                    Debug.Log("[Restore] 기간 지남 >> afterPhase 적용");
                }
            }
        }
    }
}