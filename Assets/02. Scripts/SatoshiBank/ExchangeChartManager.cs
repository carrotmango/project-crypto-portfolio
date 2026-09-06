using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using static TotalIndexChartManager;

// ChartPeriod가 다른 스크립트에 있다면 주석 처리 (에러 났던 부분!)
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
    public Color chartColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public int labelCount = 5;

    [Header("텍스트 UI 연결")]
    public TextMeshProUGUI exchangeRateText; // (구버전 호환용)
    public TextMeshProUGUI mainRateText;
    public TextMeshProUGUI diffRateText;

    // 현재 모드 (기본 30M)
    private ChartPeriod currentMode = ChartPeriod.M30;

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

    // ★ UI 켜질 때 이벤트 구독 및 그리기
    void OnEnable() {
        if (ExchangeDataManager.Instance != null) {
            ExchangeDataManager.Instance.OnDataUpdated += RefreshChart;
        }
        RefreshChart();
    }

    // ★ UI 꺼질 때 이벤트 해제 (메모리 최적화)
    void OnDisable() {
        if (ExchangeDataManager.Instance != null) {
            ExchangeDataManager.Instance.OnDataUpdated -= RefreshChart;
        }
    }

    void Start() {
        OnClick30M(); // 시작 시 30M 모드
    }

    // ★★★ 데이터를 가져와서 그리는 통합 함수 ★★★
    public void RefreshChart() {
        if (ExchangeDataManager.Instance == null) return;

        List<float> targetData = (currentMode == ChartPeriod.M30)
            ? ExchangeDataManager.Instance.history30M
            : ExchangeDataManager.Instance.history1D;

        DrawChart(targetData);
        UpdateExchangeRateUI(targetData);
    }

    // ★★★ [에러 해결 1] CoinManager 호환용 함수 (유지) ★★★
    public void AddPriceData(float newRate) {
        // 실제 데이터 관리는 ExchangeDataManager에서 하므로 비워둡니다.
    }

    // ★★★ [에러 해결 2] SatoshiBankPanel 호환용 함수 (유지) ★★★
    public void DrawChart() {
        RefreshChart();
    }

    public void UpdateExchangeRateUI() {
        RefreshChart();
    }

    // --- 버튼 이벤트 ---
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

        // 매니저에서 maxDays 가져오기
        int maxDays = ExchangeDataManager.Instance != null ? ExchangeDataManager.Instance.maxDays : 30;
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

        // 1. exchangeRateText 현지화 (대한민국 원 -> South Korean Won)
        if (exchangeRateText != null) {
            string format = LocalizationManager.GetText("LBL_EXCHANGE_RATE_FORMAT");
            // {0}: 색상 코드, {1}: 현재 가격
            exchangeRateText.text = string.Format(format, hexColor, currentPrice.ToString("N2"));
        }

        // 2. mainRateText 현지화 (1USD = XXX KRW)
        if (mainRateText != null) {
            string format = LocalizationManager.GetText("LBL_EXCHANGE_MAIN_FORMAT");
            mainRateText.text = string.Format(format, hexColor, currentPrice.ToString("N2"));
        }

        // 3. diffRateText (등락폭 표시 - 기호 및 숫자이므로 언어 무관)
        if (diffRateText != null) {
            float diffPercent = (prevPrice != 0) ? (diffPrice / prevPrice) * 100f : 0f;
            string sign = diffPrice > 0 ? "+" : "";
            diffRateText.text = $"{sign}{diffPrice:N2} / {sign}{diffPercent:N2}%";
            diffRateText.color = targetColor;
        }
    }
}