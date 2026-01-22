using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// 선물 거래소용 차트 렌더러 (다중 포지션, 달러 표시, 청산 로직 통합)
/// </summary>
public class FutureChartRenderer : LiveChartRenderer {

    public static FutureChartRenderer Instance;

    public enum MarginMode { Cross, Isolated }
    public MarginMode currentMarginMode = MarginMode.Cross;

    [System.Serializable]
    public class FuturePosition {
        public string Symbol;
        public bool IsLong;
        public MarginMode Mode; // 이 포지션이 어떤 모드로 잡혔는지 저장
        public double EntryPriceUSD;
        public double Quantity;
        public double MarginUSD;
        public float Leverage;
        public double LiquidationPriceUSD;
    }

    [Header("Future Selector UI")]
    public GameObject coinListPanel;
    public Transform listParent;
    public GameObject coinRowPrefab;

    [Header("Margin Mode UI")]
    public Button modeButton;       // Cross/Isolated 전환 버튼
    public TextMeshProUGUI modeText; // 버튼 텍스트

    [Header("Selected Coin Display")]
    public TextMeshProUGUI selectedCoinNameText;
    public Image selectedCoinIcon;

    [Header("Save Key")]
    public string lastUsedSymbolKey = "LastFutureSymbol";

    [Header("Order System UI")]
    public Slider orderPercentageSlider;
    public TMP_InputField orderAmountInput;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI maxQtyText;

    [Header("Position List UI")]
    public Transform positionListParent;
    public GameObject positionPrefab;

    [Header("Leverage Settings")]
    public float currentLeverage = 5f;

    [Header("Position Controls")]
    public Button btnLong;
    public Button btnShort;

    [Header("Liquidation And Lines")]
    public GameObject liqLinePrefab;
    private GameObject activeLiqLine;
    private GameObject activeEntryLine;

    // [핵심] 다중 포지션 관리
    private List<FuturePosition> activePositions = new List<FuturePosition>();
    private Dictionary<FuturePosition, FuturePositionUI> uiMap = new Dictionary<FuturePosition, FuturePositionUI>();

    private List<CoinData> coinList;
    private Dictionary<GameObject, CoinData> rowDataMap = new();
    private List<GameObject> rowPool = new List<GameObject>();
    private bool isRefreshing = false;

    private class FutureRowRef {
        public TextMeshProUGUI priceText;
        public TextMeshProUGUI changeText;
    }
    private Dictionary<GameObject, FutureRowRef> rowRefMap = new();

    private void Awake() {
        Instance = this;
        if (btnLong != null) btnLong.onClick.AddListener(() => OpenPosition(true));
        if (btnShort != null) btnShort.onClick.AddListener(() => OpenPosition(false));

        // [신규] 모드 전환 버튼 리스너
        if (modeButton != null) modeButton.onClick.AddListener(OnClickToggleMode);
    }

    private void OnEnable() {
        StopAllCoroutines();
        StartCoroutine(WaitAndSetup());
    }

