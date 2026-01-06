using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;

public class LiveChartRenderer : MonoBehaviour {
    [Header("Refs")]
    public RectTransform chartContent;
    public RectTransform viewport;
    public GameObject candlePrefab;

    [Header("Drivers")]
    public ChartPriceDriver priceDriver;

    [Header("UI")]
    public TextMeshProUGUI priceInfoLabel;

    [Header("Chart Settings")]
    public float chartHeight = 200f;
    public float candleSpacing = 10f;

    // [설정] 차트 위아래 여백 비율 (0.1 = 10%)
    [Range(0f, 0.5f)]
    public float verticalPadding = 0.1f;

    // ===== Scroll Settings =====
    [Header("Scroll Logic")]
    public int futureEmptyCandles = 3;
    public int maxPastScrollCandles = 100;

    // ===== Runtime Data =====
    private CoinData targetCoin;
    private RuntimeCandle currentCandle;
    private List<RuntimeCandle> candles = new();
    private List<CandleView> candleViews = new();

    [Header("High/Low Indicators")]
    public TextMeshProUGUI highPriceText;
    public TextMeshProUGUI lowPriceText;

    public float indicatorYOffset = 30f;
    public float indicatorXOffset = 0f;

    // 색상 설정
    private readonly Color colorHigh = new Color32(50, 214, 149, 255); // 초록
    private readonly Color colorLow = new Color32(230, 60, 60, 255);   // 빨강

    // [신규] 평단가 색상 (파란색 계열 추천)
    private readonly Color colorAvg = new Color32(0, 250, 255, 255);

    private bool initialized = false;
    private string coinName;
    private string coinSymbol;

    // ===== Input & State =====
    private bool isDragging = false;
    private Vector2 lastLocalMousePos;
    private bool followLatest = true;
    private Canvas parentCanvas;

    private int visibleStartIndex = 0;
    private int visibleEndIndex = 0;
    private bool lastPausedState = false;

    [Header("Current Price UI")]
    public RectTransform currentPriceLineRect;
    public RectTransform priceTagOverlayRect;
    public TextMeshProUGUI priceTagText;
    public Image priceTagImage;

    // [신규] 평균단가 UI (에디터 할당 필요)
    [Header("Average Price UI")]
    public RectTransform avgPriceLineRect;       // 평단가 선 (Viewport 내부)
    public RectTransform avgPriceTagOverlayRect; // 평단가 태그 (Panel 위, Viewport 밖)
    public TextMeshProUGUI avgPriceTagText;      // 평단가 텍스트
    public Image avgPriceTagImage;               // 평단가 배경 이미지

    [Header("Y-Axis Grid (Prefab Auto-Gen)")]
    public RectTransform yAxisGridContainer;
    public GameObject gridLabelPrefab;
    public int gridCount = 5;
    private List<TextMeshProUGUI> gridLabels = new List<TextMeshProUGUI>();

    public void Initialize(CoinData coin) {
        ClearChart();
        if (CoinManager.Instance != null) {
            CoinManager.Instance.OnCandleBoundary -= HandleCandleBoundary;
            CoinManager.Instance.OnCandleBoundary += HandleCandleBoundary;
        }

        chartContent.pivot = new Vector2(0f, 0.5f);
        chartContent.anchorMin = new Vector2(0f, 0.5f);
        chartContent.anchorMax = new Vector2(0f, 0.5f);
        chartContent.anchoredPosition = Vector2.zero;

        parentCanvas = viewport.GetComponentInParent<Canvas>();
        targetCoin = coin;
        coinName = coin.Name;
        coinSymbol = coin.Symbol;

        for (int i = 0; i < coin.CandleHistory.Count; i++) {
            CreateCandleVisual(new RuntimeCandle(
                coin.CandleHistory[i].open,
                coin.CandleHistory[i].high,
                coin.CandleHistory[i].low,
                coin.CandleHistory[i].close
            ));
        }

        var realCandle = coin.CurrentRuntimeCandle;
        if (realCandle != null) {
            currentCandle = new RuntimeCandle();
            currentCandle.SetupAsActive(
                realCandle.Open,
                realCandle.High,
                realCandle.Low,
                realCandle.Close
            );
            CreateCandleVisual(currentCandle);
            priceDriver.Initialize(currentCandle.Close);
        } else {
            priceDriver.Initialize(coin.CurrentPrice);
        }

        initialized = true;
        followLatest = true;

        StartCoroutine(SnapInitialNextFrame());
        InitializeGridLabels();
    }

