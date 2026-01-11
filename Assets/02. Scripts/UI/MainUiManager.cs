using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public enum SortType { None, Symbol, Name, Price, Change }
public enum SortOrder { Normal, Ascending, Descending }

public class MainUIManager : MonoBehaviour {
    [Header("UI References")]
    public GameObject coinRowPrefab;
    public Transform coinListParent;
    public BuyPanelController buyPanelController;
    public SellPanelController sellPanelController;
    public ChartPanelController chartPanelController;

    [Header("Sort Buttons")]
    public Button symbolButton;
    public Button nameButton;
    public Button priceButton;
    public Button changeButton;

    public TextMeshProUGUI symbolLabel;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI priceLabel;
    public TextMeshProUGUI changeLabel;

    private List<GameObject> coinRows = new();
    private Dictionary<GameObject, CoinData> rowDataMap = new();

    private SortType currentSortType = SortType.None;
    private SortOrder currentSortOrder = SortOrder.Normal;

    private bool pendingRefresh = false;
    private bool marketInitialized = false;

    // 🔴 핵심 플래그
    private bool suppressRowUpdateThisFrame = false;

    void Start() {
        AddButtonListener(symbolButton, SortType.Symbol);
        AddButtonListener(nameButton, SortType.Name);
        AddButtonListener(priceButton, SortType.Price);
        AddButtonListener(changeButton, SortType.Change);

        RefreshCoinRows();
    }

    void OnEnable() {
        if (CoinManager.Instance != null) {
            CoinManager.Instance.OnMarketUpdated += HandleMarketUpdated;
        }
    }

    void OnDisable() {
        if (CoinManager.Instance != null) {
            CoinManager.Instance.OnMarketUpdated -= HandleMarketUpdated;
        }
    }

    private void HandleMarketUpdated() {
        marketInitialized = true;

        if (currentSortOrder == SortOrder.Normal)
            return;

        pendingRefresh = true;
    }

    private void AddButtonListener(Button btn, SortType type) {
        if (btn != null) {
            btn.onClick.AddListener(() => OnSortClick(type));
        }
    }

    void Update() {

        // 🔴 Row 재생성 프레임에서는 UI 갱신 스킵
        if (suppressRowUpdateThisFrame) {
            suppressRowUpdateThisFrame = false;
            return;
        }

        foreach (var kvp in rowDataMap) {
            GameObject row = kvp.Key;
            CoinData coin = kvp.Value;

            if (coin == null || coin.IsDelisted) continue;

            row.transform.Find("PriceText")
                .GetComponent<TextMeshProUGUI>()
                .text = coin.GetFormattedPriceKRW();

            var changeText = row.transform.Find("ChangeText")
                .GetComponent<TextMeshProUGUI>();

            if (!marketInitialized || coin.InitialPrice <= 0) {
                changeText.text = "-";
                changeText.color = Color.white;
            } else {
                double change =
                    ((coin.CurrentPrice - coin.InitialPrice) / coin.InitialPrice) * 100.0;

                changeText.text = $"{change:+0.##;-0.##}%";

                if (change > 0)
                    changeText.color = new Color32(37, 167, 80, 255);
                else if (change < 0)
                    changeText.color = new Color32(255, 77, 77, 255);
                else
                    changeText.color = Color.white;
            }
        }

        if (pendingRefresh) {
            pendingRefresh = false;
            RefreshCoinRows();
        }
    }

    public void OnSortClick(SortType type) {
        if (currentSortType == type) {
            currentSortOrder = (SortOrder)(((int)currentSortOrder + 1) % 3);
        } else {
            currentSortType = type;
            currentSortOrder = SortOrder.Ascending; // 첫 클릭 = 높은 순
        }

        UpdateLabelColors();
        RefreshCoinRows();
    }

    private void UpdateLabelColors() {
        Color32 normal = Color.white;
        Color32 asc = new Color32(37, 167, 80, 255);
        Color32 desc = new Color32(255, 77, 77, 255);

        symbolLabel.color = nameLabel.color =
            priceLabel.color = changeLabel.color = normal;

        if (currentSortOrder == SortOrder.Normal) return;

        Color target = currentSortOrder == SortOrder.Ascending ? asc : desc;

        switch (currentSortType) {
            case SortType.Symbol: symbolLabel.color = target; break;
            case SortType.Name: nameLabel.color = target; break;
            case SortType.Price: priceLabel.color = target; break;
            case SortType.Change: changeLabel.color = target; break;
        }
    }

