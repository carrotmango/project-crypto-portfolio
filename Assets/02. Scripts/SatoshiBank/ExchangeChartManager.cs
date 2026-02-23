using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;

// ChartPeriod가 다른 스크립트에 있다면 주석 처리
// public enum ChartPeriod { M30, D1 } 

public class ExchangeChartManager : MonoBehaviour {
    public static ExchangeChartManager Instance;

    [Header("UI 연결")]
    public RectTransform container;
    public GameObject linePrefab;
    public RectTransform yAxisContainer;
    public GameObject priceTextPrefab;

    [Header("버튼 UI 연결")]
    public TextMeshProUGUI btnText30M;
    public TextMeshProUGUI btnText1D;
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.gray;

    [Header("차트 설정")]
    public int maxDays = 30;
    public Color chartColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public int labelCount = 5;

    [Header("텍스트 UI 연결")]
    public TextMeshProUGUI exchangeRateText; // (구버전 호환용)
    public TextMeshProUGUI mainRateText;
    public TextMeshProUGUI diffRateText;


    // 데이터 저장소
    private List<float> history30M = new List<float>();
    private List<float> history1D = new List<float>();

    // 현재 모드 (기본 30M)
    private ChartPeriod currentMode = ChartPeriod.M30;

    private DateTime lastTickTime;
    private int lastRecordedDay = -1;

    private List<GameObject> activeObjects = new List<GameObject>();
    private List<GameObject> activeLabels = new List<GameObject>();

    private float currentMinPrice;
    private float currentMaxPrice;

    [Header("디자인 설정")]
    public float verticalPadding = 35f;

    private Color colorUp = new Color32(50, 214, 149, 255);
    private Color colorDown = new Color32(230, 60, 60, 255);
    private Color colorSame = Color.white;

    void Awake() {
        if (Instance == null) Instance = this;
    }

    void OnEnable() {
        // 패널 켜질 때 자동 갱신
        DrawChart();
        UpdateExchangeRateUI();
    }

    void Start() {
        InitializeDummyData();

        if (CoinManager.Instance != null) {
            lastTickTime = CoinManager.Instance.CurrentDateTime;
            lastRecordedDay = CoinManager.Instance.CurrentDateTime.Day;
        }

        OnClick30M(); // 시작 시 30M 모드
    }

    void Update() {
        if (CoinManager.Instance == null) return;

        DateTime currentDt = CoinManager.Instance.CurrentDateTime;
        float currentRate = (float)GlobalEconomyManager.UsdToKrw;

        // [30M 로직]
        if (currentDt != lastTickTime) {
            AddDataToList(history30M, currentRate);
            lastTickTime = currentDt;
        } else {
            UpdateLastData(history30M, currentRate);
        }

        // [1D 로직]
        UpdateLastData(history1D, currentRate);

        if (currentDt.Hour == 9 && currentDt.Minute == 0 && currentDt.Day != lastRecordedDay) {
            AddDataToList(history1D, currentRate);
            lastRecordedDay = currentDt.Day;
        }

        // 화면 그리기 (매개변수 없이 호출)
        DrawChart();
        UpdateExchangeRateUI();
    }

    // ★★★ [에러 해결 1] CoinManager 호환용 함수 ★★★
    public void AddPriceData(float newRate) {
        // 실제 로직은 Update에서 돌므로 여긴 비워둠 (에러 방지용)
    }

    // ★★★ [에러 해결 2] SatoshiBankPanel 호환용 함수 (매개변수 없는 버전) ★★★
    // 이 함수가 없어서 SatoshiBankPanel에서 빨간 줄이 떴던 겁니다.
    public void DrawChart() {
        List<float> targetData = (currentMode == ChartPeriod.M30) ? history30M : history1D;
        DrawChart(targetData); // 내부적으로 진짜 그리는 함수 호출
    }

    public void UpdateExchangeRateUI() {
        List<float> targetData = (currentMode == ChartPeriod.M30) ? history30M : history1D;
        UpdateExchangeRateUI(targetData);
    }

