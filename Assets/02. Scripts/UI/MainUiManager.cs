using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System;

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

    // 핵심 플래그
    private bool suppressRowUpdateThisFrame = false;
    private bool filterOwnedOnly = false;
    private bool filterWishlistOnly = false; 

    [Header("Filter UI")]
    public Toggle ownedOnlyToggle;
    public Toggle wishlistToggle;
    public TMP_Dropdown themeDropdown;

    private HashSet<string> wishlistedSymbols = new HashSet<string>();
    private Color32 starActiveColor = new Color32(255, 200, 0, 255); // 노란색
    private Color32 starInactiveColor = new Color32(200, 200, 200, 255); // 회색/흰색
    private List<CoinTheme> sortedThemeMap = new List<CoinTheme>();

    void Start() {
        LoadWishlist();
        SetupThemeDropdown();

        AddButtonListener(symbolButton, SortType.Symbol);
        AddButtonListener(nameButton, SortType.Name);
        AddButtonListener(priceButton, SortType.Price);
        AddButtonListener(changeButton, SortType.Change);

        if (ownedOnlyToggle != null) {
            ownedOnlyToggle.onValueChanged.AddListener(ToggleOwnedFilter);
        }

        if (wishlistToggle != null) {
            wishlistToggle.onValueChanged.AddListener(ToggleWishlistFilter);
        }

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

    private void SetupThemeDropdown() {
        if (themeDropdown == null) return;

        themeDropdown.ClearOptions();
        sortedThemeMap.Clear(); // 맵 초기화

        // 1. 임시 리스트에 (한글이름, 테마) 쌍을 담습니다.
        List<(string Name, CoinTheme Theme)> tempThemes = new List<(string, CoinTheme)>();

        foreach (CoinTheme theme in Enum.GetValues(typeof(CoinTheme))) {
            tempThemes.Add((GetThemeNameKR(theme), theme));
        }

        // 2. [핵심] 한글 이름 기준으로 오름차순 정렬 (가나다 순)
        tempThemes.Sort((a, b) => a.Name.CompareTo(b.Name));

        // 3. 드롭다운 옵션 구성 (맨 위는 '전체보기')
        List<string> displayOptions = new List<string> { "전체보기" };

        foreach (var item in tempThemes) {
            displayOptions.Add(item.Name);
            sortedThemeMap.Add(item.Theme); // 정렬된 순서대로 맵에 저장
        }

        themeDropdown.AddOptions(displayOptions);

        // 값 변경 시 리프레시 호출 (람다 대신 명확하게 연결)
        themeDropdown.onValueChanged.RemoveAllListeners();
        themeDropdown.onValueChanged.AddListener((idx) => RefreshCoinRows());
    }

    private string GetThemeNameKR(CoinTheme theme) {
        switch (theme) {
            case CoinTheme.Layer1: return "레이어 1";
            case CoinTheme.Layer2: return "레이어 2";
            case CoinTheme.Meme: return "밈";
            case CoinTheme.AI: return "AI / 인공지능";
            case CoinTheme.RWA: return "RWA";
            case CoinTheme.ZK: return "ZK";
            case CoinTheme.DeFi: return "디파이";
            case CoinTheme.Stable: return "스테이블";
            default: return theme.ToString();
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

        //  Row 재생성 프레임에서는 UI 갱신 스킵
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
                    changeText.color = new Color32(50, 214, 149, 255);
                else if (change < 0)
                    changeText.color = new Color32(230, 60, 60, 255);
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
        Color32 asc = new Color32(50, 214, 149, 255);
        Color32 desc = new Color32(230, 60, 60, 255);

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

    public void ToggleOwnedFilter(bool enabled) {
        filterOwnedOnly = enabled;
        RefreshCoinRows();
    }

    public void ToggleWishlistFilter(bool enabled) {
        filterWishlistOnly = enabled;
        RefreshCoinRows();
    }

    public void RefreshCoinRows() {
        // 1. 기본 리스트 가져오기
        List<CoinData> list = CoinManager.Instance.coins
            .Where(c => !c.IsDelisted)
            .ToList();

        // 2. [필터] 보유 코인
        if (filterOwnedOnly) {
            list = list.Where(c => c.OwnedAmount > 0).ToList();
        }

        // 3. [필터] 위시리스트
        if (filterWishlistOnly) {
            list = list.Where(c => wishlistedSymbols.Contains(c.Symbol)).ToList();
        }

        // 4. [필터] 테마 (드롭다운) - 수정된 로직 적용
        if (themeDropdown != null && themeDropdown.value > 0) {

            int themeIndex = themeDropdown.value - 1;

            if (themeIndex >= 0 && themeIndex < sortedThemeMap.Count) {
                CoinTheme selectedTheme = sortedThemeMap[themeIndex];

                list = list.Where(c => {
                    var meta = CoinMetaDatabase.AllCoins.FirstOrDefault(m => m.Symbol == c.Symbol);
                    return meta != null && meta.Theme == selectedTheme;
                }).ToList();
            }
        }

        // 5. 정렬 로직 (기존 유지)
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

        // 6. Row 생성 및 관리
        for (int i = 0; i < list.Count; i++) {
            var coin = list[i];
            GameObject row = rowDataMap.FirstOrDefault(x => x.Value == coin).Key;

            if (row != null) {
                row.transform.SetAsLastSibling();
            } else {
                AddCoinRow(coin);
            }
        }

        // 7. 필터링된 Row 숨기기
        foreach (var kvp in rowDataMap) {
            bool shouldBeVisible = list.Contains(kvp.Value);
            kvp.Key.SetActive(shouldBeVisible);
        }

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

        Transform starTr = row.transform.Find("WishlistBtn");
        if (starTr != null) {
            Button starBtn = starTr.GetComponent<Button>();
            Image starImg = starTr.GetComponent<Image>();

            // 초기 색상 설정
            bool isWish = wishlistedSymbols.Contains(coin.Symbol);
            starImg.color = isWish ? starActiveColor : starInactiveColor;

            // 클릭 이벤트
            starBtn.onClick.RemoveAllListeners();
            starBtn.onClick.AddListener(() => OnStarClicked(coin, starImg));
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

    private void OnStarClicked(CoinData coin, Image starImg) {
        if (wishlistedSymbols.Contains(coin.Symbol)) {
            // 이미 있으면 제거
            wishlistedSymbols.Remove(coin.Symbol);
            starImg.color = starInactiveColor;
        } else {
            // 없으면 추가
            wishlistedSymbols.Add(coin.Symbol);
            starImg.color = starActiveColor;
        }

        // 저장
        SaveWishlist();

        // 만약 '관심 종목만 보기' 필터가 켜져 있는 상태라면, 
        // 별을 끄는 순간 목록에서 사라지게 갱신해야 자연스러움
        if (filterWishlistOnly) {
            RefreshCoinRows();
        }
    }

    private void SaveWishlist() {
        string data = string.Join(",", wishlistedSymbols);
        PlayerPrefs.SetString("UserWishlist", data);
        PlayerPrefs.Save();
    }

    // [추가] 불러오기 기능
    private void LoadWishlist() {
        string data = PlayerPrefs.GetString("UserWishlist", "");
        if (!string.IsNullOrEmpty(data)) {
            string[] symbols = data.Split(',');
            foreach (var s in symbols) {
                if (!string.IsNullOrEmpty(s)) wishlistedSymbols.Add(s);
            }
        }
    }
}
