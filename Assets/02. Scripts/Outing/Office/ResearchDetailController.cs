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
    public TextMeshProUGUI nameAndSymbolTopText; // "비트코인\n(BTC)"
    public TextMeshProUGUI currentPriceText;     // "가격\n134,300,000"
    public TextMeshProUGUI changePercentText;    // "등락률\n2%"

    [Header("Tabs")]
    public Button overviewBtn;
    public Button marketBtn;
    public Button newsBtn;
    private Color32 activeTabColor = new Color32(20, 255, 8, 255); // R20 G255 B8

    [Header("Overview Content")]
    public TextMeshProUGUI titleNameSymbolText;  // "비트코인 (BTC)"
    public TextMeshProUGUI descriptionText;      // 메타데이터 설명

    public TextMeshProUGUI circulatingVolumeText; // 좌측 유통규모 (MC)
    public TextMeshProUGUI marketCapText;         // 우측 시가총액 (FDV)

    public TextMeshProUGUI currentCirculatingSupplyText; // "BTC 19,600,000"
    public TextMeshProUGUI totalSupplyText;              // "BTC 21,000,000"

    public TextMeshProUGUI athText; // 역대 최고가
    public TextMeshProUGUI atlText; // 역대 최저가

    public TextMeshProUGUI riskGradeText; // "S+"
    public TextMeshProUGUI categoryText;  // "레이어 1"

    [Header("Chart Connection")]
    public CryptoChartManager chartManager;
    public Button tickChartBtn;   // '30분' 버튼 연결
    public Button dailyChartBtn;  // '1D' 버튼 연결
    private bool isDailyChart = false;

    private CoinData currentCoin;

    void Start() {
        if (backBtn != null) backBtn.onClick.AddListener(OnClickBack);
        if (overviewBtn != null) overviewBtn.onClick.AddListener(() => UpdateTabUI(overviewBtn));
        if (marketBtn != null) marketBtn.onClick.AddListener(() => UpdateTabUI(marketBtn));
        if (newsBtn != null) newsBtn.onClick.AddListener(() => UpdateTabUI(newsBtn));

        // [NEW] 30분 / 1D 버튼 이벤트 연결
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
        RefreshChart(); // [수정] AddPriceData 대신 RefreshChart 하나만 호출!
    }

    public void OpenPanel(CoinData coin) {
        currentCoin = coin;
        listPanel.SetActive(false);
        detailPanel.SetActive(true);

        SetChartMode(false); // [수정] 창 열 때 무조건 '30분봉' 모드로 초기화 및 차트 그리기

        RefreshTextUI();
        UpdateTabUI(overviewBtn); // 창 열면 무조건 개요 탭
    }

    public void OnClickBack() {
        detailPanel.SetActive(false);
        listPanel.SetActive(true);
    }

    // [NEW] 30분 / 1D 모드 변경 및 차트 다시 그리기
    public void SetChartMode(bool toDaily) {
        isDailyChart = toDaily;
        UpdateChartTabUI();
        RefreshChart();
    }

    // [NEW] 차트 탭 버튼 색상 연출
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

    private void RefreshTextUI() {
        if (currentCoin == null) return;
        var meta = CoinMetaDatabase.AllCoins.FirstOrDefault(m => m.Symbol == currentCoin.Symbol);

        // 1. 상단 정보
        coinIcon.sprite = Resources.Load<Sprite>($"Coins/{currentCoin.Symbol}");
        nameAndSymbolTopText.text = $"{currentCoin.Name}\n({currentCoin.Symbol})";

        currentPriceText.text = currentCoin.CurrentPrice < 10
            ? $"가격\n{currentCoin.CurrentPrice:N4} 원"
            : $"가격\n{currentCoin.CurrentPrice:N0} 원";

        double change = currentCoin.InitialPrice > 0 ? ((currentCoin.CurrentPrice - currentCoin.InitialPrice) / currentCoin.InitialPrice) * 100.0 : 0;
        changePercentText.text = $"등락률\n{change:+0.##;-0.##}%";
        changePercentText.color = change > 0 ? activeTabColor : (change < 0 ? Color.red : Color.white);

        // 2. 개요(Overview) 본문 정보
        if (meta != null) {
            titleNameSymbolText.text = $"{currentCoin.Name} ({currentCoin.Symbol})";
            descriptionText.text = meta.Description;

            // [수정] 유통규모(MC) vs 시가총액(FDV) 확실한 분리!
            double mc = currentCoin.CurrentPrice * meta.CirculatingSupply; // 유통규모
            double fdv = currentCoin.CurrentPrice * meta.MaxSupply;        // 시가총액 (완전희석)

            circulatingVolumeText.text = FormatKoreanCurrency(mc);
            marketCapText.text = FormatKoreanCurrency(fdv);

            // 심볼 + 유통량
            currentCirculatingSupplyText.text = $"{currentCoin.Symbol} {meta.CirculatingSupply:N0}";
            totalSupplyText.text = $"{currentCoin.Symbol} {meta.MaxSupply:N0}";

            riskGradeText.text = meta.GetRiskGrade();
            categoryText.text = GetThemeNameKR(meta.Theme);

            // ATH/ATL 임시 처리
            athText.text = currentCoin.AllTimeHigh < 10
                            ? $"{currentCoin.AllTimeHigh:N4} 원"
                            : $"{currentCoin.AllTimeHigh:N0} 원";

            atlText.text = currentCoin.AllTimeLow < 10
                ? $"{currentCoin.AllTimeLow:N4} 원"
                : $"{currentCoin.AllTimeLow:N0} 원";
        }
    }

    // 단위 정밀 파싱
    private string FormatKoreanCurrency(double amount) {
        if (amount >= 1_0000_0000_0000_0000_0000d) { // 1해 이상
            long hae = (long)(amount / 1_0000_0000_0000_0000_0000d);
            long kyung = (long)((amount % 1_0000_0000_0000_0000_0000d) / 1_0000_0000_0000_0000d);
            return kyung > 0 ? $"{hae}해 {kyung}경 원" : $"{hae}해 원";
        }
        if (amount >= 1_0000_0000_0000_0000d) { // 1경 이상
            long kyung = (long)(amount / 1_0000_0000_0000_0000d);
            long jo = (long)((amount % 1_0000_0000_0000_0000d) / 1_0000_0000_0000d);
            return jo > 0 ? $"{kyung}경 {jo}조 원" : $"{kyung}경 원";
        }
        if (amount >= 1_0000_0000_0000d) { // 1조 이상
            long jo = (long)(amount / 1_0000_0000_0000d);
            long uk = (long)((amount % 1_0000_0000_0000d) / 1_0000_0000d);
            return uk > 0 ? $"{jo}조 {uk}억 원" : $"{jo}조 원";
        }
        if (amount >= 1_0000_0000d) { // 1억 이상
            long uk = (long)(amount / 1_0000_0000d);
            long man = (long)((amount % 1_0000_0000d) / 1_0000d);
            return man > 0 ? $"{uk}억 {man}만 원" : $"{uk}억 원";
        }
        return $"{amount:N0} 원";
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

    // 탭 선택 시 언더라인 및 컬러 연출
    public void UpdateTabUI(Button selectedBtn) {
        ResetTab(overviewBtn);
        ResetTab(marketBtn);
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