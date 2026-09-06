using UnityEngine;
using System;
using System.Collections.Generic;

public class ExchangeDataManager : MonoBehaviour {
    public static ExchangeDataManager Instance;

    [Header("데이터 설정")]
    public int maxDays = 30;

    // 외부에 공개될 환율 데이터 리스트
    public List<float> history30M = new List<float>();
    public List<float> history1D = new List<float>();

    // 데이터가 갱신되었음을 UI에 알리는 이벤트
    public event Action OnDataUpdated;

    private DateTime lastTickTime;
    private int lastRecordedDay = -1;

    void Awake() {
        if (Instance == null) Instance = this;
    }

    void Start() {
        InitializeDummyData();

        if (CoinManager.Instance != null) {
            lastTickTime = CoinManager.Instance.CurrentDateTime;
            lastRecordedDay = CoinManager.Instance.CurrentDateTime.Day;
        }
    }

    void Update() {
        if (CoinManager.Instance == null) return;

        DateTime currentDt = CoinManager.Instance.CurrentDateTime;
        float currentRate = (float)GlobalEconomyManager.UsdToKrw;
        bool isDataChanged = false;

        // [30M 로직]
        if (currentDt != lastTickTime) {
            AddDataToList(history30M, currentRate);
            lastTickTime = currentDt;
            isDataChanged = true;
        } else {
            if (UpdateLastData(history30M, currentRate)) isDataChanged = true;
        }

        // [1D 로직]
        if (UpdateLastData(history1D, currentRate)) isDataChanged = true;

        if (currentDt.Hour == 9 && currentDt.Minute == 0 && currentDt.Day != lastRecordedDay) {
            AddDataToList(history1D, currentRate);
            lastRecordedDay = currentDt.Day;
            isDataChanged = true;
        }

        // 데이터가 변경되었다면 차트 UI에 알림
        if (isDataChanged && OnDataUpdated != null) {
            OnDataUpdated.Invoke();
        }
    }

    private void AddDataToList(List<float> list, float value) {
        list.Add(value);
        if (list.Count > maxDays) list.RemoveAt(0);
    }

    private bool UpdateLastData(List<float> list, float value) {
        if (list.Count > 0) {
            if (Mathf.Approximately(list[list.Count - 1], value)) return false;
            list[list.Count - 1] = value;
            return true;
        } else {
            list.Add(value);
            return true;
        }
    }

    private void InitializeDummyData() {
        float currentRate = (float)GlobalEconomyManager.UsdToKrw;

        history30M.Clear();
        float temp30 = currentRate;
        for (int i = 0; i < maxDays; i++) {
            history30M.Add(temp30);
            temp30 += UnityEngine.Random.Range(-0.5f, 0.5f);
        }
        history30M.Reverse();

        history1D.Clear();
        float temp1D = currentRate;
        for (int i = 0; i < maxDays; i++) {
            history1D.Add(temp1D);
            temp1D += UnityEngine.Random.Range(-5f, 5f);
        }
        history1D.Reverse();
    }
}