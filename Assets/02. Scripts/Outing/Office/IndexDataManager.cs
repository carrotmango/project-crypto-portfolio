using UnityEngine;
using System;
using System.Collections.Generic;

public class IndexDataManager : MonoBehaviour {
    public static IndexDataManager Instance;

    [Header("데이터 설정")]
    public int maxDays = 30;

    // 외부에 공개될 데이터 리스트
    public List<float> history30M = new List<float>();
    public List<float> history1D = new List<float>();

    // 데이터가 갱신되었음을 UI에 알리는 이벤트
    public event Action OnDataUpdated;

    private DateTime lastTickTime;
    private int lastRecordedDay = -1;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            // 씬이 넘어가도 데이터가 파괴되지 않도록 설정 (필요시)
            // DontDestroyOnLoad(gameObject); 
        }
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
        float currentIndex = CoinManager.Instance.CalculateBullbitIndex();
        bool isDataChanged = false; // 갱신 여부 체크

        // 1. [30M 데이터 로직]
        if (currentDt != lastTickTime) {
            AddDataToList(history30M, currentIndex);
            lastTickTime = currentDt;
            isDataChanged = true;
        } else {
            if (UpdateLastData(history30M, currentIndex)) isDataChanged = true;
        }

        // 2. [1D 데이터 로직]
        if (UpdateLastData(history1D, currentIndex)) isDataChanged = true;

        if (currentDt.Hour == 9 && currentDt.Minute == 0 && currentDt.Day != lastRecordedDay) {
            AddDataToList(history1D, currentIndex);
            lastRecordedDay = currentDt.Day;
            isDataChanged = true;
        }

        // 데이터가 변경되었다면 이벤트 발생 -> 켜져 있는 UI들이 알아서 다시 그림
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
            if (Mathf.Approximately(list[list.Count - 1], value)) return false; // 변동 없으면 갱신 안함
            list[list.Count - 1] = value;
            return true;
        } else {
            list.Add(value);
            return true;
        }
    }

    private void InitializeDummyData() {
        float startValue = CoinManager.Instance != null ? CoinManager.Instance.CalculateBullbitIndex() : 1000f;

        history30M.Clear();
        float temp30 = startValue;
        for (int i = 0; i < maxDays; i++) {
            history30M.Add(temp30);
            temp30 += UnityEngine.Random.Range(-2f, 2f);
        }
        history30M.Reverse();

        history1D.Clear();
        float temp1D = startValue;
        for (int i = 0; i < maxDays; i++) {
            history1D.Add(temp1D);
            temp1D += UnityEngine.Random.Range(-10f, 10f);
        }
        history1D.Reverse();
    }
}