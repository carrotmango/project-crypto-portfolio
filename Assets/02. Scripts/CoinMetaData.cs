using UnityEngine;

[System.Serializable]
public class CoinMetaData {
    public string Name;
    public string Symbol;
    public double InitialPrice;
    public long MaxSupply;
    public CoinTheme Theme;
    public int VolatilityLevel; // 1 (안정) ~ 5 (매우 변동)
    public string Description;
    public bool BullbitListed;
    public bool FournanceListed;


    public CoinMetaData(string name, string symbol, double price, long supply, CoinTheme theme, int volatility, string description = "", string CoinInfo = "", bool bullbitListed = true, bool fournanceListed = false) {
        Name = name;
        Symbol = symbol;
        InitialPrice = price;
        MaxSupply = supply;
        Theme = theme;
        VolatilityLevel = Mathf.Clamp(volatility, 1, 5);
        Description = description;
        BullbitListed = bullbitListed;
        FournanceListed = fournanceListed;
    }
}

public enum CoinTheme {
    Layer1,
    Layer2,
    Meme,
    GameFi,
    AI,
    NFT,
    RealWorldAsset,
    Politics,
    Community,
    Experimental,
    DeFi

}

// Example static loader (to be used inside CoinManager or a DataLoader script)
public static class CoinMetaDatabase {
    public static readonly CoinMetaData[] AllCoins = new CoinMetaData[] {
        new("비트코인", "BTC", 111000000.0, 21000000, CoinTheme.Layer1, 1, "대표적인 Layer 1 블록체인. 디지털 금 자산", "", fournanceListed: true),
        new("이더리움", "ETH", 4000000, 115000000, CoinTheme.Layer1, 2, "스마트 컨트랙트의 표준 플랫폼", "", fournanceListed: true),
        new("라이트코인", "LTC", 5000.0, 115000000, CoinTheme.Layer1, 3, "비트코인의 라이트한 버전", ""),
        new("도지코인", "DOGE", 100, 115000000, CoinTheme.Meme, 5, "시바견 밈에서 시작된 암호화폐", "", fournanceListed: true),
        new("리플", "XRP", 300, 115000000, CoinTheme.Layer1, 2, "국제 송금 시장을 위한 플랫폼", ""),
        new("페페", "PEPE", 0.18, 115000000, CoinTheme.Meme, 4, "개구리 페페 커뮤니티의 밈코인", ""),
        new("사인", "SIGN", 110.0, 115000000, CoinTheme.Experimental, 4, "분산형 메시징 중심의 소셜 플랫폼", ""),
        new("솔라나", "SOL", 130000.0, 115000000, CoinTheme.Layer1, 3, "고성능 Layer1, 빠른 처리속도 자랑", ""),
        new("트럼프", "TRUMP", 8000.0, 115000000, CoinTheme.Politics, 5, "정치적 이슈에 따라 변동", "",fournanceListed: true),
        new("에이브", "AAVE", 180000.0, 115000000, CoinTheme.DeFi, 3, "탈중앙 금융 대출 플랫폼", ""),
        new("톤", "TON", 2000.0, 115000000, CoinTheme.Community, 3, "텔레그램 기반 소셜 생태계", ""),
        new("애니메코인", "ANIME", 80.0, 115000000, CoinTheme.NFT, 4, "애니메이션 및 서브컬쳐 NFT 유틸리티", ""),
        new("무브먼트", "MOVE", 578.5, 115000000, CoinTheme.GameFi, 5, "실생활 활동 연계 + 게임 보상", ""),
        new("앱토스", "APT", 1000, 115000000, CoinTheme.Layer1, 3, "차세대 고성능 Layer1 블록체인", "", bullbitListed:false),
        new("수이", "SUI", 300, 115000000, CoinTheme.Layer1, 3, "분산형 애플리케이션(dApp)과 디지털 자산을 지원하도록 설계된 차세대 레이어1 블록체인", "", bullbitListed:false),
        new("펭구코인", "PENGU", 10.0, 115000000, CoinTheme.Meme, 4, "귀여운 펭구 밈코인", "", bullbitListed:false),
        new("유에스디티", "USDT", 1350.0, 1000000000, CoinTheme.RealWorldAsset, 1, "달러 가치에 고정된 스테이블 코인", "", bullbitListed: true),
        new("유에스디씨", "USDC", 1350.0, 1000000000, CoinTheme.RealWorldAsset, 1, "투명한 담보 기반의 스테이블 코인", "", bullbitListed: true),
        new("아비트럼", "ARB", 345, 1000000000, CoinTheme.Layer2, 4, "아비트럼(Arbitrum)은 이더리움의 느린 처리 속도와 높은 수수료 문제를 해결하기 위한 레이어 2 솔루션", "", bullbitListed: true),
        new("트론", "TRX", 280, 1000000000, CoinTheme.Layer1, 2, "고속,저비용을 추구하는 암호화폐", "", bullbitListed: true)

    };
}