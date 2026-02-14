using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;

public class ExchangeChartManager : MonoBehaviour {
    public static ExchangeChartManager Instance;

    [Header("UI 연결")]
    public RectTransform container;      // ChartContainer (Anchor: Bottom-Left)
    public GameObject linePrefab;        // 선을 그릴 프리팹 (UI Image 기반)
    public RectTransform yAxisContainer; // 가격표가 담길 부모 (Y_Axis_Labels)
    public GameObject priceTextPrefab;   // 가격표용 TextMeshPro 프리팹

    [Header("차트 설정")]
    public int maxDays = 30;
    public Color chartColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public int labelCount = 5;           // 표시할 가격표 개수

    [Header("텍스트 UI 연결")]
    public TextMeshProUGUI exchangeRateText;

    private List<float> priceHistory = new List<float>();
    private List<GameObject> activeObjects = new List<GameObject>();
    private List<GameObject> activeLabels = new List<GameObject>(); // 가격표 관리 리스트

    private float currentMinPrice;
    private float currentMaxPrice;

    [Header("디자인 설정")]
    public float verticalPadding = 35f;

    void Awake() {
        if (Instance == null) Instance = this;
    }

    void Start() {
        // 초기 데이터 생성 (기존 로직 유지)
        float lastRate = (float)GlobalEconomyManager.UsdToKrw;
        for (int i = 0; i < maxDays; i++) {
            float noise = UnityEngine.Random.Range(-0.5f, 0.5f);
            lastRate += noise;
            priceHistory.Add(lastRate);
        }
        DrawChart();
        UpdateExchangeRateUI(priceHistory.Last());
    }

    public void AddPriceData(float newRate) {
        priceHistory.Add(newRate);
        if (priceHistory.Count > maxDays) {
            priceHistory.RemoveAt(0);
        }

        DrawChart();
        UpdateExchangeRateUI(newRate);
    }

    public void DrawChart() {
        // 1. 기존 선 및 가격표 오브젝트 제거
        foreach (var obj in activeObjects) Destroy(obj);
        activeObjects.Clear();
        foreach (var label in activeLabels) Destroy(label);
        activeLabels.Clear();

        if (priceHistory.Count < 2) return;

        // 2. Y축 범위 자동 조절
        currentMinPrice = priceHistory.Min() - 5f;
        currentMaxPrice = priceHistory.Max() + 5f;

        float width = container.rect.width;
        float height = container.rect.height;
        float xStep = width / (maxDays - 1);

        // 3. 차트 선 그리기
        for (int i = 0; i < priceHistory.Count - 1; i++) {
            Vector2 startPos = new Vector2(i * xStep, GetNormalizedY(priceHistory[i], height));
            Vector2 endPos = new Vector2((i + 1) * xStep, GetNormalizedY(priceHistory[i + 1], height));

            CreateLineSegment(startPos, endPos);
        }

        // 4. 가격표 그리기 추가
        DrawYAxisLabels(height);
    }

    private float GetNormalizedY(float price, float chartHeight) {
        float range = currentMaxPrice - currentMinPrice;
        if (Mathf.Approximately(range, 0f)) return chartHeight / 2f;

        float normalized = (price - currentMinPrice) / range;
        normalized = Mathf.Clamp(normalized, 0f, 1f);

        // ★ 핵심: 전체 높이에서 위아래 여백을 뺀 '실제 그릴 높이'를 구함
        float actualDrawingHeight = chartHeight - (verticalPadding * 2);

        // (바닥 여백) + (비율 * 그릴 높이)
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
        rt.sizeDelta = new Vector2(distance, 1.0f); // 얇고 날카로운 선
        rt.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private void DrawYAxisLabels(float chartHeight) {
        if (yAxisContainer == null || priceTextPrefab == null) return;

        // [중요 수정] 
        // 기존: float axisHeight = yAxisContainer.rect.height; (제거)
        // 변경: 차트가 그려지는 높이(chartHeight)를 그대로 사용합니다.
        // 이렇게 해야 선과 글자가 1픽셀의 오차도 없이 똑같은 비례로 배치됩니다.
        float actualDrawingHeight = chartHeight - (verticalPadding * 2);

        float priceRange = currentMaxPrice - currentMinPrice;
        float priceStep = priceRange / (labelCount - 1);

        for (int i = 0; i < labelCount; i++) {
            float targetPrice = currentMinPrice + (priceStep * i);

            // 비율 계산
            float normalizedPos = (targetPrice - currentMinPrice) / priceRange;

            // 차트와 동일한 높이 공식 적용
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

    public void UpdateExchangeRateUI(float currentRate) {
        if (exchangeRateText != null) {
            exchangeRateText.text = $"{currentRate.ToString("N2")} 대한민국 원";
        }
    }
}