    private void Start() {
        if (orderPercentageSlider != null) {
            orderPercentageSlider.minValue = 0;
            orderPercentageSlider.maxValue = 100;
            orderPercentageSlider.wholeNumbers = true;
            orderPercentageSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        UpdateModeUI(); // 초기 UI 설정
    }

    public void OnClickToggleMode() {
        // 포지션이 하나라도 있으면 변경 불가 (안전장치)
        if (activePositions.Count > 0) return;

        currentMarginMode = (currentMarginMode == MarginMode.Cross) ? MarginMode.Isolated : MarginMode.Cross;
        UpdateModeUI();
    }

    // [신규] 모드 UI 갱신
    private void UpdateModeUI() {
        if (modeText != null) {
            modeText.text = currentMarginMode.ToString();
        }
        // 포지션이 있으면 버튼 비활성화, 없으면 활성화
        if (modeButton != null) {
            modeButton.interactable = (activePositions.Count == 0);
        }
    }

    // 헬퍼: 타겟 코인 달러가 가져오기
    public double GetCurrentTargetPriceUSD(string symbol) {
        var coin = CoinManager.Instance.coins.Find(c => c.Symbol == symbol);
        return (coin != null) ? coin.CurrentPrice / GlobalEconomyManager.UsdToKrw : 0;
    }

    public string FormatPriceUSD(double price) {
        if (price >= 1000) return $"${price:N2}";
        else if (price >= 1) return $"${price:N2}";
        else return $"${price:N4}";
    }

    private IEnumerator WaitAndSetup() {
        while (CoinManager.Instance == null) yield return null;
        yield return new WaitForSeconds(0.1f);
        SetupFuturePanel();
    }

    private double CalculateLiquidationPrice(double entryPrice, double quantity, double walletBalance, double usedMargin, bool isLong, MarginMode mode) {
        double liqPrice = 0;

        if (mode == MarginMode.Isolated) {
            // [Isolated] 격리: 증거금의 90% 손실 시 청산 (기존 동일)
            double priceDiff = (entryPrice / currentLeverage) * 0.9;
            liqPrice = isLong ? (entryPrice - priceDiff) : (entryPrice + priceDiff);
        } else {
            // [Cross] 교차: (남은 잔고 + 현재 포지션 증거금) 전액을 잃을 때까지 버팀
            // *수정됨*: walletBalance(잔고) + usedMargin(증거금) = 총 담보
            double totalCollateral = walletBalance + usedMargin;
            double priceDiff = totalCollateral / quantity;

            liqPrice = isLong ? (entryPrice - priceDiff) : (entryPrice + priceDiff);
        }

        if (liqPrice < 0) liqPrice = 0;
        return liqPrice;
    }

    public void SetupFuturePanel() {
        if (CoinManager.Instance == null || CoinManager.Instance.coins == null) return;
        coinList = CoinManager.Instance.coins
            .Where(c => !c.IsDelisted && CoinMetaDatabase.AllCoins.Any(meta => meta.Symbol == c.Symbol && meta.FournanceListed))
            .ToList();
        string lastSymbol = PlayerPrefs.GetString(lastUsedSymbolKey, "BTC");
        CoinData lastCoin = coinList.Find(c => c.Symbol == lastSymbol) ?? (coinList.Count > 0 ? coinList[0] : null);
        if (lastCoin != null) SelectCoin(lastCoin);
        if (coinListPanel != null) coinListPanel.SetActive(false);
    }

    public void SelectCoin(CoinData coin) {
        if (coin == null) return;
        if (selectedCoinNameText != null) selectedCoinNameText.text = $"{coin.Name} ({coin.Symbol})";
        if (selectedCoinIcon != null) {
            Sprite s = Resources.Load<Sprite>($"Coins/{coin.Symbol}");
            if (s != null) selectedCoinIcon.sprite = s;
        }
        StopAllCoroutines();
        base.Initialize(coin);
        if (orderPercentageSlider != null) orderPercentageSlider.value = 0;
        OnSliderValueChanged(0);
        if (coinListPanel != null) coinListPanel.SetActive(false);
        RefreshPositionLines();
    }

    public void ToggleCoinList() {
        if (coinListPanel == null) return;
        bool isActive = !coinListPanel.activeSelf;
        coinListPanel.SetActive(isActive);
        if (isActive) StartCoroutine(SafeRefreshList());
    }

    public void OnSliderValueChanged(float value) {
        if (targetCoin == null || PlayerManager.Instance == null) return;
        if (orderAmountInput != null) orderAmountInput.text = value.ToString("F0");

        double walletBalanceUsd = PlayerManager.Instance.fournanceCash;
        double inputMarginUsd = walletBalanceUsd * (value / 100.0);
        double totalOrderValueUsd = inputMarginUsd * currentLeverage;
        double currentPriceUsd = targetCoin.CurrentPrice / GlobalEconomyManager.UsdToKrw;
        double orderQty = (currentPriceUsd > 0) ? (totalOrderValueUsd / currentPriceUsd) : 0;

        if (costText != null) costText.text = $"COST: ${inputMarginUsd:N2} / ${totalOrderValueUsd:N2}";
        if (maxQtyText != null) maxQtyText.text = $"Size: {orderQty:F4} {targetCoin.Symbol}";
    }

    public void OpenPosition(bool isLong) {
        if (targetCoin == null || orderPercentageSlider.value <= 0) return;

        double walletBalanceUsd = PlayerManager.Instance.fournanceCash;
        double marginUsd = walletBalanceUsd * (orderPercentageSlider.value / 100.0);
        if (marginUsd <= 0) return;

        double currentEntryPrice = targetCoin.CurrentPrice / GlobalEconomyManager.UsdToKrw;
        double totalValueUsd = marginUsd * currentLeverage;
        double quantity = totalValueUsd / currentEntryPrice;

        FuturePosition existingPos = activePositions.Find(p => p.Symbol == targetCoin.Symbol);

        if (existingPos != null) {
            // [One-Way: 합산 로직]
            if (existingPos.IsLong != isLong) {
                Debug.LogError($"[One-Way] 반대 포지션 보유 중. 진입 불가.");
                return;
            }

            PlayerManager.Instance.fournanceCash -= marginUsd;

            double oldTotalValue = existingPos.Quantity * existingPos.EntryPriceUSD;
            double addTotalValue = quantity * currentEntryPrice;
            double newAvgEntry = (oldTotalValue + addTotalValue) / (existingPos.Quantity + quantity);

            existingPos.EntryPriceUSD = newAvgEntry;
            existingPos.Quantity += quantity;
            existingPos.MarginUSD += marginUsd; // 마진 합산

            // [수정] 청산가 재계산 시 '총 마진(existingPos.MarginUSD)'을 전달
            double remainingBalance = PlayerManager.Instance.fournanceCash;
            existingPos.LiquidationPriceUSD = CalculateLiquidationPrice(
                newAvgEntry,
                existingPos.Quantity,
                remainingBalance,
                existingPos.MarginUSD, // <--- 여기 추가!
                isLong,
                currentMarginMode
            );

            if (uiMap.TryGetValue(existingPos, out var ui)) ui.UpdateRealtime();
            Debug.Log($"[{existingPos.Symbol}] Merged: Avg ${newAvgEntry:N2}");
        } else {
            // [신규 진입]
            PlayerManager.Instance.fournanceCash -= marginUsd;
            double remainingBalance = PlayerManager.Instance.fournanceCash; // 100% 진입 시 0원이 됨

            // [수정] 여기서 remainingBalance가 0이어도 marginUsd를 더해서 계산하므로 안전함
            double liqPriceUsd = CalculateLiquidationPrice(
                currentEntryPrice,
                quantity,
                remainingBalance,
                marginUsd, // <--- 여기 추가!
                isLong,
                currentMarginMode
            );

            FuturePosition newPos = new FuturePosition {
                Symbol = targetCoin.Symbol,
                IsLong = isLong,
                Mode = currentMarginMode,
                EntryPriceUSD = currentEntryPrice,
                Quantity = quantity,
                MarginUSD = marginUsd,
                Leverage = currentLeverage,
                LiquidationPriceUSD = liqPriceUsd
            };

            activePositions.Add(newPos);
            // ... (프리팹 생성 등 기존 코드 동일)
            GameObject go = Instantiate(positionPrefab, positionListParent);
            FuturePositionUI ui = go.GetComponent<FuturePositionUI>();
            ui.Setup(newPos, this);
            uiMap[newPos] = ui;

            Debug.Log($"[{newPos.Symbol}] Open {newPos.Mode}: Liq ${liqPriceUsd:F2}");
        }

        RefreshPositionLines();
        UpdateModeUI();
    }

    public void ClosePositionMarket(FuturePosition pos, bool isLiquidated = false) {
        if (!activePositions.Contains(pos)) return;

        double currentPriceUsd = GetCurrentTargetPriceUSD(pos.Symbol);
        double priceDiff = pos.IsLong ? (currentPriceUsd - pos.EntryPriceUSD) : (pos.EntryPriceUSD - currentPriceUsd);
        double pnlUsd = priceDiff * pos.Quantity;

        if (isLiquidated) {
            // [강제 청산]
            if (pos.Mode == MarginMode.Cross) {
                // Cross: 잔고 0원 파산 (끝까지 버티다 죽음)
                PlayerManager.Instance.fournanceCash = 0;
                Debug.LogError($"[Cross 청산] {pos.Symbol} 파산! 잔고 소멸.");
            } else {
                // Isolated: 90% 손실 시점에 청산되지만, 남은 10%는 돌려주지 않음 (몰수)
                // 지갑 잔고(fournanceCash)는 건드리지 않음
                Debug.LogError($"[Isolated 청산] {pos.Symbol} 증거금 ${pos.MarginUSD:N2} 전액 소멸 (잔고 생존).");
            }
        } else {
            // [정상 종료] (익절/손절)
            // 원금 + PnL을 지갑에 반환
            double returnAmount = pos.MarginUSD + pnlUsd;
            PlayerManager.Instance.fournanceCash += returnAmount;
            Debug.Log($"[종료] 정산금: ${returnAmount:F2}");
        }

        // 파산 방어
        if (PlayerManager.Instance.fournanceCash < 0.0001) PlayerManager.Instance.fournanceCash = 0;

        // UI 및 데이터 정리
        if (uiMap.TryGetValue(pos, out var ui)) {
            Destroy(ui.gameObject);
            uiMap.Remove(pos);
        }
        activePositions.Remove(pos);

        RefreshPositionLines();
        UpdateModeUI();
    }

    public double CalculateTotalUnrealizedPnL() {
        double totalPnL = 0;
        foreach (var pos in activePositions) {
            double currentPrice = GetCurrentTargetPriceUSD(pos.Symbol);
            if (currentPrice <= 0) continue;
            double priceDiff = pos.IsLong ? (currentPrice - pos.EntryPriceUSD) : (pos.EntryPriceUSD - currentPrice);
            totalPnL += priceDiff * pos.Quantity;
        }
        return totalPnL;
    }

    private void RefreshPositionLines() {
        // 기존 선들 싹 지우고 시작
        if (activeLiqLine != null) Destroy(activeLiqLine);
        if (activeEntryLine != null) Destroy(activeEntryLine);

        if (targetCoin == null) return;

        // 현재 보고 있는 코인의 내 포지션 찾기
        var myPos = activePositions.FindLast(p => p.Symbol == targetCoin.Symbol);

        if (myPos != null) {
            // 1. 청산가 라인 (빨강)
            CreateHorizontalLine(myPos.LiquidationPriceUSD, Color.red, ref activeLiqLine);

            // 2. [추가됨] 진입가 라인 (초록)
            CreateHorizontalLine(myPos.EntryPriceUSD, new Color32(0, 255, 0, 255), ref activeEntryLine);
        }
    }


    private void CreateHorizontalLine(double priceUsd, Color color, ref GameObject lineObj) {
        if (hLinePrefab != null && crosshairV != null) { // hLinePrefab은 부모 클래스에 있음
            GameObject go = Instantiate(hLinePrefab, crosshairV.parent);
            HorizontalLineView view = go.GetComponent<HorizontalLineView>();
            if (view != null) {
                double priceKrw = priceUsd * GlobalEconomyManager.UsdToKrw;
                view.Setup(this, priceKrw, color);
                lineObj = go;
            }
        }
    }

    public double CalculateTotalUsedMargin() {
        double totalMargin = 0;
        foreach (var pos in activePositions) totalMargin += pos.MarginUSD;
        return totalMargin;
    }

    //private void RefreshLiquidationLineForCurrentCoin() {
    //    if (activeLiqLine != null) Destroy(activeLiqLine);
    //    if (targetCoin == null) return;
    //    var myPos = activePositions.FindLast(p => p.Symbol == targetCoin.Symbol);
    //    if (myPos != null) CreateLiquidationLine(myPos.LiquidationPriceUSD);
    //}

    private void CreateLiquidationLine(double priceUsd) {
        if (activeLiqLine != null) Destroy(activeLiqLine);
        if (hLinePrefab != null && crosshairV != null) {
            GameObject go = Instantiate(hLinePrefab, crosshairV.parent);
            HorizontalLineView view = go.GetComponent<HorizontalLineView>();
            if (view != null) {
                double priceKrw = priceUsd * GlobalEconomyManager.UsdToKrw;
                view.Setup(this, priceKrw, Color.red);
                activeLiqLine = go;
            }
        }
    }
    protected override void Update() {
        if (coinListPanel != null && coinListPanel.activeSelf) {
            HandleExternalClick();
            if (EventSystem.current.IsPointerOverGameObject()) UpdateListOnly();
        }

        base.Update();

        for (int i = activePositions.Count - 1; i >= 0; i--) {
            var pos = activePositions[i];
            if (uiMap.TryGetValue(pos, out var ui)) ui.UpdateRealtime();

            double currentPriceUsd = GetCurrentTargetPriceUSD(pos.Symbol);
            if (currentPriceUsd <= 0) continue;

            bool isLiq = pos.IsLong ? (currentPriceUsd <= pos.LiquidationPriceUSD) : (currentPriceUsd >= pos.LiquidationPriceUSD);
            if (isLiq) ClosePositionMarket(pos, isLiquidated: true);
        }

        UpdateListOnly();
        if (targetCoin != null && orderPercentageSlider != null && orderPercentageSlider.value > 0) {
            OnSliderValueChanged(orderPercentageSlider.value);
        }
    }

    protected override void UpdateHorizontalLines() {
        base.UpdateHorizontalLines();

        // 1. 청산가 라인 위치 갱신
        if (activeLiqLine != null && activePositions.Count > 0 && targetCoin != null) {
            var currentPos = activePositions.FindLast(p => p.Symbol == targetCoin.Symbol);
            if (currentPos != null) {
                double liqKrw = currentPos.LiquidationPriceUSD * GlobalEconomyManager.UsdToKrw;
                var view = activeLiqLine.GetComponent<HorizontalLineView>();
                if (view != null) view.UpdatePosition(PriceToY(liqKrw));
            }
        }

        // 2. [추가됨] 진입가 라인 위치 갱신
        if (activeEntryLine != null && activePositions.Count > 0 && targetCoin != null) {
            var currentPos = activePositions.FindLast(p => p.Symbol == targetCoin.Symbol);
            if (currentPos != null) {
                double entryKrw = currentPos.EntryPriceUSD * GlobalEconomyManager.UsdToKrw;
                var view = activeEntryLine.GetComponent<HorizontalLineView>();
                if (view != null) view.UpdatePosition(PriceToY(entryKrw));
            }
        }
    }

    private IEnumerator SafeRefreshList() {
        isRefreshing = true;
        rowRefMap.Clear();
        rowDataMap.Clear();

        coinList = CoinManager.Instance.coins
            .Where(c => !c.IsDelisted && CoinMetaDatabase.AllCoins.Any(meta => meta.Symbol == c.Symbol && meta.FournanceListed))
            .ToList();

        int coinCount = coinList.Count;
        int poolCount = rowPool.Count;
        int maxCount = Mathf.Max(coinCount, poolCount);

        for (int i = 0; i < maxCount; i++) {
            GameObject row;
            if (i < coinCount) {
                if (i < poolCount) row = rowPool[i];
                else { row = Instantiate(coinRowPrefab, listParent); rowPool.Add(row); }
                row.SetActive(true);
                SetupRow(row, coinList[i]);
            } else {
                if (i < poolCount && rowPool[i] != null) rowPool[i].SetActive(false);
            }
        }
        isRefreshing = false;
        yield return null;
    }

    private void SetupRow(GameObject row, CoinData coin) {
        Transform iconTr = row.transform.Find("Image");
        Transform nameTr = row.transform.Find("Name");
        Transform priceTr = row.transform.Find("Price");
        Transform changeTr = row.transform.Find("Change");

        if (nameTr == null || priceTr == null || changeTr == null) return;

        rowDataMap[row] = coin;
        FutureRowRef refs = new FutureRowRef {
            priceText = priceTr.GetComponent<TextMeshProUGUI>(),
            changeText = changeTr.GetComponent<TextMeshProUGUI>()
        };
        rowRefMap[row] = refs;

        nameTr.GetComponent<TextMeshProUGUI>().text = coin.Symbol + "USD";
        if (iconTr != null) {
            Image iconImg = iconTr.GetComponent<Image>();
            if (iconImg != null) iconImg.sprite = Resources.Load<Sprite>($"Coins/{coin.Symbol}");
        }

        Button btn = row.GetComponent<Button>();
        if (btn != null) {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectCoin(coin));
        }
    }

