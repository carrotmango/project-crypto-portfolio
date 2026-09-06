using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class TotalIndexChartManager : MonoBehaviour {
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
    public Color chartColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public int labelCount = 5;
    public float verticalPadding = 35f;

    [Header("텍스트 UI 연결")]
    public TextMeshProUGUI indexValueText;
    public TextMeshProUGUI indexChangeText;
    public enum ChartPeriod { M30, D1 }
    private ChartPeriod currentMode = ChartPeriod.M30;

    private List<GameObject> activeObjects = new List<GameObject>();
    private List<GameObject> activeLabels = new List<GameObject>();

    private float currentMinPrice;
    private float currentMaxPrice;

    private Color colorUp = new Color32(50, 214, 149, 255);
    private Color colorDown = new Color32(230, 60, 60, 255);
    private Color colorSame = Color.white;

    // ★ UI가 켜질 때 이벤트 구독 및 즉시 렌더링
    void OnEnable() {
        if (IndexDataManager.Instance != null) {
            IndexDataManager.Instance.OnDataUpdated += RefreshChart;
        }
        RefreshChart();
    }

    // ★ UI가 꺼질 때 이벤트 구독 해제 (메모리 누수 방지)
    void OnDisable() {
        if (IndexDataManager.Instance != null) {
            IndexDataManager.Instance.OnDataUpdated -= RefreshChart;
        }
    }

    void Start() {
        OnClick30M(); // 시작 시 30M 모드로 세팅
    }

    // Update()는 완전히 삭제합니다! 이벤트 방식으로만 동작합니다.

    public void OnClick30M() {
        currentMode = ChartPeriod.M30;
        UpdateButtonColors();
        RefreshChart();
    }

    public void OnClick1D() {
        currentMode = ChartPeriod.D1;
        UpdateButtonColors();
        RefreshChart();
    }

    private void UpdateButtonColors() {
        if (btnText30M != null) btnText30M.color = (currentMode == ChartPeriod.M30) ? activeColor : inactiveColor;
        if (btnText1D != null) btnText1D.color = (currentMode == ChartPeriod.D1) ? activeColor : inactiveColor;
    }

    // ★ 데이터를 가져와서 그리는 통합 함수
    private void RefreshChart() {
        if (IndexDataManager.Instance == null) return;

        List<float> targetData = (currentMode == ChartPeriod.M30)
            ? IndexDataManager.Instance.history30M
            : IndexDataManager.Instance.history1D;

        DrawChart(targetData);
        UpdateIndexUI(targetData);
    }

    // ==================================================================================
    // 아래 그리기 로직은 기존과 동일합니다.
    // ==================================================================================
    public void DrawChart(List<float> dataList) {
        foreach (var obj in activeObjects) Destroy(obj);
        activeObjects.Clear();
        foreach (var label in activeLabels) Destroy(label);
        activeLabels.Clear();

        if (dataList == null || dataList.Count < 2) return;

        currentMinPrice = dataList.Min() - 5f;
        currentMaxPrice = dataList.Max() + 5f;

        float width = container.rect.width;
        float height = container.rect.height;
        // 데이터 매니저에서 maxDays를 가져옵니다.
        int maxDays = IndexDataManager.Instance != null ? IndexDataManager.Instance.maxDays : 30;
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

    public void UpdateIndexUI(List<float> currentList) {
        if (currentList.Count == 0) return;

        float currentIndex = currentList.Last();
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

        if (indexValueText != null) {
            indexValueText.text = $"{currentIndex:N1}";
        }

        if (indexChangeText != null) {
            indexChangeText.text = $"{sign}{diffValue:N1} / {sign}{diffPercent:N2}%";
            indexChangeText.color = targetColor;
        }
    }
}