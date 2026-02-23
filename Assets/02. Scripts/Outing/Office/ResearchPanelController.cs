using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System;

// 정렬 옵션에 'MarketCap' 추가
public enum ResearchSortType { None, Name, Price, Change, MarketCap }
// 2. 사이클을 위한 Order 확장
public enum ResearchSortOrder { Normal, NameAsc, NameDesc, SymbolAsc, SymbolDesc }

public class ResearchPanelController : MonoBehaviour {
    [Header("UI References")]
    public GameObject coinRowPrefab;
    public Transform coinListParent;

    public ResearchDetailController detailController;

    // [NEW] 상세 패널 컨트롤러 연결용 (나중에 구현)
    // public ResearchDetailController detailController; 

    [Header("Sort Buttons")]
    public Button nameButton;
    public Button priceButton;
    public Button changeButton;
    public Button marketCapButton; // [NEW] 시총 정렬 버튼
    public Button resetButton;

    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI priceLabel;
    public TextMeshProUGUI changeLabel;
    public TextMeshProUGUI marketCapLabel; // [NEW] 시총 라벨

    private List<GameObject> coinRows = new();
    private Dictionary<GameObject, CoinData> rowDataMap = new();

    private ResearchSortType currentSortType = ResearchSortType.None;
    private ResearchSortOrder currentSortOrder = ResearchSortOrder.Normal;

    private bool pendingRefresh = false;
    private bool marketInitialized = false;
    private bool suppressRowUpdateThisFrame = false;

    [Header("Filter UI")]
    public Toggle wishlistToggle; // 관심종목 필터
    public TMP_Dropdown themeDropdown; // 테마 필터

    [Header("Currency Settings")]
    public Toggle usdToggle; // 인스펙터에서 달러 표시 체크박스 연결
    private bool showInUsd = false; // 현재 달러 표시 모드인지 여부

    [Header("Main/Analysis Tabs")]
    public GameObject mainPanel;      // Total News And Exchange 오브젝트
    public GameObject analysisPanel;  // CoinSelect 오브젝트
    public Button mainTabButton;      // '메인' 버튼
    public Button analysisTabButton;  // '분석' 버튼

    [Header("Tab Button Labels")]
    public TextMeshProUGUI mainTabText;     // '메인' 버튼의 글자
    public TextMeshProUGUI analysisTabText; // '분석' 버튼의 글자

    private bool filterWishlistOnly = false;
    private HashSet<string> wishlistedSymbols = new HashSet<string>();
    private Color32 starActiveColor = new Color32(255, 200, 0, 255);
    private Color32 starInactiveColor = new Color32(200, 200, 200, 255);
    private List<CoinTheme> sortedThemeMap = new List<CoinTheme>();

    private Color32 tabActiveColor = new Color32(20, 255, 8, 255); // #14FF08
    private Color32 tabNormalColor = Color.white;                  // 비활성 시 흰색

    void Start() {
        LoadWishlist();
        SetupThemeDropdown();

        // 정렬 버튼 이벤트 연결
        AddButtonListener(nameButton, ResearchSortType.Name);
        AddButtonListener(priceButton, ResearchSortType.Price);
        AddButtonListener(changeButton, ResearchSortType.Change);
        AddButtonListener(marketCapButton, ResearchSortType.MarketCap); // [NEW] 시총 정렬
        if (resetButton != null) resetButton.onClick.AddListener(ResetFiltersAndSort);

        if (wishlistToggle != null) {
            wishlistToggle.onValueChanged.AddListener(ToggleWishlistFilter);
        }
        if (usdToggle != null) {
            usdToggle.onValueChanged.AddListener(ToggleCurrencyMode);
        }

        if (mainTabButton != null) mainTabButton.onClick.AddListener(ShowMainPanel);
        if (analysisTabButton != null) analysisTabButton.onClick.AddListener(ShowAnalysisPanel);

        // 시작 시 기본 화면 설정 (메인 패널 오픈)
        ShowMainPanel();
        RefreshCoinRows();
    }

