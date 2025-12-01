using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using static ChartRenderer;

public static class SaveManager
{
    private static string path = Application.persistentDataPath + "/save.json";

    public static void Save()
    {
        GameData data = new GameData();

        // -----------------------------
        // PlayerManager 저장
        // -----------------------------
        data.playerName = PlayerManager.Instance.playerName;
        data.birthday = PlayerManager.Instance.birthday;
        data.characterIndex = PlayerManager.Instance.characterIndex;

        data.bullbitCash = PlayerManager.Instance.bullbitCash;
        data.satoshiBankCash = PlayerManager.Instance.satoshiBankCash;
        data.mangoCasinoCash = PlayerManager.Instance.mangoCasinoCash;

        data.holdings.Clear();
        foreach (var kv in PlayerManager.Instance.holdings)
            data.holdings.Add(new CoinEntry { symbol = kv.Key, value = kv.Value });

        data.totalBuyAmount.Clear();
        foreach (var kv in PlayerManager.Instance.totalBuyAmount)
            data.totalBuyAmount.Add(new CoinEntry { symbol = kv.Key, value = kv.Value });

        data.totalBuyQuantity.Clear();
        foreach (var kv in PlayerManager.Instance.totalBuyQuantity)
            data.totalBuyQuantity.Add(new CoinEntry { symbol = kv.Key, value = kv.Value });

        // -----------------------------
        // CoinManager 저장
        // -----------------------------
        data.survivalDays = CoinManager.Instance.survivalDays;
        data.savedDateTime = CoinManager.Instance.CurrentDateTime.ToString("o");
        data.tickCount = CoinManager.Instance.TickCount;


        // 전체 MarketPhase 저장
        data.savedMarketPhase = CoinManager.Instance.CurrentMarket.ToString();


        data.savedCoins.Clear();

        foreach (var coin in CoinManager.Instance.coins)
        {
            SavedCoin sc = new SavedCoin();

            sc.symbol = coin.Symbol;
            sc.currentPrice = coin.CurrentPrice;
            sc.initialPrice = coin.InitialPrice;

            var meta = Array.Find(CoinMetaDatabase.AllCoins, m => m.Symbol == coin.Symbol);
            sc.isListed = meta != null && meta.BullbitListed;

            // 가격 히스토리 마지막 1개만 저장
            if (coin.PriceHistory != null && coin.PriceHistory.Count > 0) {
                sc.priceHistory = new List<double> { coin.PriceHistory[^1] };
            } else {
                sc.priceHistory = new List<double>();
            }

            // 캔들 마지막 1개만 저장
            if (coin.CandleHistory != null && coin.CandleHistory.Count > 0) {
                sc.candles = new List<CandleData> { coin.CandleHistory[^1] };
            } else {
                sc.candles = new List<CandleData>();
            }


            sc.phaseOverride = coin.CurrentPhaseOverride;
            sc.phaseOverrideEndTicks = coin.PhaseOverrideEndTime.Ticks;

            data.savedCoins.Add(sc);
        }


        // -----------------------------
        // XFeed 저장 (이벤트 + UI 전체)
        // -----------------------------
        data.xFeedPosts.Clear();

        var feed = XFeedSpawner.Instance;
        if (feed != null)
        {
            foreach (var go in feed.spawnedPosts)
            {
                var loader = go.GetComponent<XPostLoader>();
                if (loader == null) continue;

                var original = loader.originalData;
                if (original == null) continue;

                SavedXPost saved = new SavedXPost();

                // ---- UI 표시 정보 ----
                saved.key = loader.key;
                saved.name = loader.authorName;
                saved.content = loader.content;
                saved.profileImage = loader.profileImgName;
                saved.contentImage = loader.contentImgName;
                saved.date = loader.date;
                saved.time = loader.time;
                saved.width = loader.width;
                saved.height = loader.height;

                // ---- 이벤트 기본 ----
                saved.authorId = original.authorId;
                saved.postYn = original.postYn;
                saved.eventMustRequired = original.eventMustRequired;
                saved.eventType = original.eventType;

                // ---- 이벤트 시간 ----
                saved.startDate = original.startDate;
                saved.endDate = original.endDate;

                // ---- 전체 시장 영향 ----
                saved.overrideMarketPhase = original.overrideMarketPhase;
                saved.marketPhaseToSet = original.marketPhaseToSet;
                saved.marketPhaseAfter = original.marketPhaseAfter;
                saved.durationHours = original.durationHours;
                saved.affectAllCoins = original.affectAllCoins;

                // ---- 타겟 그룹 저장 ----
                saved.targetGroups = original.targetGroups;

                data.xFeedPosts.Add(saved);
            }
        }

        // =====================
        // 구독 정보 저장
        // =====================
        var xn = XNotificationManager.Instance;

        data.isSubscribed = xn.isSubscribed;
        data.isCancelRequested = xn.isCancelRequested;
        data.nextBillingDate = xn.nextBillingDate.ToString("o"); // Round-trip format


        // -----------------------------
        // Active Event 저장
        // -----------------------------
        data.activeEffects.Clear();

        var orch = EventOrchestrator.Instance;
        if (orch != null)
        {
            foreach (var eff in orch.activeEffects)
            {
                var xdata = eff.data;
                var start = eff.startTime;

                data.activeEffects.Add(new SavedActiveEffect
                {
                    authorId = xdata.authorId,
                    eventKey = xdata.key,
                    startTime = start.ToString("o")
                });

                Debug.Log("[Save] ActiveEffect 저장: " + xdata.authorId + " / " + xdata.key + " 시작=" + start);
            }
        }

        // -----------------------------
        // JSON 저장
        // -----------------------------
        string json = JsonUtility.ToJson(data, true);

#if UNITY_WEBGL && !UNITY_EDITOR
    // 웹 환경이면 React로 저장 JSON 전송
    if (WebMessageSender.Instance != null)
    {
        WebMessageSender.Instance.SendSaveJsonToWeb(json);
    }
    else
    {
        Debug.LogError("WebMessageSender 인스턴스를 찾을 수 없습니다.");
    }
#else
        // 로컬 파일 저장
        File.WriteAllText(path, json);
        Debug.Log("저장 완료: " + path);
#endif
    }


    public static GameData Load()
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("저장 파일 없음");
            return null;
        }

        string json = File.ReadAllText(path);
        GameData data = JsonUtility.FromJson<GameData>(json);

        Debug.Log("불러오기 완료: " + json);

        return data;
    }


    public static void DeleteSave()
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}