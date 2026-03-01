using UnityEngine;
using System;
using System.Collections.Generic;

public class CoinEventScenarioManager : MonoBehaviour {
    public static CoinEventScenarioManager Instance { get; private set; }

    private int sessionSeedOffset;
    private HashSet<string> processedKeys = new HashSet<string>();

    void Awake() {
        Instance = this;
        sessionSeedOffset = UnityEngine.Random.Range(0, 999999);
        Debug.Log($"<color=#00FFFF>[시스템]</color> CoinEventScenarioManager 가동");
    }

    public void CheckAndExecuteScenarios(DateTime now, List<CoinData> activeCoins) {
        foreach (var coin in activeCoins) {
            if (!coin.IsListed || coin.IsDelisted) continue;

            var meta = Array.Find(CoinMetaDatabase.AllCoins, m => m.Symbol == coin.Symbol);
            if (meta == null) continue;

            if (meta.UnlockCycleMonths > 0) HandleUnlockScenario(coin, meta, now);
            if (meta.HalvingCycleMonths > 0) HandleHalvingScenario(coin, meta, now);
        }
    }

    #region [락업 해제 시나리오]
    private void HandleUnlockScenario(CoinData coin, CoinMetaData meta, DateTime now) {
        DateTime nextUnlockDate = CalculateNextEventDate(meta.UnlockCycleMonths, coin.Symbol, now);
        int daysLeft = (nextUnlockDate.Date - now.Date).Days;

        int scenarioSeed = coin.Symbol.GetHashCode() + nextUnlockDate.DayOfYear + nextUnlockDate.Year + sessionSeedOffset;
        UnityEngine.Random.InitState(scenarioSeed);
        int scenarioDice = UnityEngine.Random.Range(0, 100);
        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

        // 유통량 증가 (기존 로직 유지)
        if (daysLeft == 0) {
            string todayStr = now.ToString("yyyyMMdd");
            string supplyKey = $"lockup_applied_{coin.Symbol}_{todayStr}";

            if (!processedKeys.Contains(supplyKey)) {
                long maxSupply = coin.MaxSupply;
                long currentSupply = coin.CirculatingSupply;
                long remainingSupply = maxSupply - currentSupply;

                if (remainingSupply > 0) {
                    int amountSeed = coin.Symbol.GetHashCode() + now.DayOfYear + now.Year;
                    UnityEngine.Random.InitState(amountSeed);
                    float unlockRatio = (remainingSupply / (float)maxSupply <= 0.20f)
                                        ? (remainingSupply / (float)maxSupply)
                                        : UnityEngine.Random.Range(0.05f, 0.15f);
                    UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

                    long unlockAmount = (long)(maxSupply * unlockRatio);
                    coin.CirculatingSupply += unlockAmount;
                    meta.CirculatingSupply += unlockAmount;
                    if (coin.CirculatingSupply > maxSupply) coin.CirculatingSupply = maxSupply;

                    string msg = $"{coin.Name}({coin.Symbol})의 유통 물량 {unlockAmount:N0}개가 정식 해제되었습니다.";
                    SendNews($"news_unlock_{coin.Symbol}_{todayStr}", "[공시]", coin.Symbol, msg);

                    processedKeys.Add(supplyKey);
                }
            }
        }

        //  가격 변동 수치 대폭 하향 조정 (30분 틱 기준)
        if (daysLeft <= 14 && daysLeft >= -3) {
            string scenarioName = GetScenarioName(scenarioDice);

            // 매번 찍히면 시끄러우니, 날짜가 바뀔 때(0시 0분) 한 번만 찍히도록 권장하지만, 
            // 현재는 행님이 테스트하기 편하시게 조건문에 들어오면 바로 찍히게 두겠습니다.
            Debug.Log($"<color=#FFD700>[락업 시나리오 감지]</color> <b>{coin.Symbol}</b>: {scenarioName} (D-{daysLeft})");

            if (scenarioDice < 25) ScenarioA(coin, daysLeft, now);
            else if (scenarioDice < 45) ScenarioB(coin, daysLeft, now);
            else if (scenarioDice < 65) ScenarioC(coin, daysLeft, now);
            else if (scenarioDice < 90) ScenarioD(coin, daysLeft, now, scenarioSeed, meta.VolatilityLevel);
            else ScenarioE(coin, daysLeft);
        }
    }

    // A: 덤핑 (폭락)
    private void ScenarioA(CoinData coin, int daysLeft, DateTime now) {
        if (daysLeft == 0) {
            string instantDumpKey = $"instant_dump_{coin.Symbol}_{now:yyyyMMdd}";
            if (!processedKeys.Contains(instantDumpKey)) {
                if (UnityEngine.Random.value < 0.3f) {
                    // 한방 덤핑은 %로 깎으므로 그대로 둬도 됨 
                    float dumpPercent = UnityEngine.Random.Range(6.0f, 14.0f);
                    coin.CurrentPrice *= (1.0 - (dumpPercent / 100.0));
                    coin.OnPriceUpdate(coin.CurrentPrice);
                    Debug.Log($"<color=red>[세력 투하]</color> {coin.Symbol} {dumpPercent:F1}% 덤핑!");
                }
                processedKeys.Add(instantDumpKey);
            }
            // 틱당 -0.8(80%) -> -0.01 (1%)
            // 30분마다 1% 하락 -> 하루 48틱이면 약 40% 하락 (강력한 덤핑)
            coin.lockupAndHalvingBias = -0.01f;
            coin.lockupAndHalvingBiasEndTime = now.AddDays(1);
        }
    }