    private void HandleExternalClick() {
        if (Input.GetMouseButtonDown(0)) {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool clickedInside = false;
            foreach (var result in results) {
                if (result.gameObject.transform.IsChildOf(coinListPanel.transform)) {
                    clickedInside = true;
                    break;
                }
            }
            if (!clickedInside) coinListPanel.SetActive(false);
        }
    }

    private void UpdateListOnly() {
        if (!isRefreshing && coinListPanel != null && coinListPanel.activeSelf && rowRefMap.Count > 0) {
            double usdToKrw = GlobalEconomyManager.UsdToKrw;
            var keys = rowDataMap.Keys.ToList();

            foreach (var row in keys) {
                if (row == null || !row.activeInHierarchy) continue;
                if (rowDataMap.TryGetValue(row, out CoinData coin) && rowRefMap.TryGetValue(row, out var refs)) {
                    if (refs.priceText != null) {
                        double priceUsd = coin.CurrentPrice / usdToKrw;
                        refs.priceText.text = FormatPriceUSD(priceUsd);
                    }
                    if (refs.changeText != null) {
                        double change = coin.InitialPrice > 0 ? ((coin.CurrentPrice - coin.InitialPrice) / coin.InitialPrice) * 100.0 : 0;
                        refs.changeText.text = $"{change:+0.##;-0.##}%";
                        refs.changeText.color = change > 0 ? new Color32(50, 214, 149, 255) : (change < 0 ? new Color32(230, 60, 60, 255) : Color.white);
                    }
                }
            }
        }
    }

