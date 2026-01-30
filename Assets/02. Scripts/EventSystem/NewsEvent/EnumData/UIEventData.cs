using System;

[Serializable]
public class UIEventData {
    public string key;
    public UIEventType type;
    public NewsCategory category;

    public string authorName;
    public string title;
    public string message;


    public string profileImage;
    public string contentImage;
    public float height;
    public float width;
}

public enum UIEventType {
    News = 0,
    RumorSimple = 1,
    RumorDM = 2
}

public enum NewsCategory {
    All = 0,        // 디폴트: 모든 뉴스 통합 피드
    Community = 1,  // 커뮤니티 (유저 노출 최우선 탭)
    Economy = 2,    // 경제
    Tech = 3,       // 기술
    Exchange = 4,   // 거래소
    OnChain = 5     // 온체인
}
