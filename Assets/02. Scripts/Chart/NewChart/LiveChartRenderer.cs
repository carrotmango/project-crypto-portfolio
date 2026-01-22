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

// [신규] 한 캔들에 매수/매도 상태를 중복 없이 저장하기 위한 플래그
[System.Flags]
public enum TradeType {
    None = 0,
    Buy = 1 << 0,  // 1
    Sell = 1 << 1  // 2
    // Buy | Sell = 3 (둘 다 있는 경우)
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
    public Button btnMeasure;
    public GameObject measurementPrefab;
    private MeasurementView currentMeasurement;
    private bool isMeasuring = false;
    private bool isMeasurementMode = false;
    private Vector2 measureStartPos;
    private double measureStartPrice;

    [Header("Trade Indicators (B/S)")] // [신규] 거래 마커 설정
    public GameObject tradeMarkerPrefab; // 'B' 또는 'S' 라고 적힌 텍스트 프리팹 (배경 이미지 포함 권장)
    public Transform tradeMarkerContainer; // 마커들이 모여있을 부모 (없으면 chartContent 사용)
    public float tradeMarkerYOffset = 10f; // 캔들 위/아래 간격 (기존 tradeMarkerOffset 대체)
    public float tradeMarkerXOffset = 0f;  // 좌우 미세 조정용 오프셋
    public Color buyMarkerColor = new Color32(50, 214, 149, 255); // 매수 색상
    public Color sellMarkerColor = new Color32(230, 60, 60, 255); // 매도 색상
    public float minMarkerFontSize = 7f; // 줌 아웃 했을 때 최소 글자 크기
    public float maxMarkerFontSize = 11f; // 줌 인 했을 때(기본) 최대 글자 크기
    private TradeType pendingOfflineTrades = TradeType.None;

    // RuntimeCandle(객체) 대신 int(인덱스)로 변경하여 영구 보존
    // Key: 종목코드(Symbol), Value: { 캔들번호(Index) : 거래타입 }
    private Dictionary<string, Dictionary<int, TradeType>> allCoinTradeHistory = new Dictionary<string, Dictionary<int, TradeType>>();

    // 마커 UI 오브젝트 풀
    private List<TextMeshProUGUI> tradeMarkerPool = new List<TextMeshProUGUI>();

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

    protected CoinData targetCoin;
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
    protected List<TextMeshProUGUI> gridLabels = new List<TextMeshProUGUI>();

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
        if (btnMeasure != null) btnMeasure.onClick.AddListener(ToggleMeasurementMode);

        if (tradeMarkerContainer == null) tradeMarkerContainer = chartContent;
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

    // [수정됨] 외부에서 호출 시 코인 데이터를 명확히 지정하는 버전 (권장)
    // 차트가 꺼져있거나, 다른 코인을 보고 있어도 기록이 정확히 남습니다.
    public void RegisterTrade(CoinData coin, bool isBuy) {
        if (coin == null) return;

        string symbol = coin.Symbol;
        TradeType typeToAdd = isBuy ? TradeType.Buy : TradeType.Sell;

        // 현재 생성 중인 캔들의 인덱스 (History 개수와 동일)
        int currentIndex = coin.CandleHistory.Count;

        // 1. 해당 코인의 장부가 없으면 새로 생성
        if (!allCoinTradeHistory.ContainsKey(symbol)) {
            allCoinTradeHistory[symbol] = new Dictionary<int, TradeType>();
        }

        // 2. 해당 인덱스에 기록 없으면 초기화
        if (!allCoinTradeHistory[symbol].ContainsKey(currentIndex)) {
            allCoinTradeHistory[symbol][currentIndex] = TradeType.None;
        }

        // 3. 기록 추가 (OR 연산)
        allCoinTradeHistory[symbol][currentIndex] |= typeToAdd;
    }

    // [유지] 기존 코드 호환용 (현재 보고 있는 코인에 기록)
    // 주의: 차트가 켜져있을 때만 정상 작동합니다.
    public void RegisterTrade(bool isBuy) {
        if (targetCoin != null) {
            RegisterTrade(targetCoin, isBuy);
        } else {
            Debug.LogWarning("차트가 초기화되지 않아 거래 기록을 남길 수 없습니다. CoinData를 포함한 RegisterTrade를 사용하세요.");
        }
    }

