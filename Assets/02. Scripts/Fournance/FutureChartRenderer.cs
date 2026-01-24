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
        base.Initialize(coin);
        UpdateHeaderPriceText();
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

    public double GetCrossMarginEquity() {
        // 1. 남은 현금 (Free Cash)
        double equity = PlayerManager.Instance.fournanceCash;

        foreach (var pos in activePositions) {
            if (pos.Mode == MarginMode.Cross) {
                // [핵심 수정] 증거금도 내 돈이다! (이거 안 더하면 풀배팅 시 0원으로 인식됨)
                equity += pos.MarginUSD;

                // PnL 더하기
                double currentPrice = GetCurrentTargetPriceUSD(pos.Symbol);
                if (currentPrice > 0) {
                    double priceDiff = pos.IsLong ? (currentPrice - pos.EntryPriceUSD) : (pos.EntryPriceUSD - currentPrice);
                    double pnl = priceDiff * pos.Quantity;
                    equity += pnl;
                }
            }
        }

        return equity > 0 ? equity : 0;
    }

    public void OnSliderValueChanged(float value) {
        if (targetCoin == null || PlayerManager.Instance == null) return;
        if (orderAmountInput != null) orderAmountInput.text = value.ToString("F0");

        // [수정] Buying Power(구매력) 기준으로 계산
        double buyingPower = GetBuyingPower();

        // 주문하려는 증거금
        double inputMarginUsd = buyingPower * (value / 100.0);

        // 안전장치 if문 제거! (이제 수익금으로도 주문 가능)
        /* if (inputMarginUsd > PlayerManager.Instance.fournanceCash) {
            inputMarginUsd = PlayerManager.Instance.fournanceCash;
        }
        */

        double totalOrderValueUsd = inputMarginUsd * currentLeverage;
        double currentPriceUsd = targetCoin.CurrentPrice / GlobalEconomyManager.UsdToKrw;
        double orderQty = (currentPriceUsd > 0) ? (totalOrderValueUsd / currentPriceUsd) : 0;

        if (costText != null) costText.text = $"COST: ${inputMarginUsd:N2} / ${totalOrderValueUsd:N2}";
        if (maxQtyText != null) maxQtyText.text = $"Size: {orderQty:F4} {targetCoin.Symbol}";
    }

public void OpenPosition(bool isLong) {
        if (targetCoin == null || orderPercentageSlider.value <= 0) return;

        // [수정] Buying Power 기준
        double buyingPower = GetBuyingPower();
        double marginUsd = buyingPower * (orderPercentageSlider.value / 100.0);

        // [삭제됨] 안전장치 if문 제거! 
        // 이제 지갑에 현금이 없어도(심지어 마이너스여도) PnL이 빵빵하면 주문 들어감
        /*
        if (marginUsd > PlayerManager.Instance.fournanceCash) {
            marginUsd = PlayerManager.Instance.fournanceCash;
        }
        */

        if (marginUsd <= 0.0001) {
            Debug.LogWarning("주문 가능 잔액(Buying Power)이 부족합니다.");
            return;
        }

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
            existingPos.MarginUSD += marginUsd;

            // 청산가 로직 (기존 유지)
            double remainingBalance = PlayerManager.Instance.fournanceCash;
            existingPos.LiquidationPriceUSD = CalculateLiquidationPrice(
                newAvgEntry, existingPos.Quantity, remainingBalance, existingPos.MarginUSD, isLong, currentMarginMode
            );

            if (uiMap.TryGetValue(existingPos, out var ui)) ui.UpdateRealtime();
        } else {
            // [신규 진입]
            PlayerManager.Instance.fournanceCash -= marginUsd;
            double remainingBalance = PlayerManager.Instance.fournanceCash;

            // 청산가 로직 (기존 유지)
            double liqPriceUsd = CalculateLiquidationPrice(
                currentEntryPrice, quantity, remainingBalance, marginUsd, isLong, currentMarginMode
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
            GameObject go = Instantiate(positionPrefab, positionListParent);
            FuturePositionUI ui = go.GetComponent<FuturePositionUI>();
            ui.Setup(newPos, this);
            uiMap[newPos] = ui;
        }

        RefreshPositionLines();
        UpdateModeUI();
    }

    public void ClosePositionMarket(FuturePosition pos, bool isLiquidated = false) {
        if (!activePositions.Contains(pos)) return;

        double currentPriceUsd = GetCurrentTargetPriceUSD(pos.Symbol);
        double priceDiff = pos.IsLong ? (currentPriceUsd - pos.EntryPriceUSD) : (pos.EntryPriceUSD - currentPriceUsd);
        double pnlUsd = priceDiff * pos.Quantity;

        // [수정 1] 청산 전, 현재 지갑 잔고 백업 (Cross 계산용)
        double currentCash = PlayerManager.Instance.fournanceCash;
        double actualLoss = 0; // 실제 확정 손실액

        if (isLiquidated) {
            // [강제 청산]
            if (pos.Mode == MarginMode.Cross) {
                // Cross: 진입한 증거금 + 남은 지갑 잔고 전부 몰수
                actualLoss = pos.MarginUSD + currentCash;

                // 잔고 0원 파산 처리
                PlayerManager.Instance.fournanceCash = 0;
                Debug.LogError($"[Cross 청산] {pos.Symbol} 파산! 총 손실: -${actualLoss:N2}");
            } else {
                // Isolated: 진입한 증거금만 몰수 (지갑 잔고는 안전)
                actualLoss = pos.MarginUSD;

                Debug.LogError($"[Isolated 청산] {pos.Symbol} 증거금 -${actualLoss:N2} 전액 소멸.");
            }

            if (GlobalNotificationManager.Instance != null) {
                string sender = "Fournance Risk Team";
                string shortMsg = $"[알림] {pos.Symbol}USD 포지션이 강제 청산되었습니다.";

                // [수정] 3줄 요약 버전
                // 1줄: 종목 + 포지션 (L/S)
                string fullBody = $"[청산] {pos.Symbol}USD ({(pos.IsLong ? "Long" : "Short")})\n";

                // 2줄: 진입가 / 청산가 (가로로 배치)
                fullBody += $"진입 ${pos.EntryPriceUSD:N4} / 청산 ${pos.LiquidationPriceUSD:N4}\n";

                // [수정 2] 3줄: 실제 손실액(actualLoss) 표기 + (0원이 된) 잔고 표기
                fullBody += $"손실 -${actualLoss:N2} (잔고 ${PlayerManager.Instance.fournanceCash:N2})";

                // "Bullbit" 타입 (아이콘) 사용
                GlobalNotificationManager.Instance.ShowNotification(
                    "Bullbit",
                    sender,
                    shortMsg,
                    () => {
                        if (UIManager.Instance != null) {
                            UIManager.Instance.ShowSMSResult(
                                $"발신인: {sender}\n\n{fullBody}"
                            );
                        }
                    }
                );
            }

        } else {
            // [정상 종료] (익절/손절)
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
            headerCurrentPriceText.text = FormatPriceUSD(currentPriceUsd);
        }
    }

    public double GetBuyingPower() {
        double equity = GetCrossMarginEquity(); // (현금 + PnL)
        double usedMargin = CalculateTotalUsedMargin(); // (이미 포지션 잡느라 쓴 돈)

        double buyingPower = equity - usedMargin;
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
                "모든 포지션을 시장가로 종료했습니다.",
                null
            );
        }
    }
}