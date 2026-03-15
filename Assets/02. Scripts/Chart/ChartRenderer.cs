using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using System;

public class ChartRenderer : MonoBehaviour {
    [System.Serializable]
    public class CandleData {
        public double open;
        public double close;
        public double high;
        public double low;
        public TradeType TradeFlag;
        public DateTime Timestamp;
    }

    [Header("References")]
    public RectTransform content;
    public GameObject candlePrefab;
    public ScrollRect scrollRect;
    public RectTransform viewport;
    public RectTransform crosshairV, crosshairH;
    public Camera uiCamera;
    public TextMeshProUGUI currentPriceFixedLabel;

    [Header("Chart Settings")]
    public int maxVisibleCandles = 30;
    public float candleSpacing = 6f;
    public float candleWidth = 6f;
    public float wickWidth = 2f;
    public float chartHeight = 80f;
    public float chartPaddingBottom = 30f;
    public float chartPaddingLeft = 20f;

    [HideInInspector] public List<CandleData> candleDataList = new();
    private CoinData targetCoin;

    private double minPrice;
    private double maxPrice;
    private double minViewPrice;
    private double priceRange;
    private double latestClosePrice = 0;

    private bool chartNeedsRender = false;

    IEnumerator Start() {
        SetupCrosshairs();
        yield return null;

        if (candleDataList.Count > 0) {
            chartNeedsRender = true;
            yield return null;
        }
    }

    public void SetDataAndRender(CoinData coin, List<CandleData> newData) {
        targetCoin = coin;
        candleDataList = newData;
        chartNeedsRender = true;
    }

    void Update() {
        if (chartNeedsRender) {
            RenderCandles();
            chartNeedsRender = false;
        }

        HandleZoom();
        HandleCursorTracking();

        if (targetCoin == null || currentPriceFixedLabel == null) return;

        double price = targetCoin.CurrentPrice;

        if (price >= 1000)
            currentPriceFixedLabel.text = $"현재가: {price:N0} KRW";
        else if (price >= 100)
            currentPriceFixedLabel.text = $"현재가: {price:N1} KRW";
        else if (price >= 10)
            currentPriceFixedLabel.text = $"현재가: {price:N2} KRW";
        else if (price >= 1)
            currentPriceFixedLabel.text = $"현재가: {price:N3} KRW";
        else
            currentPriceFixedLabel.text = $"현재가: {price:N4} KRW";
    }

    void SetupCrosshairs() {
        if (crosshairV != null) {
            crosshairV.anchorMin = new Vector2(0f, 0f);
            crosshairV.anchorMax = new Vector2(0f, 1f);
            crosshairV.pivot = new Vector2(0.5f, 0.5f);
            crosshairV.sizeDelta = new Vector2(1f, 0f);
        }

        if (crosshairH != null) {
            crosshairH.anchorMin = new Vector2(0f, 1f);
            crosshairH.anchorMax = new Vector2(1f, 1f);
            crosshairH.pivot = new Vector2(0.5f, 0.5f);
            crosshairH.sizeDelta = new Vector2(0f, 1f);
            crosshairH.anchoredPosition = new Vector2(0f, 0f);
        }
    }

    public void RenderCandles() {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        if (candleDataList == null || candleDataList.Count == 0) return;

        int start = Math.Max(0, candleDataList.Count - maxVisibleCandles);
        List<CandleData> visible = candleDataList.GetRange(start, candleDataList.Count - start);

        float contentWidth = chartPaddingLeft + visible.Count * candleSpacing;
        content.sizeDelta = new Vector2(contentWidth, content.sizeDelta.y);

        minPrice = double.MaxValue;
        maxPrice = double.MinValue;
        foreach (var c in visible) {
            minPrice = Math.Min(minPrice, c.low);
            maxPrice = Math.Max(maxPrice, c.high);
        }

        double centerPrice = (maxPrice + minPrice) / 2.0;
        priceRange = Math.Max(0.000001, centerPrice * 0.1);
        minViewPrice = centerPrice - priceRange / 2.0;

        for (int i = 0; i < visible.Count; i++) {
            CandleData data = visible[i];
            GameObject candleObj = Instantiate(candlePrefab, content);
            RectTransform rt = candleObj.GetComponent<RectTransform>();

            float x = chartPaddingLeft + i * candleSpacing;
            rt.anchoredPosition = new Vector2(x, 0f);

            RectTransform wick = candleObj.transform.Find("Wick")?.GetComponent<RectTransform>();
            if (wick != null) {
                float wickTop = (float)((data.high - minViewPrice) / priceRange * chartHeight);
                float wickBottom = (float)((data.low - minViewPrice) / priceRange * chartHeight);
                float wickMidY = chartPaddingBottom + (wickTop + wickBottom) / 2f;

                wick.sizeDelta = new Vector2(wickWidth, wickTop - wickBottom);
                wick.anchoredPosition = new Vector2(0f, wickMidY);
            }

            RectTransform body = candleObj.transform.Find("Body")?.GetComponent<RectTransform>();
            if (body != null) {
                float openY = (float)((data.open - minViewPrice) / priceRange * chartHeight);
                float closeY = (float)((data.close - minViewPrice) / priceRange * chartHeight);
                float bodyHeight = Mathf.Max(1f, Mathf.Abs(closeY - openY));
                float bodyMidY = chartPaddingBottom + (openY + closeY) / 2f;

                body.sizeDelta = new Vector2(candleWidth, bodyHeight);
                body.anchoredPosition = new Vector2(0f, bodyMidY);

                Image bodyImage = body.GetComponent<Image>();
                if (bodyImage != null)
                    bodyImage.color = (data.close >= data.open) ? Color.green : Color.red;
            }
        }

        if (visible.Count > 0)
            latestClosePrice = visible[^1].close;

        if (scrollRect != null) {
            Canvas.ForceUpdateCanvases();
            scrollRect.horizontalNormalizedPosition = 1f;
        }
    }

    void HandleZoom() {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f) {
            chartHeight += scroll * 200f;
            chartHeight = Mathf.Clamp(chartHeight, 10f, 300f);
            chartNeedsRender = true;
        }
    }

    void HandleCursorTracking() {
        if (candleDataList == null || candleDataList.Count == 0) return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, uiCamera)) {
            crosshairV?.gameObject.SetActive(false);
            crosshairH?.gameObject.SetActive(false);
            return;
        }

        crosshairV?.gameObject.SetActive(true);
        crosshairH?.gameObject.SetActive(true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, Input.mousePosition, uiCamera, out Vector2 localPoint);

        if (crosshairV != null) {
            crosshairV.anchoredPosition = new Vector2(localPoint.x, 0f);
            crosshairV.sizeDelta = new Vector2(1f, viewport.rect.height);
        }

        if (crosshairH != null) {
            crosshairH.anchoredPosition = new Vector2(0f, localPoint.y);
            crosshairH.sizeDelta = new Vector2(content.rect.width, 1f);
        }
    }
}
