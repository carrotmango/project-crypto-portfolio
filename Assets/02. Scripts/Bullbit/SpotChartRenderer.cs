using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// 현물 거래소용 차트 렌더러 (레버리지/청산 제외, 코인 목록 및 헤더 UI 관리)
/// </summary>
public class SpotChartRenderer : LiveChartRenderer {
    [Header("Spot Selector UI (코인 목록 드롭다운)")]
    public GameObject coinListPanel;
    public Transform listParent;
    public GameObject coinRowPrefab;

    [Header("Selected Coin Display (상단 헤더)")]
    public TextMeshProUGUI selectedCoinNameText;
    public Image selectedCoinIcon;
    public TextMeshProUGUI headerCurrentPriceText;

    public TradePanelController tradePanelController;

    private List<CoinData> coinList;
    private Dictionary<GameObject, CoinData> rowDataMap = new();
    private List<GameObject> rowPool = new List<GameObject>();
    private bool isRefreshing = false;
    private double lastHeaderPrice = 0;

    // [버그 픽스] 외부 클릭 무시용 플래그
    private bool skipNextClickCheck = false;

    [Header("Spot Position List UI")]
    public Transform positionListParent; // ScrollView의 Content
    public GameObject positionPrefab;    // 아까 만든 SpotPositionUI가 붙은 프리팹

    private Dictionary<string, SpotPositionUI> activeSpotUIs = new Dictionary<string, SpotPositionUI>();

    public Button sellAllButton;

    protected override TradeType AllowedMarkerTypes => TradeType.SpotBuy | TradeType.SpotSell;

    private class SpotRowRef {
        public TextMeshProUGUI priceText;
        public TextMeshProUGUI changeText;
    }
    private Dictionary<GameObject, SpotRowRef> rowRefMap = new();

    private void Awake() {
        if (btn4H != null) {
            btn4H.onClick.RemoveAllListeners();
            btn4H.onClick.AddListener(() => SwitchInterval(ChartInterval._4H));
        }
        if (btn1D != null) {
            btn1D.onClick.RemoveAllListeners();
            btn1D.onClick.AddListener(() => SwitchInterval(ChartInterval._1D));
        }
        if (btnZoomIn != null) btnZoomIn.onClick.AddListener(OnZoomInBtn);
        if (btnZoomOut != null) btnZoomOut.onClick.AddListener(OnZoomOutBtn);
        if (btnHLine != null) btnHLine.onClick.AddListener(ToggleHLineMode);
        if (btnMeasure != null) btnMeasure.onClick.AddListener(ToggleMeasurementMode);

        if (coinListPanel != null) coinListPanel.SetActive(false);

        if (sellAllButton != null) {
            sellAllButton.onClick.AddListener(OnSellAllButtonClicked);
        }
    }

    public void OnSellAllButtonClicked() {
        if (PlayerManager.Instance != null) {
            // 1. 보유 중인 '상장된' 코인 전액 매도 (포트폴리오랑 완전 동일)
            PlayerManager.Instance.SellAllListedCoins();

            // 2. UI 즉시 갱신 (다 팔렸으니 하단 프리팹들이 알아서 싹 지워짐)
            UpdatePositionsUI();

            Debug.Log("[SpotChartRenderer] 상장 코인 일괄 매도가 완료되었습니다.");
        }
    }

    public void SelectCoin(CoinData coin) {
        if (coin == null) return;

        if (selectedCoinNameText != null) selectedCoinNameText.text = $"{coin.Symbol} / KRW";
        if (selectedCoinIcon != null) {
            Sprite s = Resources.Load<Sprite>($"Coins/{coin.Symbol}");
            if (s != null) selectedCoinIcon.sprite = s;
        }

        lastHeaderPrice = 0;
        if (headerCurrentPriceText != null) headerCurrentPriceText.color = Color.white;

        base.Initialize(coin);
        UpdateHeaderPriceText();

        if (coinListPanel != null) coinListPanel.SetActive(false);

        if (tradePanelController != null) {
            tradePanelController.OpenPanel(coin);
        }
    }

