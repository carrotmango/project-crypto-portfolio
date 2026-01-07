using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum ChartInterval {
    _4H,
    _1D
}

public class LiveChartRenderer : MonoBehaviour {
    [Header("Refs")]
    public RectTransform chartContent;
    public RectTransform viewport;
    public GameObject candlePrefab;

    [Header("Drivers")]
    public ChartPriceDriver priceDriver;

    [Header("UI Controls")]
    public Button btn4H;
    public Button btn1D;
    public Button btnZoomIn;
    public Button btnZoomOut;

    [Header("Horizontal Line Settings")]
    public Button btnHLine;
    public GameObject hLinePrefab;
    public Color hLineColor = Color.yellow;
    private bool isPlacingHLine = false;
    private List<HorizontalLineView> activeHLines = new List<HorizontalLineView>();

    public Color activeBtnColor = new Color32(255, 255, 255, 255);
    public Color inactiveBtnColor = new Color32(100, 100, 100, 255);

    [Header("Measurement Tool Settings")]
    public Button btnMeasure;            // [신규] 측정 도구 버튼
    public GameObject measurementPrefab;
    private MeasurementView currentMeasurement;
    private bool isMeasuring = false;
    private bool isMeasurementMode = false; // [신규] 측정 모드 플래그
    private Vector2 measureStartPos;
    private double measureStartPrice;

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

    [Header("Crosshair UI")]
    public RectTransform crosshairV;
    public RectTransform crosshairH;

    private CoinData targetCoin;
    private RuntimeCandle currentCandle;
    private List<RuntimeCandle> candles = new();
    private List<CandleView> candleViews = new();

    private ChartInterval currentInterval = ChartInterval._4H;

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

    // Y좌표 역계산을 위한 임시 변수
    private double currentVisibleMinPrice;
    private double currentVisibleMaxPrice;

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

    [Header("Y-Axis Grid")]
    public RectTransform yAxisGridContainer;
    public GameObject gridLabelPrefab;
    public int gridCount = 5;
    private List<TextMeshProUGUI> gridLabels = new List<TextMeshProUGUI>();

    [Header("Zoom Settings")]
    public float minCandleSpacing = 2f;
    public float maxCandleSpacing = 50f;
    public float zoomSensitivity = 2f;
    public float buttonZoomStep = 5f;

    private void Awake() {
        if (btn4H != null) btn4H.onClick.AddListener(() => SwitchInterval(ChartInterval._4H));
        if (btn1D != null) btn1D.onClick.AddListener(() => SwitchInterval(ChartInterval._1D));
        if (btnZoomIn != null) btnZoomIn.onClick.AddListener(OnZoomInBtn);
        if (btnZoomOut != null) btnZoomOut.onClick.AddListener(OnZoomOutBtn);

        if (btnHLine != null) btnHLine.onClick.AddListener(ToggleHLineMode);

        // [신규] 측정 버튼 리스너
        if (btnMeasure != null) btnMeasure.onClick.AddListener(ToggleMeasurementMode);
    }

    public void Initialize(CoinData coin) {
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

        SetupCrosshairs();
        InitializeGridLabels();

        SwitchInterval(ChartInterval._4H);

        priceDriver.Initialize(coin.CurrentPrice);
        initialized = true;

        StartCoroutine(SnapInitialNextFrame());
    }

    public void SwitchInterval(ChartInterval interval) {
        currentInterval = interval;
        ClearChart();

        isDragging = false;
        followLatest = true;
        currentCandle = null;

        UpdateIntervalButtonUI();

        List<RuntimeCandle> dataToRender = new List<RuntimeCandle>();
        var history = targetCoin.CandleHistory;

        if (interval == ChartInterval._4H) {
            foreach (ChartRenderer.CandleData h in history) {
                dataToRender.Add(new RuntimeCandle(h.open, h.high, h.low, h.close));
            }
            SetupCurrentCandleFor4H();
        } else {
            int totalCount = history.Count;
            int remainder = totalCount % 6;
            int fullDayCount = totalCount - remainder;

            dataToRender = AggregateFullDaysOnly(history, fullDayCount);

            List<ChartRenderer.CandleData> todayPartialHistory = new List<ChartRenderer.CandleData>();
            for (int i = fullDayCount; i < totalCount; i++) {
                todayPartialHistory.Add(history[i]);
            }
            SetupCurrentCandleFor1D(todayPartialHistory);
        }

        if (currentCandle != null) {
            dataToRender.Add(currentCandle);
        }

        foreach (var c in dataToRender) {
            CreateCandleVisual(c);
        }

        initialized = true;
        RefreshAllCandlePositions();
        Canvas.ForceUpdateCanvases();
        SnapToLatest();
        StartCoroutine(SnapInitialNextFrame());
    }