    protected override void UpdateGridLabels(double minPriceKrw, double maxPriceKrw) {
        if (gridLabels == null || gridLabels.Count == 0 || targetCoin == null) return;
        double usdToKrw = GlobalEconomyManager.UsdToKrw;
        double rangeKrw = maxPriceKrw - minPriceKrw;
        double stepKrw = rangeKrw / (gridLabels.Count - 1);

        for (int i = 0; i < gridLabels.Count; i++) {
            double targetPriceKrw = minPriceKrw + (stepKrw * i);
            gridLabels[i].text = FormatPriceUSD(targetPriceKrw / usdToKrw);
            float yPos = PriceToY(targetPriceKrw);
            gridLabels[i].rectTransform.anchoredPosition = new Vector2(0, yPos);
        }
    }

    protected override void UpdatePriceLabel(double priceKrw) {
        if (priceInfoLabel != null && targetCoin != null) {
            double priceUsd = priceKrw / GlobalEconomyManager.UsdToKrw;
            priceInfoLabel.text = $"{targetCoin.Name}({targetCoin.Symbol}) {FormatPriceUSD(priceUsd)}";
        }
    }

    protected override void UpdateCurrentPriceLine() {
        base.UpdateCurrentPriceLine();
        if (priceTagText != null && priceDriver != null && targetCoin != null) {
            double currentPriceUsd = priceDriver.displayPrice / GlobalEconomyManager.UsdToKrw;
            priceTagText.text = FormatPriceUSD(currentPriceUsd);
        }
    }