    // 메인 패널(뉴스/환율) 보여주기
    public void ShowMainPanel() {
        // 1. 만약 상세 패널이 켜져 있다면 닫기 (뒤로가기 로직 실행)
        if (detailController != null && detailController.detailPanel.activeSelf) {
            detailController.OnClickBack();
        }

        // 2. 패널 전환
        mainPanel.SetActive(true);
        analysisPanel.SetActive(false);

        // 3. 라벨 색상 변경
        if (mainTabText != null) mainTabText.color = tabActiveColor;
        if (analysisTabText != null) analysisTabText.color = tabNormalColor;

        Debug.Log("리서치: 메인 탭 활성화 (상세패널 종료 포함)");
    }

    // 분석 패널(코인 리스트) 보여주기
    public void ShowAnalysisPanel() {
        // 1. 만약 상세 패널이 켜져 있다면 닫기
        if (detailController != null && detailController.detailPanel.activeSelf) {
            detailController.OnClickBack();
        }

        // 2. 패널 전환
        mainPanel.SetActive(false);
        analysisPanel.SetActive(true);

        // 3. 라벨 색상 변경
        if (mainTabText != null) mainTabText.color = tabNormalColor;
        if (analysisTabText != null) analysisTabText.color = tabActiveColor;

        RefreshCoinRows();
        Debug.Log("리서치: 분석 탭 활성화 (상세패널 종료 포함)");
    }
    public bool ShowInUsd => showInUsd; // 자식들이 읽어갈 수 있도록 프로퍼티 노출

    public void ToggleCurrencyMode(bool isUsd) {
        showInUsd = isUsd;
        RefreshCoinRows(); // 리스트 갱신

        // 만약 상세 패널이 열려있다면 상세 패널도 즉시 갱신
        if (detailController != null && detailController.detailPanel.activeSelf) {
            detailController.RefreshTextUI();
        }
    }

    public void ResetFiltersAndSort() {
        // A. 정렬 초기화
        currentSortType = ResearchSortType.None;
        currentSortOrder = ResearchSortOrder.Normal;

        // B. 필터 초기화
        if (wishlistToggle != null) wishlistToggle.isOn = false;
        if (usdToggle != null) usdToggle.isOn = false;
        if (themeDropdown != null) themeDropdown.value = 0; // "전체보기"로 변경
        filterWishlistOnly = false;

        // C. 라벨 텍스트 및 컬러 원복
        nameLabel.text = "이름";
        UpdateLabelColors(); // 모든 컬러를 Normal로 되돌림

        // D. 리스트 갱신
        RefreshCoinRows();

        Debug.Log("모든 필터와 정렬이 초기화되었습니다.");
    }

    void OnEnable() {
        if (CoinManager.Instance != null) {
            CoinManager.Instance.OnMarketUpdated += HandleMarketUpdated;
        }
        RefreshCoinRows(); // 패널 열릴 때마다 갱신
    }

    void OnDisable() {
        if (CoinManager.Instance != null) {
            CoinManager.Instance.OnMarketUpdated -= HandleMarketUpdated;
        }
    }

    // --- 테마 드롭다운 및 위시리스트 로직은 기존 MainUIManager와 100% 동일하므로 생략 없이 넣었습니다 ---
    private void SetupThemeDropdown() {
        if (themeDropdown == null) return;
        themeDropdown.ClearOptions();
        sortedThemeMap.Clear();

        List<(string Name, CoinTheme Theme)> tempThemes = new List<(string, CoinTheme)>();
        foreach (CoinTheme theme in Enum.GetValues(typeof(CoinTheme))) {
            tempThemes.Add((GetThemeNameKR(theme), theme));
        }

        tempThemes.Sort((a, b) => a.Name.CompareTo(b.Name));
        List<string> displayOptions = new List<string> { "전체보기" };

        foreach (var item in tempThemes) {
            displayOptions.Add(item.Name);
            sortedThemeMap.Add(item.Theme);
        }

        themeDropdown.AddOptions(displayOptions);
        themeDropdown.onValueChanged.RemoveAllListeners();
        themeDropdown.onValueChanged.AddListener((idx) => RefreshCoinRows());
    }

    private string GetThemeNameKR(CoinTheme theme) {
        return theme switch {
            CoinTheme.Layer1 => "레이어 1",
            CoinTheme.Layer2 => "레이어 2",
            CoinTheme.Meme => "밈",
            CoinTheme.AI => "AI / 인공지능",
            CoinTheme.RWA => "RWA",
            CoinTheme.ZK => "ZK",
            CoinTheme.DeFi => "디파이",
            CoinTheme.Stable => "스테이블",
            _ => theme.ToString()
        };
    }