    private List<RuntimeCandle> AggregateFullDaysOnly(List<ChartRenderer.CandleData> history, int limitCount) {
        List<RuntimeCandle> list = new List<RuntimeCandle>();
        if (limitCount <= 0) return list;
        double o = 0, h = double.MinValue, l = double.MaxValue, c = 0;
        int count = 0;
        for (int i = 0; i < limitCount; i++) {
            var data = history[i];
            if (count == 0) o = data.open;
            if (data.high > h) h = data.high;
            if (data.low < l) l = data.low;
            c = data.close;
            count++;
            if (count == 6) {
                list.Add(new RuntimeCandle(o, h, l, c));
                count = 0;
                h = double.MinValue; l = double.MaxValue;
            }
        }
        return list;
    }

    private void SetupCurrentCandleFor4H() {
        var real = targetCoin.CurrentRuntimeCandle;
        if (real != null) {
            currentCandle = new RuntimeCandle();
            currentCandle.SetupAsActive(real.Open, real.High, real.Low, real.Close);
        }
    }

    private void SetupCurrentCandleFor1D(List<ChartRenderer.CandleData> todayHistory) {
        var real = targetCoin.CurrentRuntimeCandle;
        double open = 0, high = double.MinValue, low = double.MaxValue, close = 0;
        bool hasData = false;
        if (todayHistory != null && todayHistory.Count > 0) {
            open = todayHistory[0].open;
            foreach (var p in todayHistory) {
                if (p.high > high) high = p.high;
                if (p.low < low) low = p.low;
            }
            close = todayHistory[todayHistory.Count - 1].close;
            hasData = true;
        }
        if (real != null) {
            if (!hasData) {
                open = real.Open;
                high = real.High;
                low = real.Low;
            } else {
                if (real.High > high) high = real.High;
                if (real.Low < low) low = real.Low;
            }
            close = real.Close;
            hasData = true;
        }
        if (hasData) {
            currentCandle = new RuntimeCandle();
            currentCandle.SetupAsActive(open, high, low, close);
        }
    }

    private void UpdateIntervalButtonUI() {
        if (btn4H != null) btn4H.GetComponent<Image>().color = (currentInterval == ChartInterval._4H) ? activeBtnColor : inactiveBtnColor;
        if (btn1D != null) btn1D.GetComponent<Image>().color = (currentInterval == ChartInterval._1D) ? activeBtnColor : inactiveBtnColor;
    }

    public void OnZoomInBtn() { ApplyZoom(buttonZoomStep); }
    public void OnZoomOutBtn() { ApplyZoom(-buttonZoomStep); }

    public void ToggleHLineMode() {
        isPlacingHLine = !isPlacingHLine;
        if (btnHLine != null) {
            btnHLine.GetComponent<Image>().color = isPlacingHLine ? activeBtnColor : inactiveBtnColor;
        }

        // 수평선 모드 켤 때 측정 모드는 끄기 (충돌 방지)
        if (isPlacingHLine && isMeasurementMode) ToggleMeasurementMode();
    }

    // [신규] 측정 모드 토글
    public void ToggleMeasurementMode() {
        isMeasurementMode = !isMeasurementMode;
        if (btnMeasure != null) {
            btnMeasure.GetComponent<Image>().color = isMeasurementMode ? activeBtnColor : inactiveBtnColor;
        }

        // 측정 모드 켤 때 수평선 모드는 끄기
        if (isMeasurementMode && isPlacingHLine) ToggleHLineMode();
    }

