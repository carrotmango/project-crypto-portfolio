using System;

[Serializable]
public class ArticleData {
    public string provider;      // 발행사 (예: Bullbit Financial)
    public string title;         // 뉴스 제목
    public string summary;       // 뉴스 본문/요약
    public string newsImage;     // 뉴스용 별도 이미지 (없으면 contentImage 사용)
    public string[] targetSymbols;
    public string expertOpinion;
}


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
    public bool postYn = true;

    public ArticleData article;
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