    // --- 버튼 이벤트 ---
    public void OnClick30M() {
        currentMode = ChartPeriod.M30;
        UpdateButtonColors();
        DrawChart();
        UpdateExchangeRateUI();
    }

    public void OnClick1D() {
        currentMode = ChartPeriod.D1;
        UpdateButtonColors();
        DrawChart();
        UpdateExchangeRateUI();
    }

    private void UpdateButtonColors() {
        if (btnText30M != null) btnText30M.color = (currentMode == ChartPeriod.M30) ? activeColor : inactiveColor;
        if (btnText1D != null) btnText1D.color = (currentMode == ChartPeriod.D1) ? activeColor : inactiveColor;
    }

    private void AddDataToList(List<float> list, float value) {
        list.Add(value);
        if (list.Count > maxDays) list.RemoveAt(0);
    }

    private void UpdateLastData(List<float> list, float value) {
        if (list.Count > 0) list[list.Count - 1] = value;
        else list.Add(value);
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

    // ★★★ 실제 그리기 로직 (내부용) ★★★
    private void DrawChart(List<float> dataList) {
        foreach (var obj in activeObjects) Destroy(obj);
        activeObjects.Clear();
        foreach (var label in activeLabels) Destroy(label);
        activeLabels.Clear();

        if (dataList == null || dataList.Count < 2) return;

        currentMinPrice = dataList.Min() - 5f;
        currentMaxPrice = dataList.Max() + 5f;

        float width = container.rect.width;
        float height = container.rect.height;
        float xStep = width / (maxDays - 1);

        for (int i = 0; i < dataList.Count - 1; i++) {
            Vector2 startPos = new Vector2(i * xStep, GetNormalizedY(dataList[i], height));
            Vector2 endPos = new Vector2((i + 1) * xStep, GetNormalizedY(dataList[i + 1], height));
            CreateLineSegment(startPos, endPos);
        }

        DrawYAxisLabels(height);
    }

    private float GetNormalizedY(float price, float chartHeight) {
        float range = currentMaxPrice - currentMinPrice;
        if (Mathf.Approximately(range, 0f)) return chartHeight / 2f;
        float normalized = (price - currentMinPrice) / range;
        normalized = Mathf.Clamp(normalized, 0f, 1f);
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
        rt.sizeDelta = new Vector2(distance, 1.0f);
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
                txt.text = targetPrice.ToString("N0");
                txt.verticalAlignment = VerticalAlignmentOptions.Middle;
            }

            RectTransform rt = labelObj.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, yPos);
        }
    }

    // ★★★ 텍스트 갱신 로직 (내부용) ★★★
    private void UpdateExchangeRateUI(List<float> currentList) {
        if (currentList == null || currentList.Count == 0) return;

        float currentPrice = currentList.Last();
        float prevPrice = currentList.Count > 1 ? currentList[currentList.Count - 2] : currentPrice;
        float diffPrice = currentPrice - prevPrice;

        // 색상 결정 로직 (상승/하락)
        Color targetColor = colorSame;
        if (diffPrice > 0.001f) targetColor = colorUp;
        else if (diffPrice < -0.001f) targetColor = colorDown;

        string hexColor = ColorUtility.ToHtmlStringRGB(targetColor);

        // 1. exchangeRateText 변수에 "1,350.12 대한민국 원" 포맷으로 바로 주입
        if (exchangeRateText != null) {
            // N2: 천 단위 콤마 + 소수점 둘째 자리
            exchangeRateText.text = $"<color=#{hexColor}>{currentPrice:N2} 대한민국 원</color>";
        }

        // 2. mainRateText (기존 1USD = ... 형식 유지)
        if (mainRateText != null) {
            mainRateText.text = $"1USD\n=\n<color=#{hexColor}>{currentPrice:N2}KRW</color>";
        }

        // 3. diffRateText (등락폭 표시)
        if (diffRateText != null) {
            float diffPercent = (prevPrice != 0) ? (diffPrice / prevPrice) * 100f : 0f;
            string sign = diffPrice > 0 ? "+" : "";
            diffRateText.text = $"{sign}{diffPrice:N2} / {sign}{diffPercent:N2}%";
            diffRateText.color = targetColor;
        }
    }
}