    public void RefreshCoinRows() {
        // 1. 데이터 리스트 가져오기 (필터링 및 정렬)
        List<CoinData> list = CoinManager.Instance.coins
            .Where(c => !c.IsDelisted)
            .ToList();

        if (currentSortOrder != SortOrder.Normal) {
            bool highFirst = (currentSortOrder == SortOrder.Ascending);
            switch (currentSortType) {
                case SortType.Symbol:
                    list = highFirst ? list.OrderBy(c => c.Symbol).ToList() : list.OrderByDescending(c => c.Symbol).ToList();
                    break;
                case SortType.Name:
                    list = highFirst ? list.OrderBy(c => c.Name).ToList() : list.OrderByDescending(c => c.Name).ToList();
                    break;
                case SortType.Price:
                    list = highFirst ? list.OrderByDescending(c => c.CurrentPrice).ToList() : list.OrderBy(c => c.CurrentPrice).ToList();
                    break;
                case SortType.Change:
                    list = highFirst ? list.OrderByDescending(c => c.InitialPrice > 0 ? (c.CurrentPrice - c.InitialPrice) / c.InitialPrice : 0).ToList() :
                                       list.OrderBy(c => c.InitialPrice > 0 ? (c.CurrentPrice - c.InitialPrice) / c.InitialPrice : 0).ToList();
                    break;
            }
        }

        // 2. 핵심: 파괴하지 않고 순서만 변경 (Object Pooling 개념)
        for (int i = 0; i < list.Count; i++) {
            var coin = list[i];

            // 해당 코인 데이터를 가진 GameObject 찾기
            GameObject row = rowDataMap.FirstOrDefault(x => x.Value == coin).Key;

            if (row != null) {
                // 이미 존재한다면 순서만 맨 아래로 보냄 (결과적으로 리스트 순서대로 정렬됨)
                row.transform.SetAsLastSibling();
            } else {
                // 새로 상장된 코인이라면 이때만 생성
                AddCoinRow(coin);
            }
        }

        // 3. 상폐된 코인 등이 map에 남아있다면 제거
        var rowsToRemove = rowDataMap.Where(kvp => kvp.Value.IsDelisted).ToList();
        foreach (var kvp in rowsToRemove) {
            Destroy(kvp.Key);
            rowDataMap.Remove(kvp.Key);
            coinRows.Remove(kvp.Key);
        }
    }

    public void AddCoinRow(CoinData coin) {
        GameObject row = Instantiate(coinRowPrefab, coinListParent);
        coinRows.Add(row);
        rowDataMap.Add(row, coin);

        row.transform.Find("SymbolText")
            .GetComponent<TextMeshProUGUI>().text = coin.Symbol;

        row.transform.Find("NameText")
            .GetComponent<TextMeshProUGUI>().text = coin.Name;

        Image icon = row.transform.Find("IconImage")?.GetComponent<Image>();
        if (icon != null) {
            Sprite sprite = Resources.Load<Sprite>($"Coins/{coin.Symbol}");
            if (sprite != null) icon.sprite = sprite;
        }

        row.transform.Find("ChartBtn")
            .GetComponent<Button>()
            .onClick.AddListener(() => chartPanelController?.ShowChartPanel(coin));

        var buyBtn = row.transform.Find("BuyBtn").GetComponent<Button>();
        buyBtn.onClick.AddListener(() => buyPanelController?.OpenPanel(coin));

        if (TutorialManager.Instance != null) {
            if (coin.Symbol == "BTC") TutorialManager.Instance.btcBuyButton = buyBtn;
            else if (coin.Symbol == "ETH") TutorialManager.Instance.ethBuyButton = buyBtn;
        }

        row.transform.Find("SellBtn")
            .GetComponent<Button>()
            .onClick.AddListener(() => sellPanelController?.OpenPanel(coin));
    }
}