    protected override void UpdateHighLowIndicators(int highIndex, double highPriceKrw, int lowIndex, double lowPriceKrw) {
        if (targetCoin == null) return;
        double usdToKrw = GlobalEconomyManager.UsdToKrw;
        float dynamicYOffset = indicatorYOffset * (candleSpacing / 10f);
        dynamicYOffset = Mathf.Clamp(dynamicYOffset, 15f, 60f);

        void SetupFutureIndicator(TextMeshProUGUI tmp, int index, double priceKrw, bool isHigh) {
            if (tmp == null) return;
            if (index != -1) {
                tmp.gameObject.SetActive(true);
                double priceUsd = priceKrw / usdToKrw;
                tmp.text = FormatPriceUSD(priceUsd);
                tmp.color = isHigh ? new Color32(50, 214, 149, 255) : new Color32(230, 60, 60, 255);
                tmp.alignment = TextAlignmentOptions.Center;
                var rt = tmp.rectTransform;
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(0, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                float xPos = (index * candleSpacing) + indicatorXOffset;
                float yBase = PriceToY(priceKrw);
                float yPos = isHigh ? (yBase + dynamicYOffset) : (yBase - dynamicYOffset);
                rt.anchoredPosition = new Vector2(xPos, yPos);
            } else tmp.gameObject.SetActive(false);
        }
        SetupFutureIndicator(highPriceText, highIndex, highPriceKrw, true);
        SetupFutureIndicator(lowPriceText, lowIndex, lowPriceKrw, false);
    }
}