using UnityEngine;
using System;
using System.Collections.Generic;

public class CoinEventScenarioManager : MonoBehaviour {
    public static CoinEventScenarioManager Instance { get; private set; }

    private int sessionSeedOffset;
    // [추가] 하루에 유통량/뉴스가 여러 번 중복 처리되는 것을 막기 위한 도장
    private HashSet<string> processedKeys = new HashSet<string>();

    void Awake() {
        Instance = this;
        sessionSeedOffset = UnityEngine.Random.Range(0, 999999);
        Debug.Log($"<color=#00FFFF>[시스템]</color> CoinEventScenarioManager 가동 (SessionSeed: {sessionSeedOffset})");
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

    #region [락업 해제: 5종 시나리오 및 고정 뉴스]
    private void HandleUnlockScenario(CoinData coin, CoinMetaData meta, DateTime now) {
        DateTime nextUnlockDate = CalculateNextEventDate(meta.UnlockCycleMonths, coin.Symbol, now);
        int daysLeft = (nextUnlockDate.Date - now.Date).Days;

        int scenarioSeed = coin.Symbol.GetHashCode() + nextUnlockDate.DayOfYear + nextUnlockDate.Year + sessionSeedOffset;
        UnityEngine.Random.InitState(scenarioSeed);
        int scenarioDice = UnityEngine.Random.Range(0, 100);
        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

        if (daysLeft == 14) {
            Debug.Log($"<color=#00FF00>[락업 시나리오 확정]</color> {coin.Symbol} -> <color=#FFD700><b>{GetScenarioName(scenarioDice)}</b></color> (발생일: {nextUnlockDate:MM-dd})");
        }

        // =========================================================
        // [당일 로직] 실제 유통량 증가 및 고정 뉴스 발포
        // =========================================================
        if (daysLeft == 0) {
            string todayStr = now.ToString("yyyyMMdd");
            string supplyKey = $"lockup_applied_{coin.Symbol}_{todayStr}";

            Debug.Log($"<color=#FF4500>[D-Day]</color> {coin.Symbol} 락업 해제 당일! 현재 적용 시나리오: <b>{GetScenarioName(scenarioDice)}</b>");

            // 1. 하루 1회만 유통량 증가 및 뉴스 송고
            if (!processedKeys.Contains(supplyKey)) {
                long maxSupply = coin.MaxSupply;
                long currentSupply = coin.CirculatingSupply;
                long remainingSupply = maxSupply - currentSupply;

                if (remainingSupply > 0) {
                    // 수량 계산 (UI와 동기화)
                    int amountSeed = coin.Symbol.GetHashCode() + now.DayOfYear + now.Year;
                    UnityEngine.Random.InitState(amountSeed);
                    float unlockRatio = (remainingSupply / (float)maxSupply <= 0.20f)
                                        ? (remainingSupply / (float)maxSupply)
                                        : UnityEngine.Random.Range(0.05f, 0.15f);
                    UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

                    long unlockAmount = (long)(maxSupply * unlockRatio);

                    // 본체 데이터 갱신
                    coin.CirculatingSupply += unlockAmount;
                    meta.CirculatingSupply += unlockAmount;
                    if (coin.CirculatingSupply > maxSupply) coin.CirculatingSupply = maxSupply;

                    // [뉴스 내용 고정] 무미건조한 팩트 전달
                    string msg = $"{coin.Name}({coin.Symbol})의 유통 물량 {unlockAmount:N0}개가 정식 해제되었습니다.";
                    SendNews($"news_unlock_{coin.Symbol}_{todayStr}", "[공시]", coin.Symbol, msg);

                    processedKeys.Add(supplyKey);
                    Debug.Log($"<color=#FFA500>[유통량 해제]</color> {coin.Symbol} {unlockAmount:N0}개 추가됨.");
                }
            }
        }

        // 가격 변동 시나리오 (Bias 주입은 매 틱 실행)
        if (daysLeft <= 14 && daysLeft >= -3) {
            if (scenarioDice < 25) ScenarioA(coin, daysLeft, now);
            else if (scenarioDice < 45) ScenarioB(coin, daysLeft, now);
            else if (scenarioDice < 65) ScenarioC(coin, daysLeft, now);
            else if (scenarioDice < 90) ScenarioD(coin, daysLeft, now, scenarioSeed, meta.VolatilityLevel);
            else ScenarioE(coin, daysLeft);
        }
    }

    private void ScenarioA(CoinData coin, int daysLeft, DateTime now) {
        if (daysLeft == 0) {
            // 1. [확률 체크] 덤핑 당일, 30% 확률로 '세력의 투하(한방 덤핑)' 발생
            // 확률은 행님이 원하시는 대로 수정 가능합니다.
            string instantDumpKey = $"instant_dump_{coin.Symbol}_{now:yyyyMMdd}";

            if (!processedKeys.Contains(instantDumpKey)) {
                // 30% 확률로 한방 덤핑 실행
                if (UnityEngine.Random.value < 0.3f) {
                    float dumpPercent = UnityEngine.Random.Range(6.0f, 14.0f);

                    // 가격 즉시 반영
                    coin.CurrentPrice *= (1.0 - (dumpPercent / 100.0));
                    coin.OnPriceUpdate(coin.CurrentPrice);

                    Debug.Log($"<color=red>[세력 투하]</color> {coin.Symbol} 락업 해제 직후 {dumpPercent:F1}% 기습 덤핑 발생!");
                } else {
                    Debug.Log($"<color=yellow>[일반 하락]</color> {coin.Symbol} 한방 덤핑 없이 서서히 하락 압력만 적용됩니다.");
                }

                // 하루 한 번만 판단하도록 도장 꾹
                processedKeys.Add(instantDumpKey);
            }

            // 2. [기존 로직 유지] 한방 덤핑 여부와 상관없이 당일 내내 -0.8f의 하락 압력을 줍니다.
            coin.lockupAndHalvingBias = -0.8f;
            coin.lockupAndHalvingBiasEndTime = now.AddDays(1);
        }
    }

    private void ScenarioB(CoinData coin, int daysLeft, DateTime now) {
        if (daysLeft <= 7 && daysLeft > 0) {
            coin.lockupAndHalvingBias = -0.1f;
            coin.lockupAndHalvingBiasEndTime = now.AddDays(1);
        } else if (daysLeft == 0) {
            coin.lockupAndHalvingBias = +0.8f;
            coin.lockupAndHalvingBiasEndTime = now.AddDays(1);
        }
    }

    private void ScenarioC(CoinData coin, int daysLeft, DateTime now) {
        if (daysLeft == 0) {
            coin.lockupAndHalvingBias = 0f;
            coin.lockupAndHalvingBiasEndTime = now.AddDays(2);
        }
    }

    private void ScenarioD(CoinData coin, int daysLeft, DateTime now, int seed, int volLevel) {
        System.Random r = new System.Random(seed);
        int dumpStartDay = r.Next(-1, 2);
        if (daysLeft <= 8 && daysLeft > dumpStartDay) {
            coin.lockupAndHalvingBias = (volLevel >= 5) ? 0.2f : 0.15f;
            coin.lockupAndHalvingBiasEndTime = now.AddDays(1);
        } else if (daysLeft <= dumpStartDay && daysLeft > dumpStartDay - 3) {
            coin.lockupAndHalvingBias = -0.8f;
            coin.lockupAndHalvingBiasEndTime = now.AddDays(1);
        }
    }

    private void ScenarioE(CoinData coin, int daysLeft) {
        coin.lockupAndHalvingBias = 0;
    }
    #endregion

    #region [반감기 로직: 채굴량 감소 뉴스 고정]
    private void HandleHalvingScenario(CoinData coin, CoinMetaData meta, DateTime now) {
        DateTime nextHalvingDate = CalculateNextEventDate(meta.HalvingCycleMonths, coin.Symbol, now);
        int daysLeft = (nextHalvingDate.Date - now.Date).Days;

        if (daysLeft == 0) {
            string halvingKey = $"halving_applied_{coin.Symbol}_{now:yyyyMMdd}";
            if (!processedKeys.Contains(halvingKey)) {
                // 채굴량 감소
                meta.DailyMintAmount /= 2;

                // 가격 버프 주입
                float boost = (9 - meta.VolatilityLevel) * 0.01f;
                coin.lockupAndHalvingBias = 0.02f + boost;
                coin.lockupAndHalvingBiasEndTime = now.AddDays(10);

                // [뉴스 내용 고정] 팩트 전달
                string msg = $"{coin.Name}({coin.Symbol}) 반감기 적용으로 일일 채굴량이 {meta.DailyMintAmount:N0}으로 감소하였습니다.";
                SendNews($"news_halving_{coin.Symbol}_{now:yyyyMMdd}", "[공고]", coin.Symbol, msg);

                processedKeys.Add(halvingKey);
                Debug.Log($"<color=#00BFFF>[반감기 발생]</color> {coin.Symbol} 보상 절반 감소 및 10일간 가격 보너스 적용");
            }
        }
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
                category = (NewsCategory)5, // 온체인
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