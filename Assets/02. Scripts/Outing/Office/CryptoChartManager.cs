using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class CryptoChartManager : MonoBehaviour {
    [Header("UI 연결")]
    public RectTransform container;
    public GameObject linePrefab;
    public RectTransform yAxisContainer;
    public GameObject priceTextPrefab;

    [Header("차트 설정")]
    public int maxDays = 30;
    public Color chartColor = new Color32(20, 255, 8, 255);
    public int labelCount = 5;

    private List<GameObject> activeObjects = new List<GameObject>();
    private List<GameObject> activeLabels = new List<GameObject>();

    [Header("디자인 설정 (여백)")]
    public float verticalPadding = 30f;
    public float leftPadding = 80f;
    public float rightPadding = 20f;

    // ★ 이것이 유일한 차트 그리기 함수입니다. (SetChartData, AddPriceData 전부 삭제!)
    public void DrawChart(List<double> rawHistory) {
        // 1. 기존 차트 싹 지우기
        foreach (var obj in activeObjects) Destroy(obj);
        activeObjects.Clear();
        foreach (var label in activeLabels) Destroy(label);
        activeLabels.Clear();

        if (rawHistory == null || rawHistory.Count < 2) return;

        // 2. 최신 데이터 30개까지만 잘라내기
        List<float> displayData = new List<float>();
        int startIndex = Mathf.Max(0, rawHistory.Count - maxDays);
        for (int i = startIndex; i < rawHistory.Count; i++) {
            displayData.Add((float)rawHistory[i]);
        }

        if (displayData.Count < 2) return;

        // 3. Y축 최대/최소 계산
        float min = displayData.Min();
        float max = displayData.Max();
        float padding = (max - min) * 0.1f;
        if (padding == 0) padding = min * 0.01f;

        float currentMinPrice = min - padding;
        float currentMaxPrice = max + padding;

        float width = container.rect.width - leftPadding - rightPadding;
        float height = container.rect.height;
        float xStep = width / (maxDays - 1);

        // 4. 선 그리기
        for (int i = 0; i < displayData.Count - 1; i++) {
            Vector2 startPos = new Vector2(leftPadding + (i * xStep), GetNormalizedY(displayData[i], currentMinPrice, currentMaxPrice, height));
            Vector2 endPos = new Vector2(leftPadding + ((i + 1) * xStep), GetNormalizedY(displayData[i + 1], currentMinPrice, currentMaxPrice, height));
            CreateLineSegment(startPos, endPos);
        }

        // 5. 가격표 그리기
        DrawYAxisLabels(currentMinPrice, currentMaxPrice, height);
    }

    private float GetNormalizedY(float price, float min, float max, float chartHeight) {
        float range = max - min;
        if (Mathf.Approximately(range, 0f)) return chartHeight / 2f;
        float normalized = Mathf.Clamp((price - min) / range, 0f, 1f);
        return verticalPadding + (normalized * (chartHeight - (verticalPadding * 2)));
    }

    private void CreateLineSegment(Vector2 start, Vector2 end) {
        GameObject lineObj = Instantiate(linePrefab, container);
        activeObjects.Add(lineObj);
        Image img = lineObj.GetComponent<Image>();
        img.color = chartColor;

        RectTransform rt = lineObj.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0f, 0.5f);
        Vector2 direction = end - start;
        rt.anchoredPosition = start;
        rt.sizeDelta = new Vector2(direction.magnitude, 1.0f);
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private void DrawYAxisLabels(float min, float max, float chartHeight) {
        if (yAxisContainer == null || priceTextPrefab == null) return;
        float actualDrawingHeight = chartHeight - (verticalPadding * 2);
        float priceRange = max - min;
        float priceStep = priceRange / (labelCount - 1);

        for (int i = 0; i < labelCount; i++) {
            float targetPrice = min + (priceStep * i);
            float normalizedPos = (targetPrice - min) / priceRange;

            GameObject labelObj = Instantiate(priceTextPrefab, yAxisContainer);
            activeLabels.Add(labelObj);

            TextMeshProUGUI txt = labelObj.GetComponent<TextMeshProUGUI>();
            if (txt != null) {
                if (targetPrice < 10f) txt.text = targetPrice.ToString("N4");
                else if (targetPrice < 1000f) txt.text = targetPrice.ToString("N2");
                else txt.text = targetPrice.ToString("N0");
                txt.alignment = TextAlignmentOptions.Left;
            }

            RectTransform rt = labelObj.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, verticalPadding + (normalizedPos * actualDrawingHeight));
            rt.sizeDelta = new Vector2(leftPadding - 10f, 30f);
        }
    }
}