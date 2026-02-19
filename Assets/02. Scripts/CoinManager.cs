using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public enum TimeSpeed
{
    Paused,
    Normal,
    Double
}

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    public TotalAssetPanelController assetPanelController;
    public TextMeshProUGUI dateText;
    public ChartPanelController chartPanelController;
    public StatusPanelController statusPanelController;

    private int tickCount = 0;
    private DateTime currentDateTime = new DateTime(2020, 1, 1, 9, 0, 0);
    public int survivalDays = 1;

    [Header("UI 연결")]
    public TextMeshProUGUI cashText;
    //public TextMeshProUGUI playerTotalAssetText;
    public TextMeshProUGUI cashTextLabel;
    public TextMeshProUGUI bankCashText;
    public TextMeshProUGUI mangoCashText;
    public TextMeshProUGUI lobbySatoshiText;

    public TimeSpeed currentSpeed = TimeSpeed.Normal;

    public List<CoinData> coins = new();
    public MarketPhase CurrentMarket = MarketPhase.Sideways;

    public Button pauseBtn;
    public Button normalBtn;
    public Button doubleBtn;

    public Color selectedColor = new Color32(50, 50, 50, 255);
    public Color defaultColor = new Color32(255, 255, 255, 0);

    public FourNanceManager fournanceManager;

    public enum AppType
    {
        Bullbit,
        SatoshiBank,
        Xbird,
        None,
        Gamble
    }

    public AppType currentApp = AppType.Bullbit;

    public DateTime CurrentDateTime => currentDateTime;
    public int TickCount => tickCount;

    public double GetStashoCash() => PlayerManager.Instance.satoshiBankCash;
    public bool IsTimePaused => UIPauseManager.IsPaused || currentSpeed == TimeSpeed.Paused;
    public event Action<DateTime> OnTimeAdvanced;
    //public event Action OnCoinListChanged;

    public event Action<DateTime> OnCandleBoundary;
    public System.Action OnMarketUpdated;
    private HashSet<string> dailySurgeAlerts = new HashSet<string>();
    private int lastRecordedDay = -1;

    public struct CoinChangeInfo {
        public CoinData coin;
        public double changeRate;
    }

    public void GetMajorDailyChanges(
    out List<CoinChangeInfo> topGainers,
    out List<CoinChangeInfo> topLosers) {
        List<CoinChangeInfo> all = new();

        foreach (var coin in coins) {
            if (coin.InitialPrice <= 0) continue;

            double rate =
                (coin.CurrentPrice - coin.InitialPrice) / coin.InitialPrice * 100.0;

            all.Add(new CoinChangeInfo {
                coin = coin,
                changeRate = rate
            });
        }

        topGainers = all
            .OrderByDescending(x => x.changeRate)
            .Take(2)
            .ToList();

        topLosers = all
            .OrderBy(x => x.changeRate)
            .Take(2)
            .ToList();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(InitializeRoutine());
        StartCoroutine(RealtimeUIUpdateRoutine());
    }

    IEnumerator RealtimeUIUpdateRoutine() {
        // 게임이 꺼질 때까지 무한 반복
        while (true) {
            // UI 텍스트 강제 갱신
            UpdateCashText();

            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    IEnumerator GameTickRoutine()
    {
        while (true)
        {
            if (UIPauseManager.IsPaused) {
                yield return null;
                continue;
            }

            float interval = GetUpdateInterval();
            if (interval == float.MaxValue)
            {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(interval);

            tickCount++;
            currentDateTime = currentDateTime.AddMinutes(30);
            OnTimeAdvanced?.Invoke(currentDateTime);

            // 매 프레임 혹은 매 틱마다 체크
            if (currentDateTime.Hour == 9 && currentDateTime.Minute == 0) {
                // 오늘 날짜에 아직 기록을 안 했다면 (오전 9시 정각에 최초 1회 실행)
                if (lastRecordedDay != currentDateTime.Day) {
                    // 1. 코인 시가 갱신 로직 (기존 유지)
                    foreach (var coin in coins) {
                        coin.InitialPrice = coin.CurrentPrice;
                    }
                    dailySurgeAlerts.Clear();

                    // 2. 차트 데이터 추가 (오전 9시 환율 기준)
                    if (ExchangeChartManager.Instance != null) {
                        float currentRate = (float)GlobalEconomyManager.UsdToKrw;
                        ExchangeChartManager.Instance.AddPriceData(currentRate);

                        // 오늘 기록 완료 표시 (날짜를 저장해서 9시 0분 동안 중복 실행 방지)
                        lastRecordedDay = currentDateTime.Day;
                        Debug.Log($"[차트 업데이트] 오전 9시 정각 환율 기록 완료: {currentRate}원");
                    }
                }
            }


            if (currentDateTime.Hour == 8 && currentDateTime.Minute == 0) {
                FearIndexManager.Instance?.RecalculateDailyFear();
            }


            if (currentDateTime.Hour == 0 && currentDateTime.Minute == 0) {
                survivalDays++;

                if (LoanManager.Instance != null) {
                    LoanManager.Instance.CheckLoanTick();
                }

                if (ProductManager.Instance != null) {
                    ProductManager.Instance.CheckAndProcessExpiry();
                }

                if (SatoshiBankPanel.Instance != null) {
                    SatoshiBankPanel.Instance.RefreshDepositStatus();
                }
                OfficeManager.Instance?.TryPaySalary();
                RealEstatePanelController.Instance?.ProcessDailyEstateIncome(currentDateTime);

                DailyIncomeManager.Instance?.FlushAndNotify();
            }

            if (IsFourHourBoundary(currentDateTime)) {

                foreach (var coin in coins) {

                    // 진행 중 RuntimeCandle 확정
                    coin.CloseRuntimeCandle();

                    // 다음 캔들 시작
                    double nextOpen =
                        coin.CandleHistory.Count > 0
                            ? coin.CandleHistory[^1].close
                            : coin.CurrentPrice;

                    coin.EnsureRuntimeCandle(nextOpen);

                    // BaseCandle은 그대로
                    coin.CloseBaseCandle();
                }

                OnCandleBoundary?.Invoke(currentDateTime);
            }



            GlobalEconomyManager.TickExchangeRate();

            foreach (var coin in coins) {
                var phase = coin.GetEffectivePhase(currentDateTime, CurrentMarket);
                var meta = Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == coin.Symbol);
                int vol = (meta != null) ? meta.VolatilityLevel : 1;

                // 이제 CoinData 내부에서 Type을 체크하므로, 
                // Stable 코인은 phase 영향을 받지 않고 환율만 따르게 됩니다.
                coin.GenerateNextPrice(phase, vol, 2f, 0f);

                CheckPriceSurgeAndNotify(coin);
            }

            OnMarketUpdated?.Invoke();
            if (tickCount % 1 == 0)
            {
                foreach (var coin in coins)
                {
                    //if (chartPanelController != null &&
                    //    chartPanelController.chartPanel.activeSelf &&
                    //    chartPanelController.currentCoin == coin)
                    //{
                    //    chartPanelController.chartRenderer.SetDataAndRender(coin, coin.CandleHistory);
                    //}
                }
            }

            UpdateDateText();
            UpdateCashText();
            if (assetPanelController != null)
            {
                assetPanelController.UpdatePlatformAssetTexts();
            }
            if (statusPanelController != null)
            {
                statusPanelController.UpdateAssetFromStatus();
            }
        }
    }

    //  급등/급락 체크 및 문자 발송 함수
    private void CheckPriceSurgeAndNotify(CoinData coin) {
        // 1. 미보유시 패스
        if (!PlayerManager.Instance.holdings.ContainsKey(coin.Symbol) ||
            PlayerManager.Instance.holdings[coin.Symbol] <= 0) return;

        // 2. 이미 알림 보냈으면 패스
        if (dailySurgeAlerts.Contains(coin.Symbol)) return;

        if (coin.InitialPrice <= 0) return;

        // 등락률 계산
        double changeRate = (coin.CurrentPrice - coin.InitialPrice) / coin.InitialPrice * 100.0;

        // [추가] 내가 산 가격(평단가)이 시가보다 훨씬 높으면(이미 떡상 후 매수), '오늘 급등' 알림은 뒷북일 수 있음.
        // 원하시면 이 주석을 풀어서 사용하세요. (평단가가 시가 대비 15% 이상 높으면 알림 스킵)
        // double myAvg = PlayerManager.Instance.GetAvgPrice(coin.Symbol);
        // if (myAvg > coin.InitialPrice * 1.15) return; 

        // 3. 급등 (+15% 이상)
        if (changeRate >= 15.0) {
            SendAlert(coin, "Bullbit", "[급등]", $"'{coin.Symbol}' 현재 {changeRate:F2}% 급등 중!");
        }
        // 4. 급락 (-15% 이하)
        else if (changeRate <= -15.0) {
            SendAlert(coin, "Bullbit", "[급락]", $"'{coin.Symbol}'현재 {changeRate:F2}% 급락 중!");
        }
    }
    // [신규] 매수 시 호출: 이미 변동폭이 큰 코인이면 알림 스킵 처리
    public void CheckAndSkipAlertForNewBuy(string symbol) {
        if (dailySurgeAlerts.Contains(symbol)) return; // 이미 등록됨

        var coin = coins.Find(c => c.Symbol == symbol);
        if (coin == null || coin.InitialPrice <= 0) return;

        double rate = (coin.CurrentPrice - coin.InitialPrice) / coin.InitialPrice * 100.0;

        // 이미 15% 이상 올랐거나 내린 상태에서 샀다면, 오늘 알림은 안 보냄 (뒷북 방지)
        if (Math.Abs(rate) >= 15.0) {
            dailySurgeAlerts.Add(symbol);
            Debug.Log($"[알림 스킵] {symbol}은 이미 {rate:F2}% 변동된 상태에서 매수함.");
        }
    }

    // 알림 발송 헬퍼
    private void SendAlert(CoinData coin, string type, string title, string msg) {
        if (GlobalNotificationManager.Instance != null) {
            GlobalNotificationManager.Instance.ShowNotification(type, title, msg);
        }
        // 알림 보냈음 표시
        dailySurgeAlerts.Add(coin.Symbol);
    }

    void UpdateDateText()
    {
        string dateStr = currentDateTime.ToString("MM/dd/yyyy  HH:mm");
        dateText.text = $"{dateStr} {survivalDays}일차";
    }

    public double GetBullbitAsset()
    {
        double total = PlayerManager.Instance.bullbitCash;
        foreach (var coin in coins)
        {
            if (PlayerManager.Instance.holdings.TryGetValue(coin.Symbol, out double amount))
            {
                total += amount * coin.CurrentPrice;
            }
        }
        return total;
    }

    public double GetCompanyCapital() => OfficeManager.Instance.companyCapital;

    public double GetSatoshiBankAsset() => PlayerManager.Instance.satoshiBankCash;
    public double GetTotalUserAsset() => GetBullbitAsset() + GetSatoshiBankAsset() + GetCompanyCapital();
    //public double GetTotalUserAsset() => GetBullbitAsset() + GetSatoshiBankAsset() + GetGambleCashFromSatoshiBank(); // 구버전

    public void UpdateCashText() {
        double total = GetTotalUserAsset();
        double bullbit = GetBullbitAsset();
        double bank = GetSatoshiBankAsset();
        double satoshiCash = GetStashoCash();

        if (fournanceManager != null && fournanceManager.gameObject.activeInHierarchy) {
            fournanceManager.RefreshUI();
        }

        // [기존] 불비트 자산
        if (cashText != null)
            cashText.text = $"불비트 자산: {bullbit:N0}원";

        // [기존] 사토시 뱅크 앱을 켰을 때만 보이는 텍스트
        if (bankCashText != null) {
            if (currentApp == AppType.SatoshiBank) {
                bankCashText.text = $"₩{bank:N0}원";
            }
        }

    
        if (lobbySatoshiText != null) {
            lobbySatoshiText.text = $"사토시 은행 잔고 {bank:N0}원"; 
        }

        // [기존] 망고 캐시
        if (mangoCashText != null) {
            mangoCashText.text = $"{statusPanelController.playerNameText.text}님의 잔액: {satoshiCash:N0}원";
        }
    }

    public void SetTimeSpeed(TimeSpeed speed)
    {
        currentSpeed = speed;
        UpdateSpeedButtonVisuals();
    }

    void UpdateSpeedButtonVisuals()
    {
        pauseBtn.image.color = (currentSpeed == TimeSpeed.Paused) ? selectedColor : defaultColor;
        normalBtn.image.color = (currentSpeed == TimeSpeed.Normal) ? selectedColor : defaultColor;
        doubleBtn.image.color = (currentSpeed == TimeSpeed.Double) ? selectedColor : defaultColor;
    }

    public void OnPauseButtonClicked() => SetTimeSpeed(TimeSpeed.Paused);
    public void OnNormalSpeedButtonClicked() => SetTimeSpeed(TimeSpeed.Normal);
    public void OnDoubleSpeedButtonClicked() => SetTimeSpeed(TimeSpeed.Double);

    public float GetUpdateInterval()
    {
        return currentSpeed switch
        {
            TimeSpeed.Paused => float.MaxValue,
            TimeSpeed.Normal => 1f,
            TimeSpeed.Double => 0.2f,
            _ => 2f
        };
    }

    public void ListNewCoin(string symbol) {
        var existingCoin = coins.Find(c => c.Symbol == symbol);

        if (existingCoin != null && existingCoin.IsListed) {
            Debug.LogWarning($"[ListNewCoin] 코인 '{symbol}'은(는) 이미 상장되어 활성화된 상태입니다.");
            return;
        }

        if (existingCoin != null) {
            existingCoin.IsListed = true;
            existingCoin.IsDelisted = false;

            var meta = Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);
            if (meta != null) meta.BullbitListed = true;

            Debug.Log($"[ListNewCoin] 기존 데이터 '{symbol}'을(를) 상장 상태로 전환했습니다.");
        } else {
            var meta = Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);
            if (meta == null) {
                Debug.LogError($"[ListNewCoin] '{symbol}' 메타 데이터를 찾을 수 없습니다.");
                return;
            }

            meta.BullbitListed = true;
            existingCoin = new CoinData(meta);
            existingCoin.IsListed = true;
            coins.Add(existingCoin);

            Debug.Log($"[ListNewCoin] 신규 코인 '{symbol}'이(가) 시장에 추가되었습니다.");
        }

        var uiManager = FindAnyObjectByType<MainUIManager>();
        if (uiManager != null) {
            uiManager.AddCoinRow(existingCoin);
        }

        assetPanelController?.RenderPlatformRows();
    }

    public void DelistCoin(string symbol) {
        var coin = coins.Find(c => c.Symbol == symbol);
        if (coin == null)
            return;

        if (coin.IsDelisted)
            return;

        // 메타데이터 반영
        var meta = Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);
        if (meta != null)
            meta.BullbitListed = false;

        // 코인 상태 변경
        coin.IsDelisted = true;
        coin.CurrentPrice = 0;
        coin.InitialPrice = 0; 

        coin.CurrentPhaseOverride = MarketPhase.MegaBear;
        coin.PhaseOverrideEndTime = DateTime.MaxValue;

        // UI 갱신
        assetPanelController?.RenderPlatformRows();

        // 메인 코인 리스트 갱신
        var uiManager = FindAnyObjectByType<MainUIManager>();
        uiManager?.RefreshCoinRows();

        var ui = FindAnyObjectByType<MainUIManager>();
        if (ui != null) {
            ui.RefreshCoinRows();
        }


        Debug.Log($"[상폐 → 비상장] {symbol}");
    }

    public void RelistCoin(string symbol, double basePrice) {
        var coin = coins.Find(c => c.Symbol == symbol);
        if (coin == null) {
            Debug.LogWarning($"[RelistCoin] 코인 없음: {symbol}");
            return;
        }

        coin.ApplyRelist(basePrice); 
        // 메타데이터 복구 (이거 중요)
        var meta = Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);
        if (meta != null)
            meta.BullbitListed = true;

        Debug.Log($"[CoinManager] 재상장 처리: {symbol}");

        // UI 즉시 반영
        assetPanelController?.RenderPlatformRows();

        var ui = FindAnyObjectByType<MainUIManager>();
        ui?.RefreshCoinRows();
    }



    public void SetTickCount(int value) {
        tickCount = value;
    }
    public void SetDateTime(DateTime dt) {
        currentDateTime = dt;
    }
    IEnumerator InitializeRoutine() {
        foreach (var meta in CoinMetaDatabase.AllCoins) {
            // 불비트 취급 코인이면 일단 무조건 생성 (그래야 비상장 표시 가능)
            if (meta.BullbitListed) {
                // [중요] 생성자 하나로 모든 확률/테마/타입 결정 끝!
                var coin = new CoinData(meta);
                coins.Add(coin);
            }
        }

        // UI 및 시간 흐름 시작 (기존 로직 유지)
        UpdateCashText();
        UpdateDateText();
        UpdateSpeedButtonVisuals();
        if (assetPanelController != null) assetPanelController.RenderPlatformRows();

        yield return null;

        foreach (var coin in coins) {
            coin.EnsureRuntimeCandle(coin.CurrentPrice);
        }

        UpdateCashText();
        UpdateDateText();
        Canvas.ForceUpdateCanvases();

        StartCoroutine(GameTickRoutine());
        GameBootState.playerReady = true;
    }

    public bool HasCoinMeta(string symbol) {
        return Array.Exists(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);
    }

    public bool IsCoinListedOnBullbit(string symbol) {
        var meta = Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);
        if (meta == null) return false;

        return meta.BullbitListed;
    }
    public void RenderBankCashOnce() {
        if (bankCashText == null) return;

        double bank = PlayerManager.Instance.satoshiBankCash;
        bankCashText.text = $"₩{bank:N0}원";
    }

    bool IsFourHourBoundary(DateTime time) {
        // 기준: 게임 시작 시각 = 09:00
        int hoursSinceStart =
            (int)(time - new DateTime(time.Year, time.Month, time.Day, 9, 0, 0)).TotalHours;

        if (hoursSinceStart < 0)
            hoursSinceStart += 24;

        return time.Minute == 0 && hoursSinceStart % 4 == 0;
    }
    public string GetCurrentDateString() {

        return currentDateTime.ToString("yyyy/MM/dd");
    }

    // [추가] 이벤트 매니저가 호출할 신규 상장 함수
    public void ListNewCoinFromMeta(string symbol) {
        // 1. 중복 체크
        if (coins.Exists(c => c.Symbol == symbol)) return;

        // 2. 메타 데이터 찾기
        var meta = Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);
        if (meta == null) {
            Debug.LogError($"[CoinManager] 메타 데이터 없음: {symbol}");
            return;
        }

        // 3. 코인 생성 (InitializeRoutine과 똑같은 방식 사용)
        CoinType type = (meta.Theme == CoinTheme.Stable) ? CoinType.Stable : CoinType.Normal;
        var newCoin = new CoinData(meta);

        // 추가 속성 할당 (CoinData에 해당 필드가 있다면 주석 해제)
        // newCoin.Theme = meta.Theme.ToString();
        // newCoin.Volatility = meta.VolatilityLevel;
        newCoin.IsListed = true;
        newCoin.IsDelisted = false;

        // 4. 리스트 추가
        coins.Add(newCoin);

        // 5. UI 갱신
        var uiManager = FindAnyObjectByType<MainUIManager>();
        if (uiManager != null) {
            uiManager.AddCoinRow(newCoin);
        }
        assetPanelController?.RenderPlatformRows();

        Debug.Log($"[이벤트 상장] {meta.Name}({meta.Symbol}) 거래 개시!");
    }

}
