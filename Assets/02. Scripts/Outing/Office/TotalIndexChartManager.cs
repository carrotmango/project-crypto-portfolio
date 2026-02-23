using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;

public enum ChartPeriod { M30, D1 } // 30분봉 vs 일봉

public class TotalIndexChartManager : MonoBehaviour {
    public static TotalIndexChartManager Instance;

    [Header("UI 연결")]
    public RectTransform container;      // ChartContainer (Anchor: Bottom-Left)
    public GameObject linePrefab;        // 선 프리팹
    public RectTransform yAxisContainer; // Y축 라벨 부모
    public GameObject priceTextPrefab;   // 라벨 텍스트 프리팹

    [Header("버튼 UI 연결 (색상 변경용)")]
    public TextMeshProUGUI btnText30M; // 30M 버튼 텍스트
    public TextMeshProUGUI btnText1D;  // 1D 버튼 텍스트
    public Color activeColor = Color.green; // 선택된 버튼 색
    public Color inactiveColor = Color.gray; // 선택 안 된 버튼 색

    [Header("차트 설정")]
    public int maxDays = 30; // 화면에 보여질 데이터 개수
    public Color chartColor = new Color(0.2f, 0.8f, 0.2f, 1f); // 환율 차트와 동일한 색
    public int labelCount = 5;

    [Header("텍스트 UI 연결")]
    public TextMeshProUGUI indexValueText;   // "1334.6"
    public TextMeshProUGUI indexChangeText;  // "+88.1 / +1.34%"

    // ★ 두 개의 데이터 저장소 (각각 관리)
    private List<float> history30M = new List<float>(); // 30분마다 저장
    private List<float> history1D = new List<float>();  // 매일 9시에 저장

    // 현재 보고 있는 모드
    private ChartPeriod currentMode = ChartPeriod.M30;

    // 내부 변수
    private DateTime lastTickTime;
    private int lastRecordedDay = -1;

    private List<GameObject> activeObjects = new List<GameObject>();
    private List<GameObject> activeLabels = new List<GameObject>();

    private float currentMinPrice;
    private float currentMaxPrice;

    [Header("디자인 설정 (환율 차트와 동일)")]
    public float verticalPadding = 35f;

    private Color colorUp = new Color32(50, 214, 149, 255);
    private Color colorDown = new Color32(230, 60, 60, 255);
    private Color colorSame = Color.white;

    void Awake() {
        if (Instance == null) Instance = this;
    }

    void Start() {
        // 1. 초기 더미 데이터 생성 (두 리스트 모두 채움)
        InitializeDummyData();

        // 2. 시간 초기화
        if (CoinManager.Instance != null) {
            lastTickTime = CoinManager.Instance.CurrentDateTime;
            lastRecordedDay = CoinManager.Instance.CurrentDateTime.Day;
        }

        // 3. 기본 모드(30M)로 시작 및 버튼 색상 초기화
        OnClick30M();
    }

    void Update() {
        if (CoinManager.Instance == null) return;

        DateTime currentDt = CoinManager.Instance.CurrentDateTime;
        float currentIndex = CoinManager.Instance.CalculateBullbitIndex();

        // =========================================================
        // 1. [30M 데이터 로직] 매 틱(30분)마다 저장
        // =========================================================
        if (currentDt != lastTickTime) {
            // 시간이 변했다 = 새로운 틱 -> 데이터 추가 (Shift)
            AddDataToList(history30M, currentIndex);
            lastTickTime = currentDt;
        } else {
            // 같은 틱 -> 마지막 값만 갱신 (Wiggle)
            UpdateLastData(history30M, currentIndex);
        }

        // =========================================================
        // 2. [1D 데이터 로직] 매일 아침 9시에 저장
        // =========================================================
        // 장중에는 오늘자 데이터를 계속 갱신 (Wiggle)
        UpdateLastData(history1D, currentIndex);

        // 9시 정각이 되고 날짜가 바뀌면 확정 저장 (Shift)
        if (currentDt.Hour == 9 && currentDt.Minute == 0 && currentDt.Day != lastRecordedDay) {
            // 어제 마감 데이터를 확정하고, 오늘 시가 데이터를 새로 추가하는 느낌
            AddDataToList(history1D, currentIndex);
            lastRecordedDay = currentDt.Day;
            Debug.Log($"[1D 차트] 일봉 마감! 데이터 추가됨. 지수: {currentIndex}");
        }

        // =========================================================
        // 3. 화면 그리기 (현재 모드에 맞는 리스트만 그림)
        // =========================================================
        List<float> currentData = (currentMode == ChartPeriod.M30) ? history30M : history1D;

        // 매 프레임 그려서 실시간 움직임 반영
        DrawChart(currentData);
        UpdateIndexUI(currentData);
    }

    // ★ 버튼 클릭 이벤트 (30M)
    public void OnClick30M() {
        currentMode = ChartPeriod.M30;
        UpdateButtonColors();
        // 즉시 다시 그리기
        DrawChart(history30M);
        UpdateIndexUI(history30M);
    }

    // ★ 버튼 클릭 이벤트 (1D)
    public void OnClick1D() {
        currentMode = ChartPeriod.D1;
        UpdateButtonColors();
        // 즉시 다시 그리기
        DrawChart(history1D);
        UpdateIndexUI(history1D);
    }

    private void UpdateButtonColors() {
        if (btnText30M != null) btnText30M.color = (currentMode == ChartPeriod.M30) ? activeColor : inactiveColor;
        if (btnText1D != null) btnText1D.color = (currentMode == ChartPeriod.D1) ? activeColor : inactiveColor;
    }

