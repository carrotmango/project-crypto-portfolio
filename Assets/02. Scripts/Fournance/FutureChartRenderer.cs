using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using UnityEngine.EventSystems;
using System.Runtime.CompilerServices;

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
    public Button btnCloseAll;

    [Header("Liquidation And Lines")]
    public GameObject liqLinePrefab;
    private GameObject activeLiqLine;
    private GameObject activeEntryLine;

    [Header("Leverage UI System")]
    public GameObject leveragePanelObj;
    public Button openLeveragePanelButton;      // 메인 화면의 [ 5x ] 버튼
    public TextMeshProUGUI mainLeverageText;    // 버튼 안의 텍스트 ("5x")
    public LeverageSelectorUI leveragePanelScript; // 방금 만든 패널 스크립트 연결

    [Header("Chart Info Header")]
    public TextMeshProUGUI headerCurrentPriceText;

    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f;

    // [핵심] 다중 포지션 관리
    private List<FuturePosition> activePositions = new List<FuturePosition>();
    private Dictionary<FuturePosition, FuturePositionUI> uiMap = new Dictionary<FuturePosition, FuturePositionUI>();

    private List<CoinData> coinList;
    private Dictionary<GameObject, CoinData> rowDataMap = new();
    private List<GameObject> rowPool = new List<GameObject>();
    private bool isRefreshing = false;
    private double lastHeaderPrice = 0;
    private const double TRADING_FEE_RATE = 0.00036;
    public double TradingFeeRate => TRADING_FEE_RATE;
    protected override TradeType AllowedMarkerTypes => TradeType.FutureBuy | TradeType.FutureSell;

    private class FutureRowRef {
        public TextMeshProUGUI priceText;
        public TextMeshProUGUI changeText;
    }
    private Dictionary<GameObject, FutureRowRef> rowRefMap = new();

    private void Awake() {
        Instance = this;

        // 1. 롱/숏 버튼 연결
        if (btnLong != null) btnLong.onClick.AddListener(() => OpenPosition(true));
        if (btnShort != null) btnShort.onClick.AddListener(() => OpenPosition(false));

        // 2. 모드 전환 버튼
        if (modeButton != null) modeButton.onClick.AddListener(OnClickToggleMode);

        // [중요 수정] 부모의 Awake가 가려져서 실행되지 않으므로, 여기서 차트 버튼을 직접 연결해야 합니다.
        // LiveChartRenderer에 정의된 btn4H, btn1D를 사용합니다.
        if (btn4H != null) {
            btn4H.onClick.RemoveAllListeners();
            btn4H.onClick.AddListener(() => SwitchInterval(ChartInterval._4H));
        }
        if (btn1D != null) {
            btn1D.onClick.RemoveAllListeners();
            btn1D.onClick.AddListener(() => SwitchInterval(ChartInterval._1D));
        }
        // 줌 버튼 등 부모 기능도 여기서 연결
        if (btnZoomIn != null) btnZoomIn.onClick.AddListener(OnZoomInBtn);
        if (btnZoomOut != null) btnZoomOut.onClick.AddListener(OnZoomOutBtn);
        if (btnHLine != null) btnHLine.onClick.AddListener(ToggleHLineMode);
        if (btnMeasure != null) btnMeasure.onClick.AddListener(ToggleMeasurementMode);



        if (leveragePanelObj != null) leveragePanelObj.SetActive(false);
        if (btnCloseAll != null) {
            btnCloseAll.onClick.AddListener(OnClickCloseAll);
        }
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
        UpdateLeverageButtonText();
    }

    public void OnClickToggleMode() {
        // 포지션이 하나라도 있으면 변경 불가 (안전장치)
        if (activePositions.Count > 0) return;

        currentMarginMode = (currentMarginMode == MarginMode.Cross) ? MarginMode.Isolated : MarginMode.Cross;
        UpdateModeUI();
    }

    // [신규] 레버리지 패널 열기
    public void OpenLeveragePanel() {
        if (activePositions.Count > 0) {
            Debug.LogWarning("포지션 보유 중에는 레버리지를 변경할 수 없습니다.");
            return;
        }

        // 1. 패널 오브젝트를 강제로 켭니다.
        if (leveragePanelObj != null) {
            leveragePanelObj.SetActive(true);
        }

        // 2. 그 다음 스크립트 초기화 함수를 부릅니다.
        if (leveragePanelScript != null) {
            leveragePanelScript.Show(this, currentLeverage);
        }
    }

    // [신규] 패널에서 호출할 함수 (값 적용)
    public void SetLeverage(float newLev) {
        currentLeverage = newLev;
        UpdateLeverageButtonText();

        // 완료 누르면 패널 끄기 (혹시 LeverageSelectorUI에서 안 끌까봐 여기서도 처리)
        if (leveragePanelObj != null) leveragePanelObj.SetActive(false);
    }

    private void UpdateLeverageButtonText() {
        if (mainLeverageText != null) {
            mainLeverageText.text = $"{currentLeverage:F0}x";
        }
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
        if (selectedCoinNameText != null) selectedCoinNameText.text = $"{coin.Symbol}USD";
        if (selectedCoinIcon != null) {
            Sprite s = Resources.Load<Sprite>($"Coins/{coin.Symbol}");
            if (s != null) selectedCoinIcon.sprite = s;
        }
        StopAllCoroutines();
        lastHeaderPrice = 0;
        if (headerCurrentPriceText != null) headerCurrentPriceText.color = Color.white;
        base.Initialize(coin);
        UpdateHeaderPriceText();
        if (orderPercentageSlider != null) orderPercentageSlider.value = 0;
        OnSliderValueChanged(0);
        if (coinListPanel != null) coinListPanel.SetActive(false);
        RefreshPositionLines();
        CreateProceduralGridTexture();
    }

    public void ToggleCoinList() {
        if (coinListPanel == null) return;
        bool isActive = !coinListPanel.activeSelf;
        coinListPanel.SetActive(isActive);
        if (isActive) StartCoroutine(SafeRefreshList());
    }

    public double GetCrossMarginEquity() {
        double equity = PlayerManager.Instance.fournanceCash;

        foreach (var pos in activePositions) {
            if (pos.Mode == MarginMode.Cross) {
                equity += pos.MarginUSD;

                double currentPrice = GetCurrentTargetPriceUSD(pos.Symbol);
                if (currentPrice > 0) {
                    double priceDiff = pos.IsLong ? (currentPrice - pos.EntryPriceUSD) : (pos.EntryPriceUSD - currentPrice);
                    double grossPnL = priceDiff * pos.Quantity;

                    // Equity 계산 시에도 예상 종료 수수료를 미리 뺍니다 (Net PnL)
                    double estimatedExitFee = (pos.Quantity * currentPrice) * TRADING_FEE_RATE;

                    equity += (grossPnL - estimatedExitFee);
                }
            }
        }
        return equity;
    }

    public void OnSliderValueChanged(float value) {
        if (targetCoin == null || PlayerManager.Instance == null) return;
        if (orderAmountInput != null) orderAmountInput.text = $"{value:F0}%";

        double buyingPower = GetBuyingPower();

        // 1. 투입 예정 총 금액 (지갑에서 빠져나갈 돈)
        double inputTotalUsd = buyingPower * (value / 100.0);

        // [수정] 2. 수수료 역산 (Fee Inclusive)
        // 공식: 투입금 = 실제증거금 + (실제증거금 * 레버리지 * 수수료율)
        double actualMargin = inputTotalUsd / (1.0 + currentLeverage * TRADING_FEE_RATE);

        // 예상 진입 수수료
        double estimatedEntryFee = inputTotalUsd - actualMargin;

        double totalOrderValueUsd = actualMargin * currentLeverage;
        double currentPriceUsd = targetCoin.CurrentPrice / GlobalEconomyManager.UsdToKrw;
        double orderQty = (currentPriceUsd > 0) ? (totalOrderValueUsd / currentPriceUsd) : 0;

        // 비용과 수수료를 같이 보여줌
        if (costText != null) {
            string feeTextStr = LocalizationManager.GetText("LBL_FEE");
            costText.text = $"COST: ${inputTotalUsd:N2} ({feeTextStr}: ${estimatedEntryFee:N2})";
        }

        if (maxQtyText != null)
            maxQtyText.text = $"Size: {orderQty:F4} {targetCoin.Symbol}";
    }

    public void OpenPosition(bool isLong) {
        if (targetCoin == null || orderPercentageSlider.value <= 0) return;

        double buyingPower = GetBuyingPower();
        double inputTotalUsd = buyingPower * (orderPercentageSlider.value / 100.0);

        // 최소 1달러는 있어야 진입
        if (inputTotalUsd <= 1.0) {
            Debug.LogWarning("주문 금액이 너무 적습니다.");
            return;
        }

        double currentEntryPrice = GetCurrentTargetPriceUSD(targetCoin.Symbol);
        if (currentEntryPrice <= 0) return;

        // -----------------------------------------------------------
        // [핵심] 수수료 먼저 떼기 (Fee Inclusive)
        // -----------------------------------------------------------
        double actualMargin = inputTotalUsd / (1.0 + currentLeverage * TRADING_FEE_RATE);
        double entryFee = inputTotalUsd - actualMargin;

        double totalValueUsd = actualMargin * currentLeverage;
        double quantity = totalValueUsd / currentEntryPrice;

        // 지갑에서는 '수수료 포함된 금액' 차감
        PlayerManager.Instance.fournanceCash -= inputTotalUsd;

        if (targetCoin != null) {
            targetCoin.MarkTradeOnCurrentCandle(isLong ? TradeType.FutureBuy : TradeType.FutureSell);
        }

        FuturePosition existingPos = activePositions.Find(p => p.Symbol == targetCoin.Symbol);

        if (existingPos != null) {
            // [물타기 로직]
            if (existingPos.IsLong != isLong) {
                Debug.LogError($"[One-Way] 반대 포지션 보유 중.");
                PlayerManager.Instance.fournanceCash += inputTotalUsd; // 환불
                return;
            }

            // 평단가 갱신
            double oldVal = existingPos.Quantity * existingPos.EntryPriceUSD;
            double addVal = quantity * currentEntryPrice;
            existingPos.EntryPriceUSD = (oldVal + addVal) / (existingPos.Quantity + quantity);

            existingPos.Quantity += quantity;
            existingPos.MarginUSD += actualMargin; // 수수료 뗀 '알맹이'만 추가

            // 청산가 갱신
            existingPos.LiquidationPriceUSD = CalculateLiquidationPrice(
                existingPos.EntryPriceUSD, existingPos.Quantity, PlayerManager.Instance.fournanceCash, existingPos.MarginUSD, isLong, currentMarginMode
            );

            if (uiMap.TryGetValue(existingPos, out var ui)) ui.UpdateRealtime();
        } else {
            // [신규 진입]
            double liqPrice = CalculateLiquidationPrice(
                currentEntryPrice, quantity, PlayerManager.Instance.fournanceCash, actualMargin, isLong, currentMarginMode
            );

            FuturePosition newPos = new FuturePosition {
                Symbol = targetCoin.Symbol,
                IsLong = isLong,
                Mode = currentMarginMode,
                EntryPriceUSD = currentEntryPrice,
                Quantity = quantity,
                MarginUSD = actualMargin, // 수수료 뗀 금액
                Leverage = currentLeverage,
                LiquidationPriceUSD = liqPrice
            };

            activePositions.Add(newPos);
            double tradeVolume = totalValueUsd; // 레버리지 포함 총 진입 가치
            PlayerManager.Instance.fournanceTotalVolume += tradeVolume;
            PlayerManager.Instance.fournanceTotalFee += entryFee;


            GameObject go = Instantiate(positionPrefab, positionListParent);
            FuturePositionUI ui = go.GetComponent<FuturePositionUI>();
            ui.Setup(newPos, this);
            uiMap[newPos] = ui;
        }

        RefreshPositionLines();
        UpdateModeUI();

        Debug.Log($"[진입] 지출: ${inputTotalUsd:F2} (수수료: -${entryFee:F2} / 증거금: ${actualMargin:F2})");
    }

    public void ClosePositionMarket(FuturePosition pos, bool isLiquidated = false) {
        // 1. 안전장치: 이미 리스트에 없으면 중단
        if (!activePositions.Contains(pos)) return;

        // 2. 현재가 확인
        double currentPriceUsd = GetCurrentTargetPriceUSD(pos.Symbol);

        // 3. 순수 차트 손익 (Gross PnL) 계산
        double priceDiff = pos.IsLong ? (currentPriceUsd - pos.EntryPriceUSD) : (pos.EntryPriceUSD - currentPriceUsd);
        double grossPnL = priceDiff * pos.Quantity;

        // 4. [핵심] 종료 수수료 계산 (Exit Fee)
        // 공식: 종료 시점의 포지션 총 가치 * 수수료율(0.036%)
        double exitPositionValue = pos.Quantity * currentPriceUsd;
        double exitFee = exitPositionValue * TRADING_FEE_RATE; // 0.00036

        // 5. 청산 여부에 따른 분기 처리
        if (isLiquidated) {
            // [강제 청산] - 수수료고 뭐고 전액 몰수이므로 별도 계산 안 함
            double currentCash = PlayerManager.Instance.fournanceCash;
            double actualLoss = 0;

            if (pos.Mode == MarginMode.Cross) {
                // Cross: 진입한 증거금 + 남은 지갑 잔고 전부 몰수 (파산)
                actualLoss = pos.MarginUSD + currentCash;
                PlayerManager.Instance.fournanceCash = 0;
                Debug.LogError($"[Cross 청산] {pos.Symbol} 파산! 총 손실: -${actualLoss:N2}");
            } else {
                // Isolated: 진입한 증거금만 몰수 (지갑 잔고는 안전)
                actualLoss = pos.MarginUSD;
                Debug.LogError($"[Isolated 청산] {pos.Symbol} 증거금 -${actualLoss:N2} 전액 소멸.");
            }

            // 알림 발송
            string sender = "Fournance Risk Team";
            string noticeLabel = LocalizationManager.GetText("LBL_NOTICE");
            string liqNoticeFmt = LocalizationManager.GetText("MSG_LIQ_NOTICE");
            string shortMsg = $"[{noticeLabel}] " + string.Format(liqNoticeFmt, $"{pos.Symbol}USD");

            string liqLabel = LocalizationManager.GetText("LBL_LIQUIDATION");
            string lossLabel = LocalizationManager.GetText("LBL_LOSS");
            string balLabel = LocalizationManager.GetText("LBL_BALANCE");
            string senderLabel = LocalizationManager.GetText("LBL_SENDER");

            string fullBody = $"[{liqLabel}] {pos.Symbol}USD ({(pos.IsLong ? "Long" : "Short")})\n";
            fullBody += $"Entry ${pos.EntryPriceUSD:N4} / Liq ${pos.LiquidationPriceUSD:N4}\n";
            fullBody += $"{lossLabel} -${actualLoss:N2} ({balLabel} ${PlayerManager.Instance.fournanceCash:N2})";

            GlobalNotificationManager.Instance.ShowNotification(
                "Bullbit", sender, shortMsg,
                () => {
                    if (UIManager.Instance != null) {
                        UIManager.Instance.ShowSMSResult($"{senderLabel}: {sender}\n\n{fullBody}");
                    }
                }
            );

            // =========================================================
            // [추가] 통계 데이터 기록
            // =========================================================

            // 1. 거래량 누적 (종료 시점의 총 가치)
            double exitVol = pos.Quantity * currentPriceUsd;
            PlayerManager.Instance.fournanceTotalVolume += exitVol;

            // 2. 실현 손익 & 수수료 누적
            if (isLiquidated) {
                // [강제 청산]
                // Cross면 (증거금 + 남은잔고), Isolated면 (증거금) 만큼 손실 확정
                double lossAmount = (pos.Mode == MarginMode.Cross) ? (pos.MarginUSD + PlayerManager.Instance.fournanceCash) : pos.MarginUSD;

                // 실현 손익 깎기
                PlayerManager.Instance.fournanceRealizedPnL -= lossAmount;

                // 청산은 보통 수수료보다는 보험기금으로 가지만, 통계상 수수료에 포함시킬지 여부는 선택 (여기선 패스)
            }


        } else {
            // [정상 종료] (익절/손절)
            // 공식: 돌려받을 돈 = 내 원금(증거금) + 차트수익(GrossPnL) - 종료수수료(ExitFee)
            double returnAmount = pos.MarginUSD + grossPnL - exitFee;

            PlayerManager.Instance.fournanceCash += returnAmount;

            double netPnL = grossPnL - exitFee; // 순수익 (수수료 뺀거)
            PlayerManager.Instance.fournanceRealizedPnL += netPnL; // 누적!

            // 수수료도 누적
            PlayerManager.Instance.fournanceTotalFee += exitFee;

            Debug.Log($"[종료] 차트손익: ${grossPnL:F2} | 수수료: -${exitFee:F2} | 최종정산금: ${returnAmount:F2}");
        }

        // 6. 파산 방어 (부동소수점 오차로 -0.000001 같은거 방지)
        if (PlayerManager.Instance.fournanceCash < 0.0001) PlayerManager.Instance.fournanceCash = 0;

        CoinData coinToMark = CoinManager.Instance.coins.Find(c => c.Symbol == pos.Symbol);
        if (coinToMark != null) {
            coinToMark.MarkTradeOnCurrentCandle(pos.IsLong ? TradeType.FutureSell : TradeType.FutureBuy);
        }

        // 7. UI 및 데이터 정리
        if (uiMap.TryGetValue(pos, out var ui)) {
            Destroy(ui.gameObject);
            uiMap.Remove(pos);
        }
        activePositions.Remove(pos);

        RefreshPositionLines();
        UpdateModeUI();
    }

    public double CalculateTotalUnrealizedPnL() {
        double totalNetPnL = 0;
        foreach (var pos in activePositions) {
            double currentPrice = GetCurrentTargetPriceUSD(pos.Symbol);
            if (currentPrice <= 0) continue;

            double priceDiff = pos.IsLong ? (currentPrice - pos.EntryPriceUSD) : (pos.EntryPriceUSD - currentPrice);
            double grossPnL = priceDiff * pos.Quantity;

            // 총 PnL도 수수료 차감 후 계산 (Net PnL)
            double estimatedExitFee = (pos.Quantity * currentPrice) * TRADING_FEE_RATE;
            totalNetPnL += (grossPnL - estimatedExitFee);
        }
        return totalNetPnL;
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

    //private void CreateLiquidationLine(double priceUsd) {
    //    if (activeLiqLine != null) Destroy(activeLiqLine);
    //    if (hLinePrefab != null && crosshairV != null) {
    //        GameObject go = Instantiate(hLinePrefab, crosshairV.parent);
    //        HorizontalLineView view = go.GetComponent<HorizontalLineView>();
    //        if (view != null) {
    //            double priceKrw = priceUsd * GlobalEconomyManager.UsdToKrw;
    //            view.Setup(this, priceKrw, Color.red);
    //            activeLiqLine = go;
    //        }
    //    }
    //}
    protected override void Update() {
        if (leveragePanelObj != null && leveragePanelObj.activeSelf) return;

        if (coinListPanel != null && coinListPanel.activeSelf) {
            HandleExternalClick();
            if (EventSystem.current.IsPointerOverGameObject()) UpdateListOnly();
        }

        if (Input.GetMouseButtonDown(0)) {
            float timeSinceLastClick = Time.time - lastClickTime;

            if (timeSinceLastClick <= doubleClickThreshold) {
                CheckAndRemoveHorizontalLineViaRaycast(); // ✨ 레이캐스트 삭제 방식 호출
            }
            lastClickTime = Time.time;
        }

        base.Update();
        UpdateHeaderPriceText();


        // ------------------------------------------------------------------
        // [핵심] 현재 내 '찐' 전재산(Equity) 계산
        // 현금이 마이너스여도 PnL이 플러스면 Equity는 플러스임.
        // ------------------------------------------------------------------
        double currentEquity = 0;
        if (activePositions.Count > 0) {
            currentEquity = GetCrossMarginEquity();
        } else {
            currentEquity = PlayerManager.Instance.fournanceCash;
        }

        for (int i = activePositions.Count - 1; i >= 0; i--) {
            var pos = activePositions[i];
            if (uiMap.TryGetValue(pos, out var ui)) ui.UpdateRealtime();

            double currentPriceUsd = GetCurrentTargetPriceUSD(pos.Symbol);
            if (currentPriceUsd <= 0) continue;

            double realTimeLiqPrice = pos.LiquidationPriceUSD;

            if (pos.Mode == MarginMode.Cross) {
                // [수정됨] PlayerManager.fournanceCash 대신 currentEquity 사용
                // 공식: 전재산(Equity)이 0이 되는 가격을 찾음
                // Equity가 곧 나의 총알(Total Collateral)임

                // 내 전재산(Equity)을 포지션 수량으로 나누면 -> "가격이 얼마나 변해야 내 돈 다 잃나?"가 나옴
                double priceRoom = currentEquity / pos.Quantity;

                if (pos.IsLong) {
                    // 롱: 현재가에서 여유분만큼 떨어진 곳이 청산가
                    // (주의: EntryPrice가 아니라 CurrentPrice 기준이어야 실시간 Equity 반영이 정확함)
                    realTimeLiqPrice = currentPriceUsd - priceRoom;
                    if (realTimeLiqPrice < 0) realTimeLiqPrice = 0;
                } else {
                    // 숏: 현재가에서 여유분만큼 오른 곳이 청산가
                    realTimeLiqPrice = currentPriceUsd + priceRoom;
                }

                // UI 표기용 업데이트
                pos.LiquidationPriceUSD = realTimeLiqPrice;
            }

            // 청산 체크
            bool isLiq = pos.IsLong ? (currentPriceUsd <= realTimeLiqPrice) : (currentPriceUsd >= realTimeLiqPrice);

            // [추가 안전장치] Equity가 0 이하면 가격 상관없이 즉시 청산 (파산)
            if (pos.Mode == MarginMode.Cross && currentEquity <= 0.0001) {
                isLiq = true;
            }

            if (isLiq) {
                ClosePositionMarket(pos, isLiquidated: true);
            }
        }

        UpdateListOnly();
        if (targetCoin != null && orderPercentageSlider != null && orderPercentageSlider.value > 0) {
            OnSliderValueChanged(orderPercentageSlider.value);
        }
        UpdateGridBackgroundUV();
    }


    private void CheckAndRemoveHorizontalLineViaRaycast() {
        if (activeHLines == null || activeHLines.Count == 0) return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current) {
            position = Input.mousePosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults) {
            // 마우스에 걸린 모든 UI 오브젝트 중에 HorizontalLineView가 달려있는 놈이 있다면!
            HorizontalLineView clickedLine = result.gameObject.GetComponentInParent<HorizontalLineView>();

            if (clickedLine != null) {
                // 부모 클래스의 제거 함수 호출
                RemoveHorizontalLine(clickedLine);
                Debug.Log("[시스템] UI 관통 레이캐스트: 수평선 삭제 성공!");
                return; // 하나 지웠으면 끝
            }
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

    // 상단 텍스트 갱신 함수
    private void UpdateHeaderPriceText() {
        if (headerCurrentPriceText != null && targetCoin != null) {
            double currentPriceUsd = targetCoin.CurrentPrice / GlobalEconomyManager.UsdToKrw;

            // 가격 변화가 없으면 굳이 텍스트/색상 갱신 안 함 (최적화)
            if (System.Math.Abs(currentPriceUsd - lastHeaderPrice) < 0.0000001) return;

            headerCurrentPriceText.text = FormatPriceUSD(currentPriceUsd);

            // 이전 가격이 0이 아닐 때만 비교 (처음 켜질 땐 흰색 유지)
            if (lastHeaderPrice > 0) {
                if (currentPriceUsd > lastHeaderPrice) {
                    // 상승: 초록 (#32D695) -> RGB(50, 214, 149)
                    headerCurrentPriceText.color = new Color32(50, 214, 149, 255);
                } else if (currentPriceUsd < lastHeaderPrice) {
                    // 하락: 빨강 (#E63C3C) -> RGB(230, 60, 60)
                    headerCurrentPriceText.color = new Color32(230, 60, 60, 255);
                }
            } else {
                // 처음엔 흰색
                headerCurrentPriceText.color = Color.white;
            }

            // 현재 가격 저장
            lastHeaderPrice = currentPriceUsd;
        }
    }

    public double GetBuyingPower() {
        // 1. 교차 모드 기준 총 담보 가치 (Cash + Cross Margin + Cross PnL)
        // 여기에는 Isolated 포지션의 PnL이 포함되지 않음 (위에서 수정했으므로)
        double totalCrossEquity = GetCrossMarginEquity();

        // 2. [핵심 수정] 이미 사용 중인 증거금을 뺄 때도 "Cross 포지션" 것만 빼야 함.
        // 왜냐? Isolated 증거금은 이미 fournanceCash에서 영구 차감되었고, 
        // totalCrossEquity 계산할 때 더해주지도 않았으니, 여기서 또 빼면 이중 차감이 됨.

        double usedCrossMargin = 0;
        foreach (var pos in activePositions) {
            if (pos.Mode == MarginMode.Cross) {
                usedCrossMargin += pos.MarginUSD;
            }
        }

        // 3. 구매력 = (Cash + Cross PnL)
        // 수식: (Cash + CrossMargin + CrossPnL) - CrossMargin
        double buyingPower = totalCrossEquity - usedCrossMargin;

        return buyingPower > 0 ? buyingPower : 0;
    }

    public void OnClickCloseAll() {
        if (activePositions.Count == 0) return;

        // [중요] 리스트 요소를 삭제하는 로직이므로, 반드시 '역순(뒤에서부터)'으로 돌려야 함
        // 앞에서부터(0부터) 지우면 인덱스가 밀려서 에러 나거나 건너뜀
        int count = activePositions.Count;

        for (int i = count - 1; i >= 0; i--) {
            var pos = activePositions[i];

            // 기존에 잘 만들어둔 'ClosePositionMarket' 함수 재활용
            // isLiquidated = false (정상 종료)
            ClosePositionMarket(pos, isLiquidated: false);
        }

        Debug.Log($"[시스템] 포지션 {count}개 일괄 종료 완료");

        // (선택사항) "모든 포지션이 정리되었습니다" 알림 띄우기
        if (GlobalNotificationManager.Instance != null) {
            GlobalNotificationManager.Instance.ShowNotification(
                "Bullbit",
                "Fournance",
                LocalizationManager.GetText("MSG_ALL_POS_CLOSED"),
                null
            );
        }
    }
    private void CreateProceduralGridTexture() {
        if (gridBackground == null) return;

        // 1. 텍스처 생성
        Texture2D texture = new Texture2D(gridWidth, gridHeight, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;

        // 2. 초기화 (투명하게)
        Color[] cols = new Color[gridWidth * gridHeight];
        for (int i = 0; i < cols.Length; i++) {
            cols[i] = Color.clear;
        }

        // 3. 실선 그리기 (조건문 삭제)

        // [가로선 그리기] 맨 아래 (y=0) 라인 싹 다 칠하기
        for (int x = 0; x < gridWidth; x++) {
            cols[x] = gridColor;
        }

        // [세로선 그리기] 맨 왼쪽 (x=0) 라인 싹 다 칠하기
        for (int y = 0; y < gridHeight; y++) {
            // 인덱스 = y * 가로길이
            cols[y * gridWidth] = gridColor;
        }

        // 4. 적용
        texture.SetPixels(cols);
        texture.Apply();

        gridBackground.texture = texture;

        // 5. UV 세팅
        float repeatX = viewport.rect.width / gridWidth;
        float repeatY = viewport.rect.height / gridHeight;
        gridBackground.uvRect = new Rect(0, 0, repeatX, repeatY);

        // *중요* 스크롤 동기화 변수 업데이트
        gridTextureWidth = gridWidth;
    }

    private void UpdateGridBackgroundUV() {
        if (gridBackground == null || gridTextureWidth <= 0) return;

        // 1. 차트의 현재 스크롤 위치(X)를 격자 너비로 나눠서 이동량 계산
        float uvX = -(chartContent.anchoredPosition.x / gridTextureWidth);

        // 2. 화면(Viewport) 너비 대비 반복 횟수 계산
        float uvWidth = viewport.rect.width / gridTextureWidth;
        float uvHeight = viewport.rect.height / gridHeight; // 높이도 계산

        // 3. RawImage의 UV 사각형 갱신 (배경 이동)
        gridBackground.uvRect = new Rect(uvX, 0f, uvWidth, uvHeight);
    }
    protected override string FormatTooltipPrice(double priceKrw) {
        // 1. 캔들에 저장된 원화(KRW) 가격을 달러(USD)로 변환
        double priceUsd = priceKrw / GlobalEconomyManager.UsdToKrw;

        // 2. 형님이 이미 잘 만들어두신 달러 포맷 함수 재활용!
        return FormatPriceUSD(priceUsd);
    }
}