    // B: 반전 (하락하다 급등)
    private void ScenarioB(CoinData coin, int daysLeft, DateTime now) {
        // 페이즈로 제어할 때는 강제 수치(Bias)를 꺼야 자연스러운 캔들이 나옵니다.
        coin.lockupAndHalvingBias = 0f;

        // 행님 오더대로 마켓 페이즈 자체를 변경하여 자연스러운 무빙 유도
        if (daysLeft == 3) {
            // D-3: 살짝 올려서 꼬시기 (MildBull)
            coin.CurrentPhaseOverride = MarketPhase.SuperBull;
            coin.PhaseOverrideEndTime = now.AddDays(1);
            Debug.Log($"<color=cyan>[시나리오B]</color> {coin.Symbol} D-3: BigBull (꼬시기)");
        } else if (daysLeft == 2) {
            // D-2: 분위기 안 좋게 조성 (MildBear)
            coin.CurrentPhaseOverride = MarketPhase.MildBear;
            coin.PhaseOverrideEndTime = now.AddDays(1);
        } else if (daysLeft == 1) {
            // D-1: 그냥 떨구기 (BigBear) -> 투매 유도
            coin.CurrentPhaseOverride = MarketPhase.BigBear;
            coin.PhaseOverrideEndTime = now.AddDays(1);
        }

          // D-Day: 대반격 (Reversal)
          else if (daysLeft == 0) {
            // 당일은 확실한 떡상을 위해 MegaBull 페이즈 + 약간의 부스터(Bias) 조합
            coin.CurrentPhaseOverride = MarketPhase.MegaBull;

            // 시간 감쇠 로직 (장 초반 급등 후 서서히 안정화)
            float timeDecay = (24 - now.Hour) / 24f;
            coin.lockupAndHalvingBias = 0.005f * timeDecay; // MegaBull(0.35%) + Bias(0.5%) = 강력 매수

            coin.PhaseOverrideEndTime = now.AddHours(1);
            coin.lockupAndHalvingBiasEndTime = now.AddHours(1);
        }
    }

    // C: 매집 (보합)
    private void ScenarioC(CoinData coin, int daysLeft, DateTime now) {
        if (daysLeft == 0) {
            coin.lockupAndHalvingBias = 0f;
            coin.lockupAndHalvingBiasEndTime = now.AddDays(2);
        }
    }

    // D: 설거지 (천천히 흐름)
    // D: 설거지 (급등 후 급락)
    private void ScenarioD(CoinData coin, int daysLeft, DateTime now, int seed, int volLevel) {
        // 페이즈로 제어하므로 강제 수치(Bias)는 0으로 초기화
        coin.lockupAndHalvingBias = 0f;

        System.Random r = new System.Random(seed);
        // 덤핑 시작일 랜덤 설정 (-1: 해제 다음날, 0: 당일, 1: 하루 전)
        // 즉, 락업 해제일 전후로 무작위 타이밍에 던집니다.
        int dumpStartDay = r.Next(-1, 2);

        // 1. 상승 유도 (설거지 전 개미 꼬시기)
        // 4일 전부터 덤핑 전날까지
        if (daysLeft <= 4 && daysLeft > dumpStartDay) {
            //  수치 대신 페이즈 호출
            // 잡코인은 SuperBull(광기), 메이저는 BigBull(강세)로 차별화
            coin.CurrentPhaseOverride = (volLevel >= 5) ? MarketPhase.BigBull : MarketPhase.Bull;
            coin.PhaseOverrideEndTime = now.AddDays(1);

         
            // Debug.Log($"<color=#FF69B4>[설거지(D)]</color> {coin.Symbol}: 상승 유도 중 ({coin.CurrentPhaseOverride})");
        }
        // 2. 설거지 시작 (덤핑)
        // 약속된 날짜가 되면 가차 없이 MegaBear 호출
        else if (daysLeft <= dumpStartDay && daysLeft > dumpStartDay - 3) {
            coin.CurrentPhaseOverride = MarketPhase.MegaBear;
            coin.PhaseOverrideEndTime = now.AddDays(1);

            Debug.Log($"<color=red>[설거지(D)]</color> {coin.Symbol}: 덤핑 시작! (MegaBear 호출)");
        }
    }

    private void ScenarioE(CoinData coin, int daysLeft) {
        coin.lockupAndHalvingBias = 0;
    }
    #endregion