    private void HandleMarketUpdated() {
        marketInitialized = true;
        if (currentSortOrder == ResearchSortOrder.Normal) return;
        pendingRefresh = true;
    }

    private void AddButtonListener(Button btn, ResearchSortType type) {
        if (btn != null) btn.onClick.AddListener(() => OnSortClick(type));
    }

    void Update() {
        if (suppressRowUpdateThisFrame) {
            suppressRowUpdateThisFrame = false;
            return;
        }

        foreach (var kvp in rowDataMap) {
            GameObject row = kvp.Key;
            CoinData coin = kvp.Value;
            if (coin == null || coin.IsDelisted) continue;

            // 1. 가격 업데이트 (소수점 정밀도 유지)
            var priceTxt = row.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
            if (showInUsd) {
                double priceInUsd = coin.CurrentPrice / GlobalEconomyManager.UsdToKrw;
                if (priceInUsd >= 1.0) priceTxt.text = $"${priceInUsd:N2}";
                else if (priceInUsd >= 0.001) priceTxt.text = $"${priceInUsd:N4}";
                else priceTxt.text = $"${priceInUsd:F6}"; // 페페 등 밈코인 대응
            } else {
                priceTxt.text = coin.GetFormattedPriceKRW();
            }

            // 2. 등락률 업데이트
            var changeText = row.transform.Find("ChangeText").GetComponent<TextMeshProUGUI>();
            if (!marketInitialized || coin.InitialPrice <= 0) {
                changeText.text = "-";
                changeText.color = Color.white;
            } else {
                double change = ((coin.CurrentPrice - coin.InitialPrice) / coin.InitialPrice) * 100.0;
                changeText.text = $"{change:+0.##;-0.##}%";
                if (change > 0) changeText.color = new Color32(50, 214, 149, 255);
                else if (change < 0) changeText.color = new Color32(230, 60, 60, 255);
                else changeText.color = Color.white;
            }

            var mcapText = row.transform.Find("MCText")?.GetComponent<TextMeshProUGUI>();
            if (mcapText != null) {
                var meta = CoinMetaDatabase.AllCoins.FirstOrDefault(m => m.Symbol == coin.Symbol);
                if (meta != null) {
                    double marketCap = coin.CurrentPrice * meta.CirculatingSupply;

                    if (showInUsd) {
                        // 달러 환산 후 영문 단위(B, M, K) 적용
                        mcapText.text = FormatMarketCapUSD(marketCap / GlobalEconomyManager.UsdToKrw);
                    } else {
                        // 기존 한국어 단위 적용
                        mcapText.text = FormatMarketCap(marketCap);
                    }
                }
            }
        }

        if (pendingRefresh) {
            pendingRefresh = false;
            RefreshCoinRows();
        }
    }

    // [NEW] 시가총액 한글 단위 파싱 (조, 억, 만)
    private string FormatMarketCap(double amount) {
        if (amount >= 1_0000_0000_0000) // 1조 이상
            return $"{(amount / 1_0000_0000_0000):F2}조";
        else if (amount >= 1_0000_0000) // 1억 이상
            return $"{(amount / 1_0000_0000):F0}억";
        else if (amount >= 1_0000) // 1만 이상
            return $"{(amount / 1_0000):F0}만";
        else
            return $"{amount:N0}";
    }

    public void OnSortClick(ResearchSortType type) {
        if (type == ResearchSortType.Name) {
            // 이름 라벨은 4단계 사이클: NameAsc -> NameDesc -> SymbolAsc -> SymbolDesc -> NameAsc...
            if (currentSortType != ResearchSortType.Name) {
                currentSortType = ResearchSortType.Name;
                currentSortOrder = ResearchSortOrder.NameAsc;
            } else {
                // 사이클 로직 (1~4번 순환)
                int nextOrder = (int)currentSortOrder + 1;
                if (nextOrder > (int)ResearchSortOrder.SymbolDesc) nextOrder = (int)ResearchSortOrder.NameAsc;
                currentSortOrder = (ResearchSortOrder)nextOrder;
            }
        } else {
            // 다른 버튼(가격, 시총 등)은 기존처럼 2단계 사이클 (내림차순 -> 오름차순)
            if (currentSortType == type) {
                currentSortOrder = (currentSortOrder == ResearchSortOrder.NameAsc) ? ResearchSortOrder.NameDesc : ResearchSortOrder.NameAsc;
            } else {
                currentSortType = type;
                currentSortOrder = ResearchSortOrder.NameAsc; // 첫 클릭 시 높은 순
            }
        }

        UpdateLabelColors();
        RefreshCoinRows();
    }

