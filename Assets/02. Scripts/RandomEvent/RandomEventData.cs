using System;
using System.Collections.Generic;

[System.Serializable]
public class RandomEventData {
    public string id;
    public string description;

    public int satoshiBankChange;
    public int bullbitChange;

    public CoinChange[] coins;

    public bool isFixedDateEvent;   // 날짜 고정 이벤트 여부
    public string eventDate;        // "2016-01-03" 같은 형식
    public bool allowUnlistedAirdrop; // 미상장 에어드랍 여부
}

[Serializable]
public class CoinChange {
    public string symbol;
    public double amount;
}

[Serializable]
public class RandomEventDataList {
    public RandomEventData[] events; 
}