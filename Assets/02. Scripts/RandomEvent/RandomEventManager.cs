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
    public DateTime gameStartDate = new DateTime(2016, 1, 1);


    void Awake() {
        if (Instance == null) {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        } else Destroy(gameObject);
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
        UIManager.Instance.ShowConfirm(data.description);
    }

    void LoadEvents() {
        TextAsset json = Resources.Load<TextAsset>("json/RandomEvents");

        if (json == null) {
            Debug.LogError("[RandomEventManager] RandomEvents.json not found in Resources");
            return;
        }

        RandomEventDataList list =
            JsonUtility.FromJson<RandomEventDataList>(json.text);

        if (list == null || list.events == null) {
            Debug.LogError("[RandomEventManager] Failed to parse RandomEvents.json");
            return;
        }

        events = new List<RandomEventData>(list.events);

        Debug.Log($"[RandomEventManager] Loaded {events.Count} events");
    }



    void ApplyEventEffect(RandomEventData data) {
        if (data.satoshiBankChange != 0)
            PlayerManager.Instance.ChangeSatoshiMoney(data.satoshiBankChange);

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