    // [수정됨] tradeHistory.Clear()를 제거하여 기록 유지
    public void SwitchInterval(ChartInterval interval) {
        currentInterval = interval;
        ClearChart(); // 시각적 캔들 객체만 초기화

        // [중요] tradeHistory.Clear(); <-- 이 줄을 삭제했기 때문에 기록이 유지됩니다.
        // 단, 4H와 1D의 인덱스는 다르므로 1D로 바꾸면 마커 위치가 안 맞을 수 있습니다.
        // (완벽한 해결을 위해선 Timestamp가 필요하지만, 현재 요청하신 '유지' 기능은 이것으로 충분합니다)

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
    // ... (Zoom, HLine, Measurement 관련 함수들은 기존과 동일, 생략하지 않고 구조 유지) ...

    public void OnZoomInBtn() { ApplyZoom(buttonZoomStep); }
    public void OnZoomOutBtn() { ApplyZoom(-buttonZoomStep); }

    public void ToggleHLineMode() {
        isPlacingHLine = !isPlacingHLine;
        if (btnHLine != null) btnHLine.GetComponent<Image>().color = isPlacingHLine ? activeBtnColor : inactiveBtnColor;
        if (isPlacingHLine && isMeasurementMode) ToggleMeasurementMode();
    }

    public void ToggleMeasurementMode() {
        isMeasurementMode = !isMeasurementMode;
        if (btnMeasure != null) btnMeasure.GetComponent<Image>().color = isMeasurementMode ? activeBtnColor : inactiveBtnColor;
        if (isMeasurementMode && isPlacingHLine) ToggleHLineMode();
    }
    private void CreateHorizontalLineAtMouse() {
        if (hLinePrefab == null) return;
        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out localPoint)) return;
        double price = YToPrice(localPoint.y);
        Transform overlayParent = crosshairV.parent;
        GameObject go = Instantiate(hLinePrefab, overlayParent);
        go.transform.SetAsLastSibling();
        HorizontalLineView view = go.GetComponent<HorizontalLineView>();
        if (view != null) {
            view.Setup(this, price, hLineColor);
            view.UpdatePosition(PriceToY(price));
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
        return currentVisibleMinPrice + (normalized * range);
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

    protected virtual void Update() {
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

        // ... (나머지 Update 로직 그대로 유지) ...
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || isMeasurementMode) {
            HandleMeasurementInput();
        } else {
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

        UpdateTradeMarkers();
    }
    private void HandleMeasurementInput() {
        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
        if (Input.GetMouseButtonDown(0) && !isMeasuring) {
            if (IsPointerOverButton(btnMeasure, cam)) return;
            if (RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam)) {
                isMeasuring = true;
                followLatest = false;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(chartContent, Input.mousePosition, cam, out measureStartPos);
                Vector2 viewportLocalPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out viewportLocalPos);
                measureStartPrice = YToPrice(viewportLocalPos.y);
                if (currentMeasurement != null) Destroy(currentMeasurement.gameObject);
                if (measurementPrefab != null) {
                    GameObject go = Instantiate(measurementPrefab, chartContent);
                    currentMeasurement = go.GetComponent<MeasurementView>();
                    currentMeasurement.Setup(measureStartPos);
                }
            }
        }
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
        if (Input.GetMouseButtonUp(0)) {
            if (isMeasuring) {
                isMeasuring = false;
                if (currentMeasurement != null) { Destroy(currentMeasurement.gameObject); currentMeasurement = null; }
                if (isMeasurementMode) ToggleMeasurementMode();
            }
        }
    }
    private void HandleHorizontalLineInput() {
        if (Input.GetKeyDown(KeyCode.H) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) {
            Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
            if (RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam)) CreateHorizontalLineAtMouse();
        }
        if (isPlacingHLine && Input.GetMouseButtonDown(0)) {
            Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
            if (!IsPointerOverButton(btnHLine, cam) && !IsPointerOverButton(btn4H, cam) && !IsPointerOverButton(btn1D, cam) && RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam)) {
                CreateHorizontalLineAtMouse();
                ToggleHLineMode();
            }
        }
    }
    void SetupCrosshairs() {
        if (crosshairV != null) { crosshairV.gameObject.SetActive(false); }
        if (crosshairH != null) { crosshairH.gameObject.SetActive(false); }
    }
    void HandleCursorTracking() {
        if (crosshairV == null || crosshairH == null) return;
        Camera cam = null;
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera || parentCanvas.renderMode == RenderMode.WorldSpace) cam = parentCanvas.worldCamera;
        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, cam);
        if (crosshairV.gameObject.activeSelf != isInside) crosshairV.gameObject.SetActive(isInside);
        if (crosshairH.gameObject.activeSelf != isInside) crosshairH.gameObject.SetActive(isInside);
        if (!isInside) return;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out localPoint);
        crosshairV.anchoredPosition = new Vector2(localPoint.x, 0f);
        crosshairH.anchoredPosition = new Vector2(0f, localPoint.y);
    }
    protected virtual void UpdateCurrentPriceLine() {
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
            priceTagImage.color = (currentPrice >= openPrice) ? colorHigh : colorLow;
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
            if (IsPointerOverButton(btn4H, cam) || IsPointerOverButton(btn1D, cam) || IsPointerOverButton(btnZoomIn, cam) || IsPointerOverButton(btnZoomOut, cam) || IsPointerOverButton(btnHLine, cam) || IsPointerOverButton(btnMeasure, cam)) return;
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
                if (!isDragging && Mathf.Abs(chartContent.anchoredPosition.x - GetLatestDataScrollX()) < candleSpacing * 0.5f) followLatest = true;
            }
        }
        if (Input.GetMouseButtonUp(0)) isDragging = false;
    }
    private bool IsPointerOverButton(Button btn, Camera cam) {
        if (btn == null || !btn.gameObject.activeInHierarchy) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(btn.GetComponent<RectTransform>(), Input.mousePosition, cam);
    }
    private void SnapToLatest() {
        if (candles.Count == 0) return;
        float expectedWidth = (candles.Count + futureEmptyCandles) * candleSpacing;
        if (Mathf.Abs(chartContent.sizeDelta.x - expectedWidth) > 1f) chartContent.sizeDelta = new Vector2(expectedWidth, chartContent.sizeDelta.y);
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

    // [신규] 거래 마커(B/S) 위치 및 활성화 업데이트
    // [수정됨] 4H/1D 차트 타입에 맞춰 번지수를 제대로 찾아가도록 수정
    // [수정됨] 현재 보고 있는 코인(coinSymbol)의 장부만 가져와서 그리기
    private void UpdateTradeMarkers() {
        if (tradeMarkerPrefab == null) return;
        if (targetCoin == null) return; // 코인 정보 없으면 중단

        // 현재 코인의 장부가 아예 없으면 그릴 것도 없음
        if (!allCoinTradeHistory.ContainsKey(coinSymbol)) {
            // 마커 모두 끄고 리턴
            foreach (var m in tradeMarkerPool) m.gameObject.SetActive(false);
            return;
        }

        // 현재 코인의 기록만 가져옴
        var myHistory = allCoinTradeHistory[coinSymbol];
        int usedMarkerCount = 0;

        // 줌 비율에 따른 폰트 크기 계산
        float zoomRatio = Mathf.InverseLerp(minCandleSpacing, maxCandleSpacing, candleSpacing);
        float currentFontSize = Mathf.Lerp(minMarkerFontSize, maxMarkerFontSize, zoomRatio);

        // 현재 보이는 캔들 범위 내에서만 루프
        for (int i = visibleStartIndex; i <= visibleEndIndex; i++) {
            if (i < 0 || i >= candles.Count) continue;

            var candle = candles[i];
            TradeType flags = TradeType.None;

            if (currentInterval == ChartInterval._4H) {
                // 4H: 1대1 매칭
                myHistory.TryGetValue(i, out flags);
            } else {
                // 1D: 6개 합치기
                int startRawIndex = i * 6;
                for (int k = 0; k < 6; k++) {
                    if (myHistory.TryGetValue(startRawIndex + k, out TradeType f)) {
                        flags |= f;
                    }
                }
            }

            if (flags == TradeType.None) continue;

            float xPos = (i * candleSpacing) + tradeMarkerXOffset;

            // Buy Check
            if ((flags & TradeType.Buy) != 0) {
                var marker = GetMarkerFromPool(usedMarkerCount++);
                marker.text = "B";
                marker.color = buyMarkerColor;
                marker.fontSize = currentFontSize;

                RectTransform rt = marker.rectTransform;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);

                float yPos = PriceToY(candle.Low) - tradeMarkerYOffset;
                rt.anchoredPosition = new Vector2(xPos, yPos);

                marker.gameObject.SetActive(true);
            }

            // Sell Check
            if ((flags & TradeType.Sell) != 0) {
                var marker = GetMarkerFromPool(usedMarkerCount++);
                marker.text = "S";
                marker.color = sellMarkerColor;
                marker.fontSize = currentFontSize;

                RectTransform rt = marker.rectTransform;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);

                float yPos = PriceToY(candle.High) + tradeMarkerYOffset;
                rt.anchoredPosition = new Vector2(xPos, yPos);

                marker.gameObject.SetActive(true);
            }
        }

        // 남은 마커 비활성화
        for (int i = usedMarkerCount; i < tradeMarkerPool.Count; i++) {
            if (tradeMarkerPool[i].gameObject.activeSelf)
                tradeMarkerPool[i].gameObject.SetActive(false);
        }
    }

    // [신규] 마커 풀링 시스템
    private TextMeshProUGUI GetMarkerFromPool(int index) {
        // 풀이 모자라면 생성
        while (tradeMarkerPool.Count <= index) {
            GameObject go = Instantiate(tradeMarkerPrefab, tradeMarkerContainer);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tradeMarkerPool.Add(tmp);
        }
        return tradeMarkerPool[index];
    }

    // ... (UpdateHorizontalLines, GridLabels, FormatPrice, HighLowIndicators, PriceToY, HandleCandleBoundary, TickPrice, UpdateCurrentCandle 등 나머지 기존 로직 유지) ...
    protected virtual void UpdateHorizontalLines() {
        if (activeHLines.Count == 0) return;
        foreach (var line in activeHLines) {
            if (line != null) {
                float yPos = PriceToY(line.TargetPrice);
                line.UpdatePosition(yPos);
            }
        }
    }
    protected virtual void UpdateGridLabels(double minPrice, double maxPrice) {
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
    // [수정됨] 줌 레벨에 따라 텍스트와 캔들 사이의 거리를 자동 조절
    protected virtual void UpdateHighLowIndicators(int highIndex, double highPrice, int lowIndex, double lowPrice) {
        // 기존: float leftGap = candleSpacing * 1.0f; (필요 없음)

        // [핵심] 현재 줌 상태(candleSpacing)에 비례하여 Y축 간격 조절
        // candleSpacing이 클수록(줌인) 멀리, 작을수록(줌아웃) 가깝게
        // 기본값 30f는 candleSpacing이 10f일 때 기준이라고 가정하고 비율 적용
        float dynamicYOffset = indicatorYOffset * (candleSpacing / 10f);

        // 너무 딱 붙거나 너무 멀어지지 않게 최소/최대값 제한 (취향껏 조절 가능)
        dynamicYOffset = Mathf.Clamp(dynamicYOffset, 15f, 60f);

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

                // X 위치: 해당 캔들의 정중앙
                float xPos = (index * candleSpacing) + indicatorXOffset;

                // Y 위치: 캔들 끝(High/Low) + 동적 오프셋
                float yBase = PriceToY(price);
                float yPos = isHigh ? (yBase + dynamicYOffset) : (yBase - dynamicYOffset);

                rt.anchoredPosition = new Vector2(xPos, yPos);
            } else tmp.gameObject.SetActive(false);
        }

        SetupIndicator(highPriceText, highIndex, highPrice, true);
        SetupIndicator(lowPriceText, lowIndex, lowPrice, false);
    }
    protected virtual float PriceToY(double price) {
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
            if (time.Hour == 9 && time.Minute == 0) SwitchInterval(ChartInterval._1D);
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
    protected virtual void UpdateCurrentCandle() {
        if (currentCandle == null || currentCandle.IsClosed) return;
        currentCandle.UpdatePrice(priceDriver.displayPrice);
    }
    protected virtual void UpdatePriceLabel(double price) {
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

        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
        Vector2 localMousePos;

        // 1. 뷰포트 내부에서의 마우스 위치를 구함
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, cam, out localMousePos)) {

            // [핵심 1] 줌 하기 전, "컨텐츠 시작점(0)"에서 "마우스"까지의 거리 계산
            float oldContentX = chartContent.anchoredPosition.x;
            float mouseOffsetFromOrigin = localMousePos.x - oldContentX;

            // [핵심 2] 줌 실행 (spacing 변경)
            float oldSpacing = candleSpacing;
            ApplyZoom(scroll * zoomSensitivity);
            float newSpacing = candleSpacing;

            // 간격이 실제로 변했을 때만 위치 보정 수행
            if (Mathf.Abs(newSpacing - oldSpacing) > 0.001f) {
                // [핵심 3] 확대/축소 비율 계산 (예: 10 -> 20이면 2배)
                float zoomRatio = newSpacing / oldSpacing;

                // [핵심 4] 비율에 맞춰 새로운 거리 계산
                // 예: 거리가 100이었는데 2배 줌되면 200이 되어야 함
                float newMouseOffset = mouseOffsetFromOrigin * zoomRatio;

                // [핵심 5] 마우스 커서 위치는 화면에 고정되어야 하므로,
                // 늘어난 거리만큼 컨텐츠 시작점(X)을 뒤로 밀어줌
                float newContentX = localMousePos.x - newMouseOffset;

                // 범위 제한 (너무 멀리 스크롤되지 않게)
                float maxScrollX = maxPastScrollCandles * candleSpacing;
                float minScrollX = GetMaxFutureScrollX();
                newContentX = Mathf.Clamp(newContentX, minScrollX, maxScrollX);

                // 위치 적용
                chartContent.anchoredPosition = new Vector2(newContentX, 0f);

                // 마우스로 줌을 당겼다는 건 특정 지점을 보고 싶다는 뜻이므로 '최신 따라가기' 해제
                followLatest = false;
            }
        }
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