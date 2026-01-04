using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;

public class LiveChartRenderer : MonoBehaviour {
    [Header("Refs")]
    public RectTransform chartContent;
    public RectTransform viewport;
    public GameObject candlePrefab;

    [Header("Drivers")]
    public ChartPriceDriver priceDriver;

    [Header("UI")]
    public TextMeshProUGUI priceInfoLabel;

    [Header("Chart")]
    public float chartHeight = 200f;
    public float chartPaddingBottom = 20f;
    public float candleSpacing = 10f;

    // ===== Runtime =====
    private CoinData targetCoin;
    private RuntimeCandle currentCandle;
    private CandleView currentCandleView;

    private double minViewPrice;
    private double priceRange;

    private bool initialized = false;
    private string coinName;
    private string coinSymbol;


    private List<RuntimeCandle> candles = new();
    private List<CandleView> candleViews = new();
    private int candleIndex = 0;

    private double viewMinPrice;
    private double viewMaxPrice;
    private bool lastPausedState = false;
    public void Initialize(CoinData coin) {

        // 이전 코인의 진행 중 캔들 확정
        if (targetCoin != null) {
            targetCoin.RecordCurrentCandle();
        }

        ClearChart();

        CoinManager.Instance.OnCandleBoundary -= HandleCandleBoundary;

        targetCoin = coin;
        coinName = coin.Name;
        coinSymbol = coin.Symbol;

        priceDriver.Initialize(coin.CurrentPrice);

        for (int i = 0; i < coin.CandleHistory.Count; i++) {
            CreateCandleFromHistory(coin.CandleHistory[i]);
        }

        CreateNewCandle(priceDriver.displayPrice);

        initialized = true;

        CoinManager.Instance.OnCandleBoundary += HandleCandleBoundary;
    }

    private void Update() {
        if (!initialized || targetCoin == null)
            return;

        bool paused = CoinManager.Instance.IsTimePaused;

        // 전이 감지
        if (paused != lastPausedState) {
            if (paused) {
                priceDriver.Freeze();
            } else {
                priceDriver.Resume();
            }
            lastPausedState = paused;
        }

        // 완전 정지
        if (paused)
            return;

        TickPrice();
        UpdateCurrentCandle();
        Render();
    }

    private void UpdatePriceLabel(double price) {
        if (priceInfoLabel == null)
            return;

        string priceText;

        if (price >= 1000)
            priceText = price.ToString("N0");
        else if (price >= 100)
            priceText = price.ToString("N1");
        else if (price >= 10)
            priceText = price.ToString("N2");
        else if (price >= 1)
            priceText = price.ToString("N3");
        else
            priceText = price.ToString("N4");

        priceInfoLabel.text = $"{coinName}({coinSymbol}) {priceText}원";
    }

    // 가격 >> Y 좌표 변환
    private float PriceToY(double price) {
        double min = double.MaxValue;
        double max = double.MinValue;

        int start = Mathf.Max(0, candles.Count - 20);
        for (int i = start; i < candles.Count; i++) {
            min = Math.Min(min, candles[i].Low);
            max = Math.Max(max, candles[i].High);
        }

        // 패딩 10%
        double padding = (max - min) * 0.1;
        min -= padding;
        max += padding;

        double range = Math.Max(1e-6, max - min);
        float normalized = (float)((price - min) / range);

        return Mathf.Lerp(-chartHeight * 0.5f, chartHeight * 0.5f, normalized);
    }


    void HandleCandleBoundary(DateTime time) {
        if (currentCandle == null)
            return;

        // 이전 봉 Close 확정
        currentCandle.CloseCandle();

        //이전 봉의 Close를 Open으로 사용
        double nextOpen = currentCandle.Close;

        CreateNewCandle(nextOpen);
    }

    void CreateNewCandle(double startPrice) {
        // RuntimeCandle
        var candle = new RuntimeCandle();
        candle.Start(startPrice);
        candles.Add(candle);

        // View 생성
        var go = Instantiate(candlePrefab, chartContent);
        var rt = go.GetComponent<RectTransform>();

        float x = candleIndex * candleSpacing;
        rt.anchoredPosition = new Vector2(x, 0f);

        var view = go.GetComponent<CandleView>();
        view.PriceToY = PriceToY;

        candleViews.Add(view);

        // 현재 캔들 갱신
        currentCandle = candle;
        currentCandleView = view;

        candleIndex++;

        UpdateChartWidth();
    }
    void UpdateChartWidth() {
        float width = Mathf.Max(viewport.rect.width, candleIndex * candleSpacing);
        chartContent.sizeDelta = new Vector2(width, chartContent.sizeDelta.y);
    }

    void TickPrice() {
        priceDriver.SetTargetPrice(targetCoin.CurrentPrice);
        priceDriver.Tick(Time.deltaTime);
    }

    void UpdateCurrentCandle() {
        if (currentCandle == null || currentCandle.IsClosed)
            return;

        currentCandle.UpdatePrice(priceDriver.displayPrice);
    }
    void Render() {
        UpdatePriceLabel(targetCoin.CurrentPrice);

        for (int i = 0; i < candleViews.Count; i++) {
            candleViews[i].UpdateView(candles[i]);
        }
    }
    void CreateCandleFromHistory(ChartRenderer.CandleData data) {
        var candle = new RuntimeCandle(
            data.open,
            data.high,
            data.low,
            data.close
        );

        candles.Add(candle);

        var go = Instantiate(candlePrefab, chartContent);
        go.GetComponent<RectTransform>().anchoredPosition =
            new Vector2(candleIndex * candleSpacing, 0f);

        var view = go.GetComponent<CandleView>();
        view.PriceToY = PriceToY;

        candleViews.Add(view);
        candleIndex++;
    }


    void ClearChart() {
        foreach (var view in candleViews) {
            if (view != null)
                Destroy(view.gameObject);
        }

        candles.Clear();
        candleViews.Clear();
        candleIndex = 0;

        currentCandle = null;
        currentCandleView = null;
        initialized = false;
    }
}