    public void ToggleCoinList() {
        Debug.Log("=====================================");
        Debug.Log("[디버그 1] ToggleCoinList() 함수 호출됨! (버튼 눌림 인식 정상)");

        if (coinListPanel == null) {
            Debug.LogError("[디버그 에러] coinListPanel이 인스펙터에 안 들어가 있습니다!");
            return;
        }

        bool isActive = !coinListPanel.activeSelf;
        Debug.Log($"[디버그 2] 패널 켜기 상태 변경: {isActive} (현재 상태: {coinListPanel.activeSelf})");

        coinListPanel.SetActive(isActive);

        if (isActive) {
            skipNextClickCheck = true;
            Debug.Log("[디버그 3] 코인 리스트 생성 코루틴(SafeRefreshList) 시작 지시!");
            StartCoroutine(SafeRefreshList());
        }
    }

    protected override void Update() {
        if (coinListPanel != null && coinListPanel.activeSelf) {
            HandleExternalClick();
            if (EventSystem.current.IsPointerOverGameObject()) UpdateListOnly();
        }

        base.Update();
        UpdateHeaderPriceText();

        UpdatePositionsUI();
    }

    private void UpdatePositionsUI() {
        if (PlayerManager.Instance == null || positionListParent == null || positionPrefab == null) return;

        List<string> symbolsToRemove = new List<string>();

        // 1. 내 지갑(holdings)을 싹 훑으면서 수량 체크
        foreach (var kvp in PlayerManager.Instance.holdings) {
            string sym = kvp.Key;
            double amount = kvp.Value;
            CoinData coin = CoinManager.Instance.coins.Find(c => c.Symbol == sym);

            bool isListedAndValid = (amount > 0 && coin != null && coin.IsListed && !coin.IsDelisted);

            if (isListedAndValid) {
                if (!activeSpotUIs.ContainsKey(sym)) {
                    // [신규 진입] 아직 UI가 없으면 프리팹 복사해서 띄움
                    GameObject go = Instantiate(positionPrefab, positionListParent);
                    SpotPositionUI ui = go.GetComponent<SpotPositionUI>();

                    // [수정] this (SpotChartRenderer)를 넘겨주어 클릭 이벤트를 처리할 수 있게 함
                    ui.Setup(sym, this);

                    activeSpotUIs[sym] = ui;
                } else {
                    // [기존 보유] 이미 UI가 있으면 가격/수익률 실시간 갱신
                    activeSpotUIs[sym].UpdateRealtime();
                }
            } else {
                // [전액 매도] 수량이 0이 되었거나 상폐되었는데 UI가 남아있다면 삭제 리스트에 추가
                if (activeSpotUIs.ContainsKey(sym)) {
                    symbolsToRemove.Add(sym);
                }
            }
        }

        // 2. 다 팔아서 0개가 된 코인들의 UI를 화면에서 깔끔하게 날려버림
        foreach (string sym in symbolsToRemove) {
            if (activeSpotUIs.TryGetValue(sym, out var ui)) {
                Destroy(ui.gameObject);
            }
            activeSpotUIs.Remove(sym);
        }
    }

    private void UpdateHeaderPriceText() {
        if (headerCurrentPriceText != null && targetCoin != null) {
            double currentPrice = targetCoin.CurrentPrice;

            if (System.Math.Abs(currentPrice - lastHeaderPrice) < 0.0000001) return;

            headerCurrentPriceText.text = targetCoin.GetFormattedPriceKRW();

            if (lastHeaderPrice > 0) {
                if (currentPrice > lastHeaderPrice) {
                    headerCurrentPriceText.color = new Color32(50, 214, 149, 255);
                } else if (currentPrice < lastHeaderPrice) {
                    headerCurrentPriceText.color = new Color32(230, 60, 60, 255);
                }
            } else {
                headerCurrentPriceText.color = Color.white;
            }

            lastHeaderPrice = currentPrice;
        }
    }