    private void InitializeGridLabels() {
        foreach (var lbl in gridLabels) if (lbl != null) Destroy(lbl.gameObject);
        gridLabels.Clear();

        if (yAxisGridContainer == null || gridLabelPrefab == null) return;

        for (int i = 0; i < gridCount; i++) {
            GameObject go = Instantiate(gridLabelPrefab, yAxisGridContainer);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Right;
            tmp.enableWordWrapping = false;
            go.SetActive(true);
            gridLabels.Add(tmp);
        }
    }

    IEnumerator SnapInitialNextFrame() {
        Canvas.ForceUpdateCanvases();
        yield return null;
        SnapToLatest();
    }

    private void Update() {
        if (!initialized || targetCoin == null) return;

        bool paused = CoinManager.Instance.IsTimePaused;

        // 일시정지 상태 변경 체크 (기존 로직 유지)
        if (paused != lastPausedState) {
            if (paused) priceDriver.Freeze(); else priceDriver.Resume();
            lastPausedState = paused;
        }

        // [삭제 또는 주석 처리] 이 줄 때문에 일시정지 시 입력도 막히던 것임
        // if (paused) return; 

        // 1. 데이터(가격/캔들) 업데이트는 '일시정지가 아닐 때만' 실행
        if (!paused) {
            TickPrice();
            UpdateCurrentCandle();
        }

        // 2. 입력(드래그) 및 화면 렌더링은 '항상' 실행 (일시정지 상관없이)
        HandleInput();

        // 최신 데이터 따라가기 (드래그 중이 아니고, 일시정지가 아닐 때만 강제로 붙게 하거나
        // 일시정지 중에도 맨 끝에 있으면 계속 붙어있게 할 수 있음. 
        // 보통 일시정지 중 드래그를 원하시니 기본 로직 그대로 둡니다.)
        if (followLatest && !isDragging) {
            // (선택사항) 일시정지 중에는 굳이 강제로 최신 위치로 이동 안 해도 된다면
            // 조건문에 && !paused 를 추가하셔도 됩니다.
            SnapToLatest();
        }

        UpdateVisibleRangeAndRender();

        UpdateCurrentPriceLine();

        // [신규] 평단가 라인 업데이트 (매 프레임)
        UpdateAveragePriceLine();
    }

    private void UpdateCurrentPriceLine() {
        double currentPrice = priceDriver.displayPrice;
        float yPos = PriceToY(currentPrice);

        // 현재가는 보통 화면 밖으로 나가면 선도 안 보이는 게 자연스러우므로 Clamping 안 함
        // (원하면 Clamping 추가 가능)

        if (currentPriceLineRect != null) {
            currentPriceLineRect.anchoredPosition = new Vector2(0f, yPos);
        }

        if (priceTagOverlayRect != null) {
            float currentX = priceTagOverlayRect.anchoredPosition.x;
            priceTagOverlayRect.anchoredPosition = new Vector2(currentX, yPos);
        }

        if (priceTagText != null) {
            priceTagText.text = FormatPrice(currentPrice);
        }

        if (currentCandle != null && priceTagImage != null) {
            bool isBull = currentPrice >= currentCandle.Open;
            priceTagImage.color = isBull ? colorHigh : colorLow;
        }
    }

    // [핵심] 평단가 라인 업데이트 (화면 밖 고정 기능 포함)
    private void UpdateAveragePriceLine() {
        // 1. UI 연결 및 보유 여부 확인
        if (avgPriceLineRect == null || avgPriceTagOverlayRect == null) return;

        bool hasCoin = PlayerManager.Instance.holdings.ContainsKey(coinSymbol) &&
                       PlayerManager.Instance.holdings[coinSymbol] > 0;

        if (!hasCoin) {
            if (avgPriceLineRect.gameObject.activeSelf) avgPriceLineRect.gameObject.SetActive(false);
            if (avgPriceTagOverlayRect.gameObject.activeSelf) avgPriceTagOverlayRect.gameObject.SetActive(false);
            return;
        }

        if (!avgPriceLineRect.gameObject.activeSelf) avgPriceLineRect.gameObject.SetActive(true);
        if (!avgPriceTagOverlayRect.gameObject.activeSelf) avgPriceTagOverlayRect.gameObject.SetActive(true);

        // 2. 평단가 가져오기 & Y좌표 변환
        double avgPrice = PlayerManager.Instance.GetAvgPrice(coinSymbol);
        float rawY = PriceToY(avgPrice);

        // 3. 화면 밖으로 나가지 않게 고정 (Clamping)
        // Viewport의 높이 절반 (중심이 0이므로 위쪽은 +Height/2, 아래쪽은 -Height/2)
        float viewportHalfHeight = viewport.rect.height * 0.5f;

        // 태그가 화면 끝에 딱 걸치도록 태그 높이의 절반만큼 여유를 둠
        float tagHalfHeight = avgPriceTagOverlayRect.rect.height * 0.5f;
        float clampLimit = viewportHalfHeight - tagHalfHeight;

        // 최종 Y 좌표 (화면 안이면 그대로, 밖이면 끝에 고정)
        float clampedY = Mathf.Clamp(rawY, -clampLimit, clampLimit);

        // 4. 위치 적용
        // 선 (Viewport 안쪽)
        avgPriceLineRect.anchoredPosition = new Vector2(0f, clampedY);

        // 태그 (Overlay 위) - 선과 동일한 Y높이
        float currentX = avgPriceTagOverlayRect.anchoredPosition.x;
        avgPriceTagOverlayRect.anchoredPosition = new Vector2(currentX, clampedY);

        // 5. 텍스트 & 색상
        if (avgPriceTagText != null) avgPriceTagText.text = FormatPrice(avgPrice);
        if (avgPriceTagImage != null) avgPriceTagImage.color = colorAvg;
    }

