using System;
using System.Collections.Generic;

[System.Serializable]
public class RandomEventData {
    public string id;
    public string description;
    public string title;

    public int satoshiBankChange;
    public int bullbitChange;

    public CoinChange[] coins;

    public bool isFixedDateEvent;   // 날짜 고정 이벤트 여부 >> 추후 관리 고정 시스템을 다른곳에서 관리할 수도
    public string eventDate;        // "2016-01-03" 같은 형식 >> 추후 관리 고정 시스템을 다른곳에서 관리할 수도
    public bool allowUnlistedAirdrop; // 미상장 에어드랍 여부
    public bool once;
    public bool worldEffect; // 일단 세계선에서 영향받는 것 여부 >> 추후 관리
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