    private string FormatMarketCapUSD(double amount) {
        if (amount >= 1_000_000_000_000) // 1조 달러 이상 (Trillion)
            return $"{(amount / 1_000_000_000_000):F2}T";
        else if (amount >= 1_000_000_000) // 10억 달러 이상 (Billion)
            return $"{(amount / 1_000_000_000):F2}B";
        else if (amount >= 1_000_000) // 100만 달러 이상 (Million)
            return $"{(amount / 1_000_000):F2}M";
        else if (amount >= 1_000) // 1,000 달러 이상 (K)
            return $"{(amount / 1_000):F2}K";
        else
            return $"{amount:N2}";
    }

    private void UpdateLabelColors() {
        Color32 normal = Color.white;
        Color32 active = new Color32(20, 255, 8, 255);

        // 모든 라벨 컬러 초기화
        nameLabel.color = priceLabel.color = changeLabel.color = marketCapLabel.color = normal;

        // 만약 정렬이 없는 상태(초기화 후)라면 텍스트만 복구하고 리턴
        if (currentSortOrder == ResearchSortOrder.Normal) {
            nameLabel.text = "이름";
            return;
        }

        if (currentSortType == ResearchSortType.Name) {
            nameLabel.color = active;
            switch (currentSortOrder) {
                case ResearchSortOrder.NameAsc: nameLabel.text = "이름 ▲"; break;
                case ResearchSortOrder.NameDesc: nameLabel.text = "이름 ▼"; break;
                case ResearchSortOrder.SymbolAsc: nameLabel.text = "이름(심볼) ▲"; break;
                case ResearchSortOrder.SymbolDesc: nameLabel.text = "이름(심볼) ▼"; break;
            }
        } else {
            nameLabel.text = "이름"; // 이름 정렬이 아닐 땐 원복
            switch (currentSortType) {
                case ResearchSortType.Price: priceLabel.color = active; break;
                case ResearchSortType.Change: changeLabel.color = active; break;
                case ResearchSortType.MarketCap: marketCapLabel.color = active; break;
            }
        }
    }

    public void ToggleWishlistFilter(bool enabled) {
        filterWishlistOnly = enabled;
        RefreshCoinRows();
    }

    public void RefreshCoinRows() {
        List<CoinData> list = CoinManager.Instance.coins.Where(c => !c.IsDelisted).ToList();

        if (filterWishlistOnly) {
            list = list.Where(c => wishlistedSymbols.Contains(c.Symbol)).ToList();
        }

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

        // 정렬 로직
        // [수정] 정렬 로직
        if (currentSortOrder != ResearchSortOrder.Normal) {
            switch (currentSortType) {
                // 1. 이름/심볼 사이클 정렬 (4단계)
                case ResearchSortType.Name:
                    list = currentSortOrder switch {
                        ResearchSortOrder.NameAsc => list.OrderBy(c => c.Name).ToList(),
                        ResearchSortOrder.NameDesc => list.OrderByDescending(c => c.Name).ToList(),
                        ResearchSortOrder.SymbolAsc => list.OrderBy(c => c.Symbol).ToList(),
                        ResearchSortOrder.SymbolDesc => list.OrderByDescending(c => c.Symbol).ToList(),
                        _ => list
                    };
                    break;

                // 2. 가격 정렬 (2단계: 높은 순 / 낮은 순)
                case ResearchSortType.Price:
                    // NameAsc를 '높은 순(Desc)'으로, NameDesc를 '낮은 순(Asc)'으로 활용
                    list = (currentSortOrder == ResearchSortOrder.NameAsc)
                        ? list.OrderByDescending(c => c.CurrentPrice).ToList()
                        : list.OrderBy(c => c.CurrentPrice).ToList();
                    break;

                // 3. 시가총액 정렬 (2단계: 높은 순 / 낮은 순)
                case ResearchSortType.MarketCap:
                    list = (currentSortOrder == ResearchSortOrder.NameAsc)
                        ? list.OrderByDescending(c => {
                            var m = CoinMetaDatabase.AllCoins.FirstOrDefault(meta => meta.Symbol == c.Symbol);
                            return c.CurrentPrice * (m != null ? m.CirculatingSupply : 0);
                        }).ToList()
                        : list.OrderBy(c => {
                            var m = CoinMetaDatabase.AllCoins.FirstOrDefault(meta => meta.Symbol == c.Symbol);
                            return c.CurrentPrice * (m != null ? m.CirculatingSupply : 0);
                        }).ToList();
                    break;

                // 4. 등락률 정렬 (2단계)
                case ResearchSortType.Change:
                    list = (currentSortOrder == ResearchSortOrder.NameAsc)
                        ? list.OrderByDescending(c => c.InitialPrice > 0 ? (c.CurrentPrice - c.InitialPrice) / c.InitialPrice : 0).ToList()
                        : list.OrderBy(c => c.InitialPrice > 0 ? (c.CurrentPrice - c.InitialPrice) / c.InitialPrice : 0).ToList();
                    break;
            }
        }

        // Row 생성 및 관리
        for (int i = 0; i < list.Count; i++) {
            var coin = list[i];
            GameObject row = rowDataMap.FirstOrDefault(x => x.Value == coin).Key;

            if (row != null) row.transform.SetAsLastSibling();
            else AddCoinRow(coin);
        }

        foreach (var kvp in rowDataMap) {
            bool shouldBeVisible = list.Contains(kvp.Value);
            kvp.Key.SetActive(shouldBeVisible);
        }
    }