    private void CreateCandleVisual(RuntimeCandle candle) {
        if (candles.Contains(candle)) return;

        var go = Instantiate(candlePrefab, chartContent);
        var rt = go.GetComponent<RectTransform>();

        float xPos = candles.Count * candleSpacing;
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(xPos, 0f);

        var view = go.GetComponent<CandleView>();
        view.PriceToY = PriceToY;

        candles.Add(candle);
        candleViews.Add(view);

        float contentWidth = (candles.Count + futureEmptyCandles) * candleSpacing;
        chartContent.sizeDelta = new Vector2(contentWidth, chartContent.sizeDelta.y);
    }

    private void HandleInput() {
        Camera cam = parentCanvas.worldCamera;

        if (Input.GetMouseButtonDown(0)) {
            if (RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam)) {
                isDragging = true;
                followLatest = false;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out lastLocalMousePos);
            }
        }

        if (isDragging && Input.GetMouseButton(0)) {
            Vector2 currentLocalPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out currentLocalPos)) {

                float deltaX = currentLocalPos.x - lastLocalMousePos.x;
                Vector2 newPos = chartContent.anchoredPosition;
                newPos.x += deltaX;

                float maxScrollX = maxPastScrollCandles * candleSpacing;
                float minScrollX = GetMaxFutureScrollX();

                newPos.x = Mathf.Clamp(newPos.x, minScrollX, maxScrollX);
                chartContent.anchoredPosition = newPos;
                lastLocalMousePos = currentLocalPos;

                if (!isDragging && Mathf.Abs(chartContent.anchoredPosition.x - GetLatestDataScrollX()) < candleSpacing * 0.5f) {
                    followLatest = true;
                }
            }
        }

        if (Input.GetMouseButtonUp(0)) {
            isDragging = false;
        }
    }

    private void SnapToLatest() {
        if (candles.Count == 0) return;
        chartContent.anchoredPosition = new Vector2(GetLatestDataScrollX(), 0f);
    }

    float GetLatestDataScrollX() {
        float realContentWidth = candles.Count * candleSpacing;
        float viewportWidth = viewport.rect.width;

        if (realContentWidth <= viewportWidth) return 0f;
        return viewportWidth - realContentWidth;
    }

    float GetMaxFutureScrollX() {
        int latestRemain = futureEmptyCandles;
        float contentWidth = (candles.Count) * candleSpacing;
        float viewportWidth = viewport.rect.width;
        float lastVisibleCenterIndex = (candles.Count - latestRemain) + 0.5f;
        return -lastVisibleCenterIndex * candleSpacing;
    }

    private void UpdateVisibleRangeAndRender() {
        UpdatePriceLabel(targetCoin.CurrentPrice);

        float currentX = chartContent.anchoredPosition.x;
        float viewportWidth = viewport.rect.width;

        float viewStartPos = -currentX;
        float viewEndPos = -currentX + viewportWidth;

        visibleStartIndex = Mathf.FloorToInt(viewStartPos / candleSpacing);
        visibleEndIndex = Mathf.CeilToInt(viewEndPos / candleSpacing);

        visibleStartIndex -= 2;
        visibleEndIndex += 2;

        visibleStartIndex = Mathf.Clamp(visibleStartIndex, 0, candles.Count - 1);
        visibleEndIndex = Mathf.Clamp(visibleEndIndex, 0, candles.Count - 1);

        double maxHigh = double.MinValue;
        double minLow = double.MaxValue;
        int maxHighIndex = -1;
        int minLowIndex = -1;

        for (int i = 0; i < candleViews.Count; i++) {
            bool isVisible = (i >= visibleStartIndex && i <= visibleEndIndex);

            if (candleViews[i].gameObject.activeSelf != isVisible) {
                candleViews[i].gameObject.SetActive(isVisible);
            }

            if (isVisible) {
                candleViews[i].UpdateView(candles[i]);

                var c = candles[i];
                if (c.High >= maxHigh) {
                    maxHigh = c.High;
                    maxHighIndex = i;
                }
                if (c.Low < minLow) {
                    minLow = c.Low;
                    minLowIndex = i;
                }
            }
        }

        UpdateHighLowIndicators(maxHighIndex, maxHigh, minLowIndex, minLow);

        // [중요] 패딩을 적용하여 텍스트 범위를 계산
        if (maxHigh > minLow) {
            double range = maxHigh - minLow;
            double padding = (range == 0) ? maxHigh * 0.01 : range * verticalPadding;
            UpdateGridLabels(minLow - padding, maxHigh + padding);
        }
    }

    private void UpdateGridLabels(double minPrice, double maxPrice) {
        if (gridLabels.Count == 0) return;

        double priceRange = maxPrice - minPrice;
        double step = priceRange / (gridCount - 1);

        for (int i = 0; i < gridLabels.Count; i++) {
            double targetPrice = minPrice + (step * i);
            gridLabels[i].text = FormatPrice(targetPrice);
            float yPos = PriceToY(targetPrice);
            gridLabels[i].rectTransform.anchoredPosition = new Vector2(0, yPos);
        }
    }

    private string FormatPrice(double price) {
        if (price >= 1000) return price.ToString("N0");
        else if (price >= 100) return price.ToString("N2");
        else if (price >= 10) return price.ToString("N3");
        else return price.ToString("N4");
    }

    private void UpdateHighLowIndicators(int highIndex, double highPrice, int lowIndex, double lowPrice) {
        float leftGap = candleSpacing * 1.0f;

        void SetupIndicator(TextMeshProUGUI tmp, int index, double price, bool isHigh) {
            if (tmp == null) return;

            if (index != -1) {
                tmp.gameObject.SetActive(true);
                tmp.text = FormatPrice(price);
                tmp.color = isHigh ? colorHigh : colorLow;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;

                var rt = tmp.rectTransform;
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(0, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(150f, 50f);

                float xPos = (index * candleSpacing) + leftGap + indicatorXOffset;
                float yBase = PriceToY(price);
                float yPos = isHigh ? (yBase + indicatorYOffset) : (yBase - indicatorYOffset);

                rt.anchoredPosition = new Vector2(xPos, yPos);
            } else {
                tmp.gameObject.SetActive(false);
            }
        }

        SetupIndicator(highPriceText, highIndex, highPrice, true);
        SetupIndicator(lowPriceText, lowIndex, lowPrice, false);
    }

    private float PriceToY(double price) {
        if (candles.Count == 0) return 0f;
        double min = double.MaxValue;
        double max = double.MinValue;
        for (int i = visibleStartIndex; i <= visibleEndIndex; i++) {
            var c = candles[i];
            if (c.High > max) max = c.High;
            if (c.Low < min) min = c.Low;
        }
        if (max == double.MinValue) return 0f;

        double range = max - min;
        double padding = (range == 0) ? max * 0.01 : range * verticalPadding;

        min -= padding;
        max += padding;
        range = max - min;

        if (range <= 0) range = 1f;
        float normalized = (float)((price - min) / range);
        return Mathf.Lerp(-chartHeight * 0.5f, chartHeight * 0.5f, normalized);
    }

    void HandleCandleBoundary(DateTime time) {
        var realRuntime = targetCoin.CurrentRuntimeCandle;
        if (realRuntime == null) return;
        currentCandle = new RuntimeCandle();
        currentCandle.Start(realRuntime.Open);
        CreateCandleVisual(currentCandle);
    }

    void TickPrice() {
        priceDriver.SetTargetPrice(targetCoin.CurrentPrice);
        priceDriver.Tick(Time.deltaTime);
    }
    void UpdateCurrentCandle() {
        if (currentCandle == null || currentCandle.IsClosed) return;
        currentCandle.UpdatePrice(priceDriver.displayPrice);
    }
    private void UpdatePriceLabel(double price) {
        if (priceInfoLabel != null && targetCoin != null) {
            priceInfoLabel.text =
                $"{coinName}({coinSymbol}) {targetCoin.GetFormattedPriceKRW()}";
        }
    }
    private void ClearChart() {
        foreach (var view in candleViews) if (view != null) Destroy(view.gameObject);
        candles.Clear();
        candleViews.Clear();
        chartContent.anchoredPosition = Vector2.zero;
        initialized = false;
        followLatest = true;
    }
}