    private IEnumerator SafeRefreshList() {
        Debug.Log("[디버그 4] SafeRefreshList() 코루틴 진입 성공!");
        isRefreshing = true;
        rowRefMap.Clear();
        rowDataMap.Clear();

        if (CoinManager.Instance == null || CoinManager.Instance.coins == null) {
            Debug.LogError("[디버그 에러] CoinManager.Instance 또는 coins 리스트가 Null입니다! (매니저 로드 안 됨)");
            yield break;
        }

        coinList = CoinManager.Instance.coins.Where(c => !c.IsDelisted).ToList();
        Debug.Log($"[디버그 5] 현물 코인 데이터 {coinList.Count}개 가져옴!");

        if (coinList.Count == 0) {
            Debug.LogWarning("[디버그 경고] 불러올 코인이 0개입니다!");
            yield break;
        }

        if (listParent == null) {
            Debug.LogError("[디버그 에러] listParent (Content) 가 인스펙터에 안 들어있습니다!");
            yield break;
        }

        if (coinRowPrefab == null) {
            Debug.LogError("[디버그 에러] coinRowPrefab 이 인스펙터에 안 들어있습니다!");
            yield break;
        }

        int coinCount = coinList.Count;
        int poolCount = rowPool.Count;
        int maxCount = Mathf.Max(coinCount, poolCount);

        Debug.Log("[디버그 6] 코인 UI 생성 시작...");
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
        Debug.Log("[디버그 7] 코인 UI 생성 완벽하게 끝!");
        yield return null;
    }

    private void SetupRow(GameObject row, CoinData coin) {
        Transform iconTr = row.transform.Find("Image");
        Transform nameTr = row.transform.Find("Name");
        Transform priceTr = row.transform.Find("Price");
        Transform changeTr = row.transform.Find("Change");

        if (nameTr == null || priceTr == null || changeTr == null) {
            Debug.LogError($"[디버그 에러] 프리팹 자식을 못 찾았습니다! (Name:{nameTr != null}, Price:{priceTr != null}, Change:{changeTr != null})");
            return;
        }

        rowDataMap[row] = coin;
        SpotRowRef refs = new SpotRowRef {
            priceText = priceTr.GetComponent<TextMeshProUGUI>(),
            changeText = changeTr.GetComponent<TextMeshProUGUI>()
        };
        rowRefMap[row] = refs;

        nameTr.GetComponent<TextMeshProUGUI>().text = $"{coin.Symbol} / KRW";

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

    private void UpdateListOnly() {
        if (!isRefreshing && coinListPanel != null && coinListPanel.activeSelf && rowRefMap.Count > 0) {
            foreach (var row in rowDataMap.Keys.ToList()) {
                if (row == null || !row.activeInHierarchy) continue;
                if (rowDataMap.TryGetValue(row, out CoinData coin) && rowRefMap.TryGetValue(row, out var refs)) {

                    // [요구사항 반영] 원화(KRW) 포맷으로 가격 갱신 (예: 53,000,000)
                    if (refs.priceText != null) refs.priceText.text = coin.GetFormattedPriceKRW();

                    if (refs.changeText != null) {
                        double change = coin.InitialPrice > 0 ? ((coin.CurrentPrice - coin.InitialPrice) / coin.InitialPrice) * 100.0 : 0;
                        refs.changeText.text = $"{change:+0.##;-0.##}%";
                        refs.changeText.color = change > 0 ? new Color32(50, 214, 149, 255) : (change < 0 ? new Color32(230, 60, 60, 255) : Color.white);
                    }
                }
            }
        }
    }

    private void HandleExternalClick() {
        if (Input.GetMouseButtonDown(0)) {
            if (skipNextClickCheck) {
                Debug.Log("[디버그 7] 방금 막 버튼으로 켰으므로 닫기 방지! (skipNextClickCheck 해제)");
                skipNextClickCheck = false;
                return;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool clickedInside = false;
            foreach (var result in results) {
                if (result.gameObject.transform.IsChildOf(coinListPanel.transform)) {
                    clickedInside = true; break;
                }
            }
            if (!clickedInside) {
                Debug.Log("[디버그 8] 패널 바깥을 클릭해서 창을 닫습니다!");
                coinListPanel.SetActive(false);
            }
        }
    }
}