    public void AddCoinRow(CoinData coin) {
        GameObject row = Instantiate(coinRowPrefab, coinListParent);
        coinRows.Add(row);
        rowDataMap.Add(row, coin);

        row.transform.Find("Names/Symbol").GetComponent<TextMeshProUGUI>().text = coin.Symbol;
        row.transform.Find("Names/Name").GetComponent<TextMeshProUGUI>().text = coin.Name;

        Image icon = row.transform.Find("IconImage")?.GetComponent<Image>();
        if (icon != null) {
            Sprite sprite = Resources.Load<Sprite>($"Coins/{coin.Symbol}");
            if (sprite != null) icon.sprite = sprite;
        }

        // 관심 종목 버튼
        Transform starTr = row.transform.Find("WishlistBtn");
        if (starTr != null) {
            Button starBtn = starTr.GetComponent<Button>();
            Image starImg = starTr.GetComponent<Image>();
            bool isWish = wishlistedSymbols.Contains(coin.Symbol);
            starImg.color = isWish ? starActiveColor : starInactiveColor;
            starBtn.onClick.AddListener(() => OnStarClicked(coin, starImg));
        }

        // [NEW] 보기 버튼 (상세 패널 열기)
        var viewBtn = row.transform.Find("DetailButton")?.GetComponent<Button>();
        if (viewBtn != null) {
            viewBtn.onClick.AddListener(() => {
                Debug.Log($"[{coin.Name}] 상세 리서치 패널 열기 시도!");
                detailController.OpenPanel(coin);
            });
        }
    }

    // --- 관심종목 저장/로드 로직 동일 ---
    private void OnStarClicked(CoinData coin, Image starImg) {
        if (wishlistedSymbols.Contains(coin.Symbol)) {
            wishlistedSymbols.Remove(coin.Symbol);
            starImg.color = starInactiveColor;
        } else {
            wishlistedSymbols.Add(coin.Symbol);
            starImg.color = starActiveColor;
        }
        SaveWishlist();
        if (filterWishlistOnly) RefreshCoinRows();
    }

    private void SaveWishlist() {
        string data = string.Join(",", wishlistedSymbols);
        PlayerPrefs.SetString("UserWishlist_Research", data); // 키값 분리 추천
        PlayerPrefs.Save();
    }

    private void LoadWishlist() {
        string data = PlayerPrefs.GetString("UserWishlist_Research", ""); // 불비트랑 공유할거면 키를 맞추세요
        if (!string.IsNullOrEmpty(data)) {
            string[] symbols = data.Split(',');
            foreach (var s in symbols) {
                if (!string.IsNullOrEmpty(s)) wishlistedSymbols.Add(s);
            }
        }
    }
}