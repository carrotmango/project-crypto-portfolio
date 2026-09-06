using UnityEngine;
using System;
using System.Collections.Generic;

public class RandomEventManager : MonoBehaviour {
    public static RandomEventManager Instance;

    [Header("Event Settings")]
    public float eventChance = 0.10f;
    public int dailyCheckCount = 3;

    private bool eventTriggeredToday = false;
    private DateTime currentDay;

    private List<RandomEventData> events = new List<RandomEventData>();
    private List<int> checkHours = new List<int>();

    [Header("Game Date")]
    public DateTime gameStartDate = new DateTime(2020, 1, 1);

    void Awake() {
        // 싱글톤 꼬임 방지를 위해 씬 전환 시 강제 갱신
        Instance = this;
    }

    void Start() {
        currentDay = CoinManager.Instance.CurrentDateTime.Date;
        LoadEvents();
        ScheduleDailyChecks();
    }

    void Update() {
        // >> UI 패널 열려 있으면 이벤트 시스템 완전 정지
        if (UIPauseManager.IsPaused)
            return;

        DateTime gameDate = CoinManager.Instance.CurrentDateTime.Date;

        if (gameDate != currentDay) {
            currentDay = gameDate;
            OnNewDay();
        }

        if (IsFirstDay(currentDay))
            return;

        CheckEventTime();
    }

    bool IsFirstDay(DateTime today) {
        return today.Date == gameStartDate.Date;
    }

    void OnNewDay() {
        eventTriggeredToday = false;

        TriggerFixedDateEvents(currentDay);
        ScheduleDailyChecks();
    }

    void TriggerFixedDateEvents(DateTime today) {
        foreach (var data in events) {
            if (!data.isFixedDateEvent) continue;
            if (string.IsNullOrEmpty(data.eventDate)) continue;

            if (DateTime.TryParse(data.eventDate, out DateTime eventDay)) {
                if (eventDay.Date == today.Date) {
                    ApplyEventEffect(data);
                    ShowEventMessage(data);
                    eventTriggeredToday = true;
                }
            }
        }
    }

    void ScheduleDailyChecks() {
        checkHours.Clear();

        while (checkHours.Count < dailyCheckCount) {
            int hour = UnityEngine.Random.Range(9, 23); // 활동 시간
            if (!checkHours.Contains(hour))
                checkHours.Add(hour);
        }
    }

    void CheckEventTime() {
        if (eventTriggeredToday) return;

        int nowHour = CoinManager.Instance.CurrentDateTime.Hour;
        if (checkHours.Contains(nowHour)) {
            checkHours.Remove(nowHour);
            TryTriggerEvent();
        }
    }

    void TryTriggerEvent() {
        if (UnityEngine.Random.value <= eventChance) {
            TriggerRandomEvent();
            eventTriggeredToday = true;
        }
    }

    void TriggerRandomEvent() {
        if (events.Count == 0) return;

        RandomEventData data =
            events[UnityEngine.Random.Range(0, events.Count)];

        ApplyEventEffect(data);
        ShowEventMessage(data);
    }

    void ShowEventMessage(RandomEventData data) {
        if (string.IsNullOrEmpty(data.title)) {
            UIManager.Instance.ShowConfirm(data.description);
        } else {
            UIManager.Instance.ShowConfirm(data.title, data.description);
        }
    }

    // =======================================================
    // [수정됨] 다국어 지원 LoadEvents (public으로 변경)
    // =======================================================
    public void LoadEvents() {
        int savedLang = PlayerPrefs.GetInt("Saved_Language", 1);
        string suffix = (savedLang == 0) ? "_En" : ""; // 0이면 영어
        string fileName = $"json/RandomEvents{suffix}";

        TextAsset json = Resources.Load<TextAsset>(fileName);

        if (json == null) {
            Debug.LogError($"[RandomEventManager] {fileName} not found in Resources");
            return;
        }

        RandomEventDataList list =
            JsonUtility.FromJson<RandomEventDataList>(json.text);

        if (list == null || list.events == null) {
            Debug.LogError($"[RandomEventManager] Failed to parse {fileName}");
            return;
        }

        events = new List<RandomEventData>(list.events);
        Debug.Log($"[RandomEventManager] Loaded {events.Count} events from {fileName}");
    }

    void ApplyEventEffect(RandomEventData data) {
        if (data.satoshiBankChange != 0) {
            PlayerManager.Instance.ChangeSatoshiMoney(data.satoshiBankChange);

            // [수정됨] 거래 내역 로그도 언어에 따라 변경되게 처리
            int savedLang = PlayerPrefs.GetInt("Saved_Language", 1);
            string logDesc = (savedLang == 0) ? "Event" : "사건";
            string logType = data.satoshiBankChange >= 0
                ? ((savedLang == 0) ? "Deposit" : "입금")
                : ((savedLang == 0) ? "Withdrawal" : "출금");
            string logAsset = (savedLang == 0) ? "Cash" : "사토시 현금";

            TransactionManager.Instance.AddRecord(logDesc, Mathf.Abs((float)data.satoshiBankChange), logType, logAsset);
        }

        if (data.bullbitChange != 0)
            PlayerManager.Instance.ChangeBullbitCash(data.bullbitChange);

        // 코인 에어드랍 / 지급 처리
        if (data.coins != null) {
            foreach (var c in data.coins) {
                if (!CoinManager.Instance.HasCoinMeta(c.symbol)) {
                    Debug.LogWarning($"[RandomEvent] Undefined coin: {c.symbol}");
                    continue;
                }

                bool listed = CoinManager.Instance.IsCoinListedOnBullbit(c.symbol);

                if (!listed && !data.allowUnlistedAirdrop) {
                    Debug.Log($"[RandomEvent] {c.symbol} not listed. Skip.");
                    continue;
                }

                PlayerManager.Instance.ChangeCoin(c.symbol, c.amount);
            }
        }
    }
}