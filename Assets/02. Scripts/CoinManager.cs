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
    private DateTime currentDateTime = new DateTime(2016, 1, 1, 9, 0, 0);
    public int survivalDays = 1;

    [Header("UI 연결")]
    public TextMeshProUGUI cashText;
    //public TextMeshProUGUI playerTotalAssetText;
    public TextMeshProUGUI cashTextLabel;
    public TextMeshProUGUI bankCashText;
    public TextMeshProUGUI mangoCashText;

    public TimeSpeed currentSpeed = TimeSpeed.Normal;

    public List<CoinData> coins = new();
    public MarketPhase CurrentMarket = MarketPhase.Sideways;

    public Button pauseBtn;
    public Button normalBtn;
    public Button doubleBtn;

    public Color selectedColor = new Color32(50, 50, 50, 255);
    public Color defaultColor = new Color32(255, 255, 255, 0);

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
    public bool IsTimePaused => UIPauseManager.IsPaused;



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
        //foreach (var meta in CoinMetaDatabase.AllCoins)
        //{
        //    if (meta.BullbitListed)
        //    {
        //        var coin = new CoinData(meta.Name, meta.Symbol, meta.InitialPrice, meta.MaxSupply);
        //        coins.Add(coin);
        //    }
        //}

        //UpdateCashText();
        //UpdateDateText();
        //UpdateSpeedButtonVisuals();
        //assetPanelController.RenderPlatformRows();

        //StartCoroutine(GameTickRoutine());
        StartCoroutine(InitializeRoutine());
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

            if (currentDateTime.Hour == 9 && currentDateTime.Minute == 0)
            {   
                foreach (var coin in coins)
                {
                    coin.InitialPrice = coin.CurrentPrice;
                }
            }

            if (currentDateTime.Hour == 8 && currentDateTime.Minute == 0) {
                FearIndexManager.Instance?.RecalculateDailyFear();
            }


            if (currentDateTime.Hour == 0 && currentDateTime.Minute == 0)
            {
                survivalDays++;
            }

            foreach (var coin in coins)
            {
                var phase = coin.GetEffectivePhase(currentDateTime, CurrentMarket);
                coin.GenerateNextPrice(phase, 2f, 0f);
            }

            if (tickCount % 2 == 0)
            {
                foreach (var coin in coins)
                {
                    coin.RecordCurrentCandle();
                    if (chartPanelController != null &&
                        chartPanelController.chartPanel.activeSelf &&
                        chartPanelController.currentCoin == coin)
                    {
                        chartPanelController.chartRenderer.SetDataAndRender(coin, coin.CandleHistory);
                    }
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

    public double GetSatoshiBankAsset() => PlayerManager.Instance.satoshiBankCash;
    public double GetTotalUserAsset() => GetBullbitAsset() + GetSatoshiBankAsset();
    //public double GetTotalUserAsset() => GetBullbitAsset() + GetSatoshiBankAsset() + GetGambleCashFromSatoshiBank(); // 구버전

    public void UpdateCashText()
    {
        double total = GetTotalUserAsset();
        double bullbit = GetBullbitAsset();
        double bank = GetSatoshiBankAsset();
        double satoshiCash = GetStashoCash();

        //if (playerTotalAssetText != null)
        //    playerTotalAssetText.text = $"총자산: {total:N0} KRW";

        if (cashText != null)
            cashText.text = $"불비트 자산: {bullbit:N0}원";

        if (bankCashText != null)
        {
            if (currentApp == AppType.SatoshiBank)
            {
                bankCashText.text = $"₩{bank:N0}원";
            }
        }

        if (mangoCashText != null)
        {
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
            TimeSpeed.Normal => 2f,
            TimeSpeed.Double => 0.25f,
            _ => 2f
        };
    }
    
    public void ListNewCoin(string symbol)
    {
        if (coins.Exists(c => c.Symbol == symbol))
        {
            Debug.LogWarning($"코인 '{symbol}'은(는) 이미 상장되어 있습니다.");
            return;
        }

        var meta = Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);
        if (meta == null)
        {
            Debug.LogError($"상장하려는 코인 '{symbol}'을(를) 데이터베이스에서 찾을 수 없습니다.");
            return;
        }

        meta.BullbitListed = true;

        var coin = new CoinData(meta.Name, meta.Symbol, meta.InitialPrice, meta.MaxSupply);
        coins.Add(coin);

        // 메인 UI 갱신
        var uiManager = FindAnyObjectByType<MainUIManager>();
        if (uiManager != null) {
            uiManager.AddCoinRow(coin);
        }

        // 자산 패널 갱신
        assetPanelController?.RenderPlatformRows();

        Debug.Log($"[신규 상장] 코인 '{symbol}'이(가) 시장에 추가되었습니다.");
    }

    public void SetTickCount(int value) {
        tickCount = value;
    }
    public void SetDateTime(DateTime dt) {
        currentDateTime = dt;
    }
    IEnumerator InitializeRoutine() {
        
        foreach (var meta in CoinMetaDatabase.AllCoins) {
            if (meta.BullbitListed) {
                var coin = new CoinData(meta.Name, meta.Symbol, meta.InitialPrice, meta.MaxSupply);
                coins.Add(coin);
            }
        }

        UpdateCashText();
        UpdateDateText();
        UpdateSpeedButtonVisuals();
        assetPanelController.RenderPlatformRows();

       // 한 프레임 대기
        yield return null;

        //  UI 강제 최종 동기화 (0.2초 문제 해결 지점)
        UpdateCashText();
        UpdateDateText();
        Canvas.ForceUpdateCanvases();

        // 이제 틱 시작
        StartCoroutine(GameTickRoutine());

        // 여기서 준비 완료 선언
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
}
