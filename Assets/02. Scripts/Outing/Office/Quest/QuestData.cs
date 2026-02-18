using UnityEngine;

// 1. 퀘스트 수행 조건 (어떤 행동을 해야 하는가?)
public enum QuestType {
    BuyCount,         // 매수 횟수
    SellCount,        // 매도 횟수
    TotalTradeAmount  // 총 거래 금액
}

// 2. 퀘스트 분류 (어떤 종류의 퀘스트인가?)
public enum QuestCategory {
    Main,       // 메인 스토리 (정치적 사건 관련)
    Daily,      // 일일 퀘스트 (단순 매매 등)
    Achievement // 업적 (누적 거래량 등)
}

// 3. [추가됨] 보상 종류 (무엇을 줄 것인가?)
public enum RewardType {
    Cash,      // 현금 (KRW, USDT 등)
    Crypto,    // 코인 (BTC, ETH 등)
    Item       // 게임 아이템 (필요 시)
}

// 4. [추가됨] 보상 상세 정보 (구체적으로 무엇을 몇 개?)
[System.Serializable]
public class QuestReward {
    public RewardType type;   // 현금인지 코인인지
    public string targetID;   // 대상 ID ("Bullbit", "BTC", "DOGE")
    public double amount;     // 수량 (10000원, 0.5개)
}

// 5. 실시간 진행 데이터 (저장될 데이터)
[System.Serializable]
public class QuestProgress {
    public string questID;
    public double currentCount;
    public bool isClaimed;
    public bool IsCompleted(double target) => currentCount >= target;
}