    #region [반감기 로직]
    private void HandleHalvingScenario(CoinData coin, CoinMetaData meta, DateTime now) {
        // 지나간 최근 반감기 날짜를 찾음
        DateTime lastEventDate = GetMostRecentEventDate(meta.HalvingCycleMonths, coin.Symbol, now);
        // 반감기 당일부터 14일 후까지 (총 15일)
        int daysSinceHalving = (now.Date - lastEventDate.Date).Days;

        if (daysSinceHalving >= 0 && daysSinceHalving < 15) {

            // 1. 유통량 정산 (반감기 당일 자정에 딱 한 번)
            if (daysSinceHalving == 0 && now.Hour == 0 && now.Minute == 0) {
                string halvingKey = $"halving_applied_{coin.Symbol}_{lastEventDate:yyyyMMdd}";
                if (!processedKeys.Contains(halvingKey)) {
                    meta.DailyMintAmount /= 2;
                    string msg = $"{coin.Name}({coin.Symbol}) 반감기 시즌으로, 채굴 보상이 절반으로 줄어듭니다.";
                    SendNews($"news_halving_{coin.Symbol}_{now:yyyyMMdd}", "[공고]", coin.Symbol, msg);
                    processedKeys.Add(halvingKey);
                    Debug.Log($"<color=#00BFFF>[반감기 시스템]</color> {coin.Symbol}: 채굴량 반토막");
                }
            }

           
            bool isPumpingTime = false;
            int duration = 7; 

            // 당일 날짜 기반 시드로 랜덤 시작 시간 결정
            int todaySeed = coin.Symbol.GetHashCode() + lastEventDate.DayOfYear + lastEventDate.Year + daysSinceHalving + sessionSeedOffset;
            int todayStart = new System.Random(todaySeed).Next(0, 24);

            if (now.Hour >= todayStart && now.Hour < todayStart + duration) isPumpingTime = true;

            // 날짜 겹침 처리 (밤 늦게 시작해서 다음날 새벽까지 이어지는 경우)
            if (!isPumpingTime) {
                int yesterdaySeed = coin.Symbol.GetHashCode() + lastEventDate.DayOfYear + lastEventDate.Year + (daysSinceHalving - 1) + sessionSeedOffset;
                int yesterdayStart = new System.Random(yesterdaySeed).Next(0, 24);
                if (yesterdayStart + duration > 24) {
                    int overlap = (yesterdayStart + duration) - 24;
                    if (now.Hour < overlap) isPumpingTime = true;
                }
            }

            if (isPumpingTime) {
                coin.CurrentPhaseOverride = MarketPhase.Bull;
                coin.PhaseOverrideEndTime = now.AddMinutes(31);
                coin.lockupAndHalvingBias = 0.008f + (meta.VolatilityLevel * 0.001f);
                coin.lockupAndHalvingBiasEndTime = now.AddMinutes(31);
            } else {
                // 펌핑 시간 아님: 오버라이드 즉시 해제
                if (now < coin.PhaseOverrideEndTime) {
                    coin.PhaseOverrideEndTime = now;
                    coin.lockupAndHalvingBias = 0f;
                }
            }
        }
    }

    // 가장 최근에 발생했던(혹은 오늘 발생한) 이벤트 날짜 반환
    private DateTime GetMostRecentEventDate(int cycleMonths, string symbol, DateTime now) {
        int dateSeed = symbol.GetHashCode();
        int fixedDay = new System.Random(dateSeed).Next(1, 29);
        DateTime eventDate = new DateTime(2020, 1, fixedDay);

        // 현재 날짜를 넘기 전까지 계속 주기를 더함
        while (eventDate.AddMonths(cycleMonths) <= now.Date) {
            eventDate = eventDate.AddMonths(cycleMonths);
        }
        return eventDate;
    }
    #endregion

    private string GetScenarioName(int dice) {
        if (dice < 25) return "덤핑(A)"; if (dice < 45) return "반전(B)";
        if (dice < 65) return "매집(C)"; if (dice < 90) return "설거지(D)"; return "평온(E)";
    }

    private DateTime CalculateNextEventDate(int cycleMonths, string symbol, DateTime now) {
        int dateSeed = symbol.GetHashCode();
        int fixedDay = new System.Random(dateSeed).Next(1, 29);
        DateTime eventDate = new DateTime(2020, 1, fixedDay);
        while (eventDate.Date < now.Date) eventDate = eventDate.AddMonths(cycleMonths);
        return eventDate;
    }

    private void SendNews(string specificKey, string tag, string symbol, string msg) {
        if (EventUIManager.Instance != null) {
            UIEventData newsData = new UIEventData {
                key = specificKey,
                type = UIEventType.News,
                authorName = "불비트 뉴스",
                category = (NewsCategory)5,
                title = "",
                message = $"<b>{tag} {symbol}</b>\n\n{msg}",
                profileImage = "bullbit.png",
                contentImage = "",
                width = 550,
                height = 300
            };
            EventUIManager.Instance.Show(newsData);
        } else if (GlobalNotificationManager.Instance != null) {
            GlobalNotificationManager.Instance.ShowNotification("Xbird", tag, msg);
        }
    }
}