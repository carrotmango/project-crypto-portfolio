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
    [Range(0f, 0.5f)]
    public float verticalPadding = 0.1f;

    [Header("Scroll Logic")]
    public int futureEmptyCandles = 3;
    public int maxPastScrollCandles = 100;

    // ===== Crosshair UI (추가됨) =====
    [Header("Crosshair UI")]
    public RectTransform crosshairV; // 세로선
    public RectTransform crosshairH; // 가로선
    // ===============================

    private CoinData targetCoin;
    private RuntimeCandle currentCandle;
    private List<RuntimeCandle> candles = new();
    private List<CandleView> candleViews = new();

    [Header("High/Low Indicators")]
    public TextMeshProUGUI highPriceText;
    public TextMeshProUGUI lowPriceText;
    public float indicatorYOffset = 30f;
    public float indicatorXOffset = 0f;

    private readonly Color colorHigh = new Color32(50, 214, 149, 255);
    private readonly Color colorLow = new Color32(230, 60, 60, 255);
    private readonly Color colorAvg = new Color32(0, 250, 255, 255);

    private bool initialized = false;
    private string coinName;
    private string coinSymbol;

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

    [Header("Average Price UI")]
    public RectTransform avgPriceLineRect;
    public RectTransform avgPriceTagOverlayRect;
    public TextMeshProUGUI avgPriceTagText;
    public Image avgPriceTagImage;

    [Header("Y-Axis Grid (Prefab Auto-Gen)")]
    public RectTransform yAxisGridContainer;
    public GameObject gridLabelPrefab;
    public int gridCount = 5;
    private List<TextMeshProUGUI> gridLabels = new List<TextMeshProUGUI>();

    [Header("Zoom Settings")]
    public float minCandleSpacing = 2f;  // 제일 작게 (많이 보임)
    public float maxCandleSpacing = 50f; // 제일 크게 (적게 보임)
    public float zoomSensitivity = 2f;   // 휠 감도

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
            currentCandle.SetupAsActive(realCandle.Open, realCandle.High, realCandle.Low, realCandle.Close);
            CreateCandleVisual(currentCandle);
            priceDriver.Initialize(currentCandle.Close);
        } else {
            priceDriver.Initialize(coin.CurrentPrice);
        }

        initialized = true;
        followLatest = true;

        // [추가] 십자선 초기화
        SetupCrosshairs();

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
        if (paused != lastPausedState) {
            if (paused) priceDriver.Freeze(); else priceDriver.Resume();
            lastPausedState = paused;
        }

        if (!paused) {
            TickPrice();
            UpdateCurrentCandle();
        }

        HandleInput();
        HandleZoom();
        // [추가] 십자선 업데이트 (일시정지 상관없이 항상 작동)
        HandleCursorTracking();

        if (followLatest && !isDragging) {
            SnapToLatest();
        }

        UpdateVisibleRangeAndRender();
        UpdateCurrentPriceLine();
        UpdateAveragePriceLine();
    }

    // ===== 십자선 로직 (추가됨) =====
    // [수정] 초기 설정: 앵커를 중앙(0.5, 0.5)으로 통일해야 좌표 오차가 사라짐
    void SetupCrosshairs() {
        if (crosshairV != null) {
            // 세로선: 가로 위치는 중앙 기준, 세로는 꽉 차게
            crosshairV.anchorMin = new Vector2(0.5f, 0f);
            crosshairV.anchorMax = new Vector2(0.5f, 1f);
            crosshairV.pivot = new Vector2(0.5f, 0.5f);
            crosshairV.sizeDelta = new Vector2(1f, 0f); // 폭 1px, 높이는 0(Stretch)
            crosshairV.gameObject.SetActive(false);
        }

        if (crosshairH != null) {
            // 가로선: 세로 위치는 중앙 기준, 가로는 꽉 차게
            crosshairH.anchorMin = new Vector2(0f, 0.5f);
            crosshairH.anchorMax = new Vector2(1f, 0.5f);
            crosshairH.pivot = new Vector2(0.5f, 0.5f);
            crosshairH.sizeDelta = new Vector2(0f, 1f); // 폭 0(Stretch), 높이 1px
            crosshairH.gameObject.SetActive(false);
        }
    }

    // [수정] 마우스 추적 로직
    void HandleCursorTracking() {
        if (crosshairV == null || crosshairH == null) return;

        // 캔버스 렌더 모드에 따라 카메라 설정 (Overlay면 null, Camera면 worldCamera)
        Camera cam = null;
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera || parentCanvas.renderMode == RenderMode.WorldSpace) {
            cam = parentCanvas.worldCamera;
        }

        // 마우스가 Viewport 안에 있는지 확인
        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam);

        if (crosshairV.gameObject.activeSelf != isInside) crosshairV.gameObject.SetActive(isInside);
        if (crosshairH.gameObject.activeSelf != isInside) crosshairH.gameObject.SetActive(isInside);

        if (!isInside) return;

        // 마우스 좌표를 Viewport 기준 로컬 좌표로 변환
        // 이 좌표는 Viewport의 Pivot(보통 중앙)을 기준으로 나옵니다.
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out localPoint);

        // 십자선의 앵커도 중앙(0.5, 0.5)으로 맞췄으므로, 좌표를 그대로 넣으면 정확히 일치합니다.

        // 세로선: X좌표만 따라감
        crosshairV.anchoredPosition = new Vector2(localPoint.x, 0f);

        // 가로선: Y좌표만 따라감
        crosshairH.anchoredPosition = new Vector2(0f, localPoint.y);
    }
    // ===============================

    private void UpdateCurrentPriceLine() {
        double currentPrice = priceDriver.displayPrice;
        float yPos = PriceToY(currentPrice);

        if (currentPriceLineRect != null) currentPriceLineRect.anchoredPosition = new Vector2(0f, yPos);
        if (priceTagOverlayRect != null) {
            float currentX = priceTagOverlayRect.anchoredPosition.x;
            priceTagOverlayRect.anchoredPosition = new Vector2(currentX, yPos);
        }
        if (priceTagText != null) priceTagText.text = FormatPrice(currentPrice);
        if (currentCandle != null && priceTagImage != null) {
            bool isBull = currentPrice >= currentCandle.Open;
            priceTagImage.color = isBull ? colorHigh : colorLow;
        }
    }

    private void UpdateAveragePriceLine() {
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

        double avgPrice = PlayerManager.Instance.GetAvgPrice(coinSymbol);
        float rawY = PriceToY(avgPrice);

        float viewportHalfHeight = viewport.rect.height * 0.5f;
        float tagHalfHeight = avgPriceTagOverlayRect.rect.height * 0.5f;
        float clampLimit = viewportHalfHeight - tagHalfHeight;
        float clampedY = Mathf.Clamp(rawY, -clampLimit, clampLimit);

        avgPriceLineRect.anchoredPosition = new Vector2(0f, clampedY);
        float currentX = avgPriceTagOverlayRect.anchoredPosition.x;
        avgPriceTagOverlayRect.anchoredPosition = new Vector2(currentX, clampedY);

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
        Camera cam = parentCanvas != null ? parentCanvas.worldCamera : null;

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

        if (Input.GetMouseButtonUp(0)) isDragging = false;
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

        // [핵심] 현재 줌 상태(spacing)에 맞춰 캔들 너비 결정 (0.8 = 80% 채우기, 20% 여백)
        // 캔들 간격이 아무리 좁아져도 최소 1px은 보이게 Max(1f, ...) 처리
        float currentBodyWidth = Mathf.Max(1f, candleSpacing * 0.8f);

        for (int i = 0; i < candleViews.Count; i++) {
            bool isVisible = (i >= visibleStartIndex && i <= visibleEndIndex);

            if (candleViews[i].gameObject.activeSelf != isVisible)
                candleViews[i].gameObject.SetActive(isVisible);

            if (isVisible) {
                // [수정] 너비 값을 같이 넘겨줌
                candleViews[i].UpdateView(candles[i], currentBodyWidth);

                var c = candles[i];
                if (c.High >= maxHigh) { maxHigh = c.High; maxHighIndex = i; }
                if (c.Low < minLow) { minLow = c.Low; minLowIndex = i; }
            }
        }

        UpdateHighLowIndicators(maxHighIndex, maxHigh, minLowIndex, minLow);

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
            } else tmp.gameObject.SetActive(false);
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
            priceInfoLabel.text = $"{coinName}({coinSymbol}) {targetCoin.GetFormattedPriceKRW()}";
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
    // ===== 줌(Zoom) 로직 추가 =====
    void HandleZoom() {
        // 1. 휠 입력 감지
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        // 2. 줌 기준점 계산 (마우스 위치)
        // 마우스가 Viewport 안에 없으면 줌 동작 안 함
        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
        if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam)) return;

        // 마우스의 Viewport 내 로컬 좌표 구하기
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out localMousePos);

        // 현재 차트 위치 기준으로, 마우스가 "몇 번째 캔들" 위에 있는지 계산 (Pivot Index)
        // 수식: (마우스X - 차트시작X) / 간격 = 인덱스
        float chartX = chartContent.anchoredPosition.x;
        // Viewport 중심이 (0,0)이고 Pivot이 (0.5,0.5)일 때의 보정 필요할 수 있으나, 
        // 보통 Content.anchoredPosition과 마우스 좌표 차이를 이용함.
        // 여기서는 간단히 상대 거리를 이용:
        float mousePosInContent = localMousePos.x - chartX;
        float pivotIndex = mousePosInContent / candleSpacing;

        // 3. 간격 변경 (Zoom)
        float oldSpacing = candleSpacing;
        float newSpacing = oldSpacing + (scroll * zoomSensitivity);
        newSpacing = Mathf.Clamp(newSpacing, minCandleSpacing, maxCandleSpacing);

        // 변화가 없으면 리턴
        if (Mathf.Abs(newSpacing - oldSpacing) < 0.001f) return;

        candleSpacing = newSpacing;

        // 4. 모든 캔들 위치 재배치 (성능 최적화를 위해 보여지는 것만 할 수도 있지만, 구조상 전체 갱신이 안전)
        RefreshAllCandlePositions();

        // 5. 위치 보정 (Pivot 유지)
        // 아까 마우스 아래에 있던 그 인덱스가, 새로운 간격에서도 마우스 아래에 오도록 차트 이동
        // 새 마우스 위치(이론상) = pivotIndex * newSpacing
        // 이동해야 할 거리 = (새 마우스 위치 - 옛 마우스 위치) 만큼 차트를 반대로 밀어야 함
        float newMousePosInContent = pivotIndex * newSpacing;
        float diff = newMousePosInContent - mousePosInContent;

        Vector2 newChartPos = chartContent.anchoredPosition;
        newChartPos.x -= diff; // 차트를 이동시켜 마우스 위치 고정

        // 스크롤 범위 제한 (너무 멀리 안 가게)
        float maxScrollX = maxPastScrollCandles * candleSpacing;
        float minScrollX = GetMaxFutureScrollX();
        newChartPos.x = Mathf.Clamp(newChartPos.x, minScrollX, maxScrollX);

        chartContent.anchoredPosition = newChartPos;

        // 줌을 하면 '최신 따라가기' 모드를 끌지 말지 결정 (보통 줌하면 끔)
        followLatest = false;
    }

    // 캔들 간격이 바뀌었으므로 모든 캔들의 X좌표를 다시 잡아주는 함수
    void RefreshAllCandlePositions() {
        for (int i = 0; i < candleViews.Count; i++) {
            RectTransform rt = candleViews[i].GetComponent<RectTransform>();
            float xPos = i * candleSpacing;
            rt.anchoredPosition = new Vector2(xPos, 0f);
        }

        // Content 전체 크기 조절
        float contentWidth = (candles.Count + futureEmptyCandles) * candleSpacing;
        chartContent.sizeDelta = new Vector2(contentWidth, chartContent.sizeDelta.y);
    }

}