    // 리스트 관리 헬퍼 함수들
    private void AddDataToList(List<float> list, float value) {
        list.Add(value);
        if (list.Count > maxDays) { // 화면에 보여줄 개수만큼 유지 (오래된 것 삭제)
            list.RemoveAt(0);
        }
    }

    private void UpdateLastData(List<float> list, float value) {
        if (list.Count > 0) {
            list[list.Count - 1] = value;
        } else {
            list.Add(value);
        }
    }

    private void InitializeDummyData() {
        float startValue = CoinManager.Instance != null ? CoinManager.Instance.CalculateBullbitIndex() : 1000f;

        // 30M용 더미
        history30M.Clear();
        float temp30 = startValue;
        for (int i = 0; i < maxDays; i++) {
            history30M.Add(temp30);
            temp30 += UnityEngine.Random.Range(-2f, 2f);
        }
        history30M.Reverse();

        // 1D용 더미
        history1D.Clear();
        float temp1D = startValue;
        for (int i = 0; i < maxDays; i++) {
            history1D.Add(temp1D);
            temp1D += UnityEngine.Random.Range(-10f, 10f);
        }
        history1D.Reverse();
    }

    // ==================================================================================
    // ★★★ ExchangeChartManager와 100% 동일한 그리기 로직 (데이터만 매개변수로 받음) ★★★
    // ==================================================================================
    public void DrawChart(List<float> dataList) {
        // 1. 청소
        foreach (var obj in activeObjects) Destroy(obj);
        activeObjects.Clear();
        foreach (var label in activeLabels) Destroy(label);
        activeLabels.Clear();

        if (dataList == null || dataList.Count < 2) return;

        // 2. Y축 범위 자동 조절 (ExchangeChartManager와 똑같이 +/- 5f 여백 사용)
        currentMinPrice = dataList.Min() - 5f;
        currentMaxPrice = dataList.Max() + 5f;

        float width = container.rect.width;
        float height = container.rect.height;
        float xStep = width / (maxDays - 1);

        // 3. 차트 선 그리기
        for (int i = 0; i < dataList.Count - 1; i++) {
            Vector2 startPos = new Vector2(i * xStep, GetNormalizedY(dataList[i], height));
            Vector2 endPos = new Vector2((i + 1) * xStep, GetNormalizedY(dataList[i + 1], height));

            CreateLineSegment(startPos, endPos);
        }

        // 4. 가격표 그리기
        DrawYAxisLabels(height);
    }

    private float GetNormalizedY(float price, float chartHeight) {
        float range = currentMaxPrice - currentMinPrice;
        if (Mathf.Approximately(range, 0f)) return chartHeight / 2f;

        float normalized = (price - currentMinPrice) / range;
        normalized = Mathf.Clamp(normalized, 0f, 1f);

        // ★ 핵심: 전체 높이에서 위아래 여백을 뺀 '실제 그릴 높이' (환율 차트와 동일)
        float actualDrawingHeight = chartHeight - (verticalPadding * 2);

        return verticalPadding + (normalized * actualDrawingHeight);
    }

    private void CreateLineSegment(Vector2 start, Vector2 end) {
        GameObject lineObj = Instantiate(linePrefab, container);
        activeObjects.Add(lineObj);

        Image img = lineObj.GetComponent<Image>();
        img.color = chartColor;

        RectTransform rt = lineObj.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0f, 0.5f);

        Vector2 direction = end - start;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = start;
        rt.sizeDelta = new Vector2(distance, 1.0f); // 선 두께 1.0f (환율 차트와 동일)
        rt.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private void DrawYAxisLabels(float chartHeight) {
        if (yAxisContainer == null || priceTextPrefab == null) return;

        float actualDrawingHeight = chartHeight - (verticalPadding * 2);
        float priceRange = currentMaxPrice - currentMinPrice;
        float priceStep = priceRange / (labelCount - 1);

        for (int i = 0; i < labelCount; i++) {
            float targetPrice = currentMinPrice + (priceStep * i);
            float normalizedPos = (targetPrice - currentMinPrice) / priceRange;
            float yPos = verticalPadding + (normalizedPos * actualDrawingHeight);

            GameObject labelObj = Instantiate(priceTextPrefab, yAxisContainer);
            activeLabels.Add(labelObj);

            TextMeshProUGUI txt = labelObj.GetComponent<TextMeshProUGUI>();
            if (txt != null) {
                // 지수는 소수점 없이 혹은 1자리로 깔끔하게 (환율과 비슷하게 N0 사용)
                txt.text = targetPrice.ToString("N0");
                txt.verticalAlignment = VerticalAlignmentOptions.Middle;
            }

            RectTransform rt = labelObj.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, yPos);
        }
    }

    public void UpdateIndexUI(List<float> currentList) {
        if (currentList.Count == 0) return;

        float currentIndex = currentList.Last();
        // 비교 대상: 전 데이터 (30M이면 30분 전, 1D면 어제)
        float prevIndex = currentList.Count > 1 ? currentList[currentList.Count - 2] : currentIndex;

        float diffValue = currentIndex - prevIndex;
        float diffPercent = (prevIndex != 0) ? (diffValue / prevIndex) * 100f : 0f;

        Color targetColor = colorSame;
        string sign = "";

        if (diffValue > 0.001f) {
            targetColor = colorUp;
            sign = "+";
        } else if (diffValue < -0.001f) {
            targetColor = colorDown;
            sign = "";
        }

        string hexColor = ColorUtility.ToHtmlStringRGB(targetColor);

        if (indexValueText != null) {
            indexValueText.text = $"{currentIndex:N1}"; // N1 or N2
        }

        if (indexChangeText != null) {
            indexChangeText.text = $"{sign}{diffValue:N1} / {sign}{diffPercent:N2}%";
            indexChangeText.color = targetColor;
        }
    }
}