    private void CreateHorizontalLineAtMouse() {
        if (hLinePrefab == null) return;

        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out localPoint)) {
            return;
        }

        double price = YToPrice(localPoint.y);

        Transform overlayParent = crosshairV.parent;
        GameObject go = Instantiate(hLinePrefab, overlayParent);
        go.transform.SetAsLastSibling();

        HorizontalLineView view = go.GetComponent<HorizontalLineView>();
        if (view != null) {
            view.Setup(this, price, hLineColor);
            float yPos = PriceToY(price);
            view.UpdatePosition(yPos);
            activeHLines.Add(view);
        }
    }

    public void RemoveHorizontalLine(HorizontalLineView line) {
        if (activeHLines.Contains(line)) {
            activeHLines.Remove(line);
            Destroy(line.gameObject);
        }
    }

    private double YToPrice(float yPos) {
        float halfHeight = chartHeight * 0.5f;
        float normalized = (yPos + halfHeight) / chartHeight;
        double range = currentVisibleMaxPrice - currentVisibleMinPrice;
        if (range <= 0) range = 1f;
        double price = currentVisibleMinPrice + (normalized * range);
        return price;
    }

    private void ApplyZoom(float amount) {
        float newSpacing = candleSpacing + amount;
        newSpacing = Mathf.Clamp(newSpacing, minCandleSpacing, maxCandleSpacing);
        if (Mathf.Abs(newSpacing - candleSpacing) > 0.001f) {
            candleSpacing = newSpacing;
            RefreshAllCandlePositions();
        }
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

        // [핵심] 모드에 따른 입력 처리 분리
        // Shift 키를 누르고 있거나, 측정 버튼 모드가 켜져있으면 측정 입력 처리
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || isMeasurementMode) {
            HandleMeasurementInput();
        } else {
            // 그 외에는 일반 입력(드래그) 및 수평선 입력
            HandleInput();
            HandleHorizontalLineInput();
        }

        HandleZoom();
        HandleCursorTracking();

        if (followLatest && !isDragging && !isMeasuring) {
            SnapToLatest();
        }

        UpdateVisibleRangeAndRender();
        UpdateCurrentPriceLine();
        UpdateAveragePriceLine();
    }

    private void HandleMeasurementInput() {
        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;

        // 1. 클릭 시작 (측정 시작)
        if (Input.GetMouseButtonDown(0) && !isMeasuring) {

            // [중요] 버튼 클릭 방지 (버튼 눌러서 모드 켰는데, 또 버튼 누르면 측정 시작되면 안됨)
            if (IsPointerOverButton(btnMeasure, cam)) return;

            if (RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam)) {
                isMeasuring = true;
                followLatest = false;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(chartContent, Input.mousePosition, cam, out measureStartPos);

                Vector2 viewportLocalPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out viewportLocalPos);
                measureStartPrice = YToPrice(viewportLocalPos.y);

                if (currentMeasurement != null) {
                    Destroy(currentMeasurement.gameObject);
                }

                if (measurementPrefab != null) {
                    GameObject go = Instantiate(measurementPrefab, chartContent);
                    currentMeasurement = go.GetComponent<MeasurementView>();
                    currentMeasurement.Setup(measureStartPos);
                }
            }
        }

        // 2. 드래그 중
        if (isMeasuring && Input.GetMouseButton(0)) {
            if (currentMeasurement != null) {
                Vector2 currentPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(chartContent, Input.mousePosition, cam, out currentPos);

                Vector2 viewportLocalPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out viewportLocalPos);
                double currentPrice = YToPrice(viewportLocalPos.y);

                currentMeasurement.UpdateView(measureStartPos, currentPos, measureStartPrice, currentPrice);
            }
        }

        // 3. 마우스 뗌 (측정 종료 및 삭제)
        if (Input.GetMouseButtonUp(0)) {
            if (isMeasuring) {
                isMeasuring = false;
                if (currentMeasurement != null) {
                    Destroy(currentMeasurement.gameObject);
                    currentMeasurement = null;
                }

                // [핵심] 일회성: 버튼으로 모드를 켰다면, 측정 후 자동으로 꺼줌
                if (isMeasurementMode) {
                    ToggleMeasurementMode();
                }
            }
        }
    }

    private void HandleHorizontalLineInput() {
        if (Input.GetKeyDown(KeyCode.H) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) {
            Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
            if (RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam)) {
                CreateHorizontalLineAtMouse();
            }
        }

        if (isPlacingHLine && Input.GetMouseButtonDown(0)) {
            Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
            if (!IsPointerOverButton(btnHLine, cam) &&
                !IsPointerOverButton(btn4H, cam) &&
                !IsPointerOverButton(btn1D, cam) &&
                RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam)) {
                CreateHorizontalLineAtMouse();
                ToggleHLineMode();
            }
        }
    }

    void SetupCrosshairs() {
        if (crosshairV != null) {
            crosshairV.anchorMin = new Vector2(0.5f, 0f);
            crosshairV.anchorMax = new Vector2(0.5f, 1f);
            crosshairV.pivot = new Vector2(0.5f, 0.5f);
            crosshairV.sizeDelta = new Vector2(1f, 0f);
            crosshairV.gameObject.SetActive(false);
        }
        if (crosshairH != null) {
            crosshairH.anchorMin = new Vector2(0f, 0.5f);
            crosshairH.anchorMax = new Vector2(1f, 0.5f);
            crosshairH.pivot = new Vector2(0.5f, 0.5f);
            crosshairH.sizeDelta = new Vector2(0f, 1f);
            crosshairH.gameObject.SetActive(false);
        }
    }

    void HandleCursorTracking() {
        if (crosshairV == null || crosshairH == null) return;
        Camera cam = null;
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera || parentCanvas.renderMode == RenderMode.WorldSpace)
            cam = parentCanvas.worldCamera;

        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam);
        if (crosshairV.gameObject.activeSelf != isInside) crosshairV.gameObject.SetActive(isInside);
        if (crosshairH.gameObject.activeSelf != isInside) crosshairH.gameObject.SetActive(isInside);

        if (!isInside) return;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out localPoint);
        crosshairV.anchoredPosition = new Vector2(localPoint.x, 0f);
        crosshairH.anchoredPosition = new Vector2(0f, localPoint.y);
    }

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
            double openPrice = currentCandle.Open > 0 ? currentCandle.Open : currentPrice;
            bool isBull = currentPrice >= openPrice;
            priceTagImage.color = isBull ? colorHigh : colorLow;
        }
    }

    private void UpdateAveragePriceLine() {
        if (avgPriceLineRect == null || avgPriceTagOverlayRect == null) return;
        bool hasCoin = PlayerManager.Instance.holdings.ContainsKey(coinSymbol) && PlayerManager.Instance.holdings[coinSymbol] > 0;
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
            if (IsPointerOverButton(btn4H, cam) || IsPointerOverButton(btn1D, cam) ||
                IsPointerOverButton(btnZoomIn, cam) || IsPointerOverButton(btnZoomOut, cam) ||
                IsPointerOverButton(btnHLine, cam) || IsPointerOverButton(btnMeasure, cam)) { // [수정] 측정 버튼도 예외 처리
                return;
            }
            if (RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam)) {
                if (!isPlacingHLine) {
                    isDragging = true;
                    followLatest = false;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out lastLocalMousePos);
                }
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

    private bool IsPointerOverButton(Button btn, Camera cam) {
        if (btn == null || !btn.gameObject.activeInHierarchy) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(btn.GetComponent<RectTransform>(), Input.mousePosition, cam);
    }

    private void SnapToLatest() {
        if (candles.Count == 0) return;
        float expectedWidth = (candles.Count + futureEmptyCandles) * candleSpacing;
        if (Mathf.Abs(chartContent.sizeDelta.x - expectedWidth) > 1f) {
            chartContent.sizeDelta = new Vector2(expectedWidth, chartContent.sizeDelta.y);
        }
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

        float currentBodyWidth = Mathf.Max(1f, candleSpacing * 0.8f);

        for (int i = 0; i < candleViews.Count; i++) {
            bool isVisible = (i >= visibleStartIndex && i <= visibleEndIndex);
            if (candleViews[i].gameObject.activeSelf != isVisible)
                candleViews[i].gameObject.SetActive(isVisible);

            if (isVisible) {
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

            currentVisibleMinPrice = minLow - padding;
            currentVisibleMaxPrice = maxHigh + padding;

            UpdateGridLabels(currentVisibleMinPrice, currentVisibleMaxPrice);
        }

        UpdateHorizontalLines();
    }

    private void UpdateHorizontalLines() {
        if (activeHLines.Count == 0) return;

        foreach (var line in activeHLines) {
            if (line != null) {
                float yPos = PriceToY(line.TargetPrice);
                line.UpdatePosition(yPos);
            }
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
        if (currentInterval == ChartInterval._1D) {
            if (time.Hour == 9 && time.Minute == 0) {
                SwitchInterval(ChartInterval._1D);
            }
            return;
        }
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
        foreach (var view in candleViews) if (view != null) DestroyImmediate(view.gameObject);
        candles.Clear();
        candleViews.Clear();
    }

    void HandleZoom() {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;
        ApplyZoom(scroll * zoomSensitivity);
    }

    void RefreshAllCandlePositions() {
        if (candles.Count == 0) return;
        for (int i = 0; i < candleViews.Count; i++) {
            RectTransform rt = candleViews[i].GetComponent<RectTransform>();
            float xPos = i * candleSpacing;
            rt.anchoredPosition = new Vector2(xPos, 0f);
        }
        float contentWidth = (candles.Count + futureEmptyCandles) * candleSpacing;
        chartContent.sizeDelta = new Vector2(contentWidth, chartContent.sizeDelta.y);
    }
}