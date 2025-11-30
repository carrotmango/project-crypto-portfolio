using System.Collections.Generic;
using System;

[Serializable]
public class CoinEntry {
    public string symbol;
    public double value;
}
public class SavedActiveEffect {
    public string authorId;
    public string eventKey;
    public string startTime;
}


[Serializable]
public class GameData {
    // PlayerManager
    public string playerName;
    public string birthday;
    public int characterIndex;
    public double bullbitCash;
    public double satoshiBankCash;
    public double mangoCasinoCash;

    public List<CoinEntry> holdings = new List<CoinEntry>();
    public List<CoinEntry> totalBuyAmount = new List<CoinEntry>();
    public List<CoinEntry> totalBuyQuantity = new List<CoinEntry>();

    // CoinManager
    public string savedDateTime;
    public int survivalDays;
    public int tickCount;

    // 코인 전체
    public List<SavedCoin> savedCoins = new();

    // XPost
    public List<SavedXPost> xFeedPosts = new();

    // DurationTime
    public List<SavedActiveEffect> activeEffects = new List<SavedActiveEffect>();

    // MarketPhase
    public string savedMarketPhase;

    // X subcirbes
    public bool isSubscribed;
    public bool isCancelRequested;
    public string nextBillingDate;

}
