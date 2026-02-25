using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class ResearchDetailController : MonoBehaviour {
    [Header("Main Panels")]
    public GameObject detailPanel;
    public GameObject listPanel;
    public Button backBtn;

    [Header("Top Info")]
    public Image coinIcon;
    public TextMeshProUGUI nameAndSymbolTopText;
    public TextMeshProUGUI currentPriceText;
    public TextMeshProUGUI changePercentText;

    [Header("Tabs")]
    public Button overviewBtn;
    public Button newsBtn;
    private Color32 activeTabColor = new Color32(20, 255, 8, 255);

    [Header("Tab Contents")]
    public GameObject overviewGroup;
    public GameObject newsGroup;

    [Header("Overview Content")]
    public TextMeshProUGUI titleNameSymbolText;
    public TextMeshProUGUI descriptionText;

    public TextMeshProUGUI circulatingVolumeText;
    public TextMeshProUGUI marketCapText;

    public TextMeshProUGUI currentCirculatingSupplyText;
    public TextMeshProUGUI totalSupplyText;

    public TextMeshProUGUI athText;
    public TextMeshProUGUI atlText;

    public TextMeshProUGUI riskGradeText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI contractAddressText;
    public TextMeshProUGUI proofTypeText;

    [Header("Sub Panels")]
    public LockupHalvingInfoPanel lockupInfoPanel;

    [Header("Chart Connection")]
    public CryptoChartManager chartManager;
    public Button tickChartBtn;
    public Button dailyChartBtn;
    private bool isDailyChart = false;

    private CoinData currentCoin;
    public string CurrentCoinSymbol => currentCoin != null ? currentCoin.Symbol : "";

    void Start() {
        if (backBtn != null) backBtn.onClick.AddListener(OnClickBack);

        if (overviewBtn != null) overviewBtn.onClick.AddListener(() => {
            UpdateTabUI(overviewBtn);
            ShowTabGroup(true);
        });

        if (newsBtn != null) newsBtn.onClick.AddListener(() => {
            UpdateTabUI(newsBtn);
            ShowTabGroup(false);
        });

        if (tickChartBtn != null) tickChartBtn.onClick.AddListener(() => SetChartMode(false));
        if (dailyChartBtn != null) dailyChartBtn.onClick.AddListener(() => SetChartMode(true));
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
        if (detailPanel != null) detailPanel.SetActive(false);
        if (listPanel != null) listPanel.SetActive(true);
    }

    private void HandleMarketUpdated() {
        if (currentCoin == null || !detailPanel.activeSelf) return;

        RefreshTextUI();
        RefreshChart();
    }

    public void OpenPanel(CoinData coin) {
        currentCoin = coin;
        listPanel.SetActive(false);
        detailPanel.SetActive(true);

        SetChartMode(false);
        RefreshTextUI();

        UpdateTabVisual(true);
        ShowTabGroup(true);
    }

    public void ShowTabGroup(bool isOverview) {
        if (overviewGroup != null) overviewGroup.SetActive(isOverview);
        if (newsGroup != null) newsGroup.SetActive(!isOverview);
    }

    public void UpdateTabVisual(bool isOverview) {
        SetTabStyle(overviewBtn, isOverview);
        SetTabStyle(newsBtn, !isOverview);
    }
    private void SetTabStyle(Button btn, bool isActive) {
        if (btn == null) return;
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) {
            txt.color = isActive ? activeTabColor : Color.white;
            txt.fontStyle = isActive ? FontStyles.Underline : FontStyles.Normal;
        }
    }

    public void OnClickBack() {
        detailPanel.SetActive(false);
        listPanel.SetActive(true);
    }

    public void SetChartMode(bool toDaily) {
        isDailyChart = toDaily;
        UpdateChartTabUI();
        RefreshChart();
    }

    private void UpdateChartTabUI() {
        if (tickChartBtn != null) {
            var txt = tickChartBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = isDailyChart ? Color.white : activeTabColor;
        }
        if (dailyChartBtn != null) {
            var txt = dailyChartBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = isDailyChart ? activeTabColor : Color.white;
        }
    }

    private void RefreshChart() {
        if (chartManager == null || currentCoin == null) return;

        List<double> finalChartData = new List<double>();

        if (isDailyChart) {
            if (currentCoin.DailyHistory != null && currentCoin.DailyHistory.Count > 0) {
                finalChartData.AddRange(currentCoin.DailyHistory);
            } else {
                finalChartData.Add(currentCoin.InitialPrice);
            }
            finalChartData.Add(currentCoin.CurrentPrice);
        } else {
            if (currentCoin.PriceHistory != null) {
                finalChartData.AddRange(currentCoin.PriceHistory);
            }
        }
        chartManager.DrawChart(finalChartData);
    }

    //   public으로 유지 및 GetThemeNameKR 추가
    public void RefreshTextUI() {
        if (currentCoin == null) return;
        var meta = CoinMetaDatabase.AllCoins.FirstOrDefault(m => m.Symbol == currentCoin.Symbol);

        bool isUsd = false;
        var parent = GetComponentInParent<ResearchPanelController>();
        if (parent != null) isUsd = parent.ShowInUsd;

        if (meta != null && lockupInfoPanel != null) {
            lockupInfoPanel.Setup(currentCoin, meta, isUsd);
        }

        if (coinIcon != null) coinIcon.sprite = Resources.Load<Sprite>($"Coins/{currentCoin.Symbol}");
        if (nameAndSymbolTopText != null) nameAndSymbolTopText.text = $"{currentCoin.Name}\n({currentCoin.Symbol})";

        if (currentPriceText != null) {
            currentPriceText.text = isUsd
                ? $"가격\n{GetFormattedPriceUSD(currentCoin.CurrentPrice)}"
                : $"가격\n{currentCoin.GetFormattedPriceKRW().Replace("₩", "")} 원";
        }

        double change = currentCoin.InitialPrice > 0 ? ((currentCoin.CurrentPrice - currentCoin.InitialPrice) / currentCoin.InitialPrice) * 100.0 : 0;
        if (changePercentText != null) {
            changePercentText.text = $"등락률\n{change:+0.##;-0.##}%";
            changePercentText.color = change > 0 ? activeTabColor : (change < 0 ? Color.red : Color.white);
        }

        if (meta != null) {
            double mc = currentCoin.CurrentPrice * currentCoin.CirculatingSupply;
            double fdv = currentCoin.CurrentPrice * currentCoin.MaxSupply;

            if (circulatingVolumeText != null) circulatingVolumeText.text = FormatCurrency(mc, isUsd);
            if (marketCapText != null) marketCapText.text = FormatCurrency(fdv, isUsd);

            if (currentCirculatingSupplyText != null) currentCirculatingSupplyText.text = $"{currentCoin.Symbol} {currentCoin.CirculatingSupply:N0}";
            if (totalSupplyText != null) totalSupplyText.text = $"{currentCoin.Symbol} {currentCoin.MaxSupply:N0}";

            if (athText != null)
                athText.text = isUsd ? GetFormattedPriceUSD(currentCoin.AllTimeHigh) : $"{currentCoin.AllTimeHigh:N0} 원";

            if (atlText != null)
                atlText.text = isUsd ? GetFormattedPriceUSD(currentCoin.AllTimeLow) : $"{currentCoin.AllTimeLow:N0} 원";

            if (titleNameSymbolText != null) titleNameSymbolText.text = $"{currentCoin.Name} ({currentCoin.Symbol})";
            if (descriptionText != null) descriptionText.text = meta.Description;
            if (contractAddressText != null) contractAddressText.text = meta.ContractAddress;
            if (proofTypeText != null) proofTypeText.text = $"[{meta.Proof.ToString()}]";
            if (riskGradeText != null) riskGradeText.text = meta.GetRiskGrade();

            //   여기서 호출되는 함수
            if (categoryText != null) categoryText.text = GetThemeNameKR(meta.Theme);
        }
    }

    public string FormatCurrency(double krwAmount, bool isUsd) {
        if (isUsd) {
            double usdAmount = krwAmount / GlobalEconomyManager.UsdToKrw;

            // 달러($) 단위 확장: T(조), B(십억)를 넘어 Q(경)까지
            if (usdAmount >= 1_000_000_000_000_000d) return $"${(usdAmount / 1_000_000_000_000_000d):F2}Q"; // Quadrillion (경)
            if (usdAmount >= 1_000_000_000_000d) return $"${(usdAmount / 1_000_000_000_000d):F2}T";      // Trillion (조)
            if (usdAmount >= 1_000_000_000d) return $"${(usdAmount / 1_000_000_000d):F2}B";           // Billion (십억)
            if (usdAmount >= 1_000_000d) return $"${(usdAmount / 1_000_000d):F2}M";                // Million (백만)
            if (usdAmount >= 1_000d) return $"${(usdAmount / 1_000d):F2}K";                        // Thousand (천)
            return $"${usdAmount:N2}";
        } else {
            // 원화(₩) 단위 확장: 조(兆)를 넘어 경(京), 해(垓)까지
            // 해(垓) = 10의 20승 (1,0000 * 경)
            if (krwAmount >= 1_0000_0000_0000_0000_0000d) return $"{(krwAmount / 1_0000_0000_0000_0000_0000d):F2}해 원";
            // 경(京) = 10의 16승 (1,0000 * 조)
            if (krwAmount >= 1_0000_0000_0000_0000d) return $"{(krwAmount / 1_0000_0000_0000_0000d):F2}경 원";
            // 조(兆) = 10의 12승 (1,0000 * 억)
            if (krwAmount >= 1_0000_0000_0000d) return $"{(krwAmount / 1_0000_0000_0000d):F2}조 원";
            // 억(億) = 10의 8승
            if (krwAmount >= 1_0000_0000d) return $"{(krwAmount / 1_0000_0000d):F2}억 원";
            // 만(萬) = 10의 4승
            if (krwAmount >= 1_0000d) return $"{(krwAmount / 1_0000d):F2}만 원";

            return $"{krwAmount:N0} 원";
        }
    }

    public string GetFormattedPriceUSD(double krwPrice) {
        double usd = krwPrice / GlobalEconomyManager.UsdToKrw;
        if (usd >= 1.0) return $"${usd:N2}";
        if (usd >= 0.001) return $"${usd:N4}";
        return $"${usd:F6}";
    }

    //   에러 원인 해결: 테마 이름을 가져오는 함수 추가
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

    public void UpdateTabUI(Button selectedBtn) {
        ResetTab(overviewBtn);
        ResetTab(newsBtn);
        var txt = selectedBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) {
            txt.color = activeTabColor;
            txt.fontStyle = FontStyles.Underline;
        }
    }

    private void ResetTab(Button btn) {
        if (btn == null) return;
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) {
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Normal;
        }
    }
}