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
    public bool IsDefaultListed;


    public CoinMetaData(string name, string symbol, double price, long supply, CoinTheme theme, int volatility, string description = "", string CoinInfo = "", bool bullbitListed = true, bool fournanceListed = false, bool isDefaultListed = true) {
        Name = name;
        Symbol = symbol;
        InitialPrice = price;
        MaxSupply = supply;
        Theme = theme;
        VolatilityLevel = Mathf.Clamp(volatility, 1, 5);
        Description = description;
        BullbitListed = bullbitListed;
        FournanceListed = fournanceListed;
        IsDefaultListed = isDefaultListed;
    }
}

public enum CoinTheme {
    Layer1,
    Layer2,
    Meme,
    AI,
    RWA,
    ZK,
    DeFi,
    Stable,
}

// Example static loader (to be used inside CoinManager or a DataLoader script)
public static class CoinMetaDatabase {
    public static readonly CoinMetaData[] AllCoins = new CoinMetaData[] {
        
        // =========================================================
        // 1. [확정 상장] (Bullbit: O, Default: O) - 메이저 9종
        // =========================================================
        new("비트코인", "BTC", 111000000.0, 21000000, CoinTheme.Layer1, 1, "디지털 금 자산", "", fournanceListed: true, isDefaultListed: true),
        new("이더리움", "ETH", 4000000, 115000000, CoinTheme.Layer1, 2, "스마트 컨트랙트 표준", "", fournanceListed: true, isDefaultListed: true),
        new("리플", "XRP", 300, 115000000, CoinTheme.Layer1, 2, "국제 송금 프로젝트", "", isDefaultListed: true),
        new("도지코인", "DOGE", 100, 115000000, CoinTheme.Meme, 5, "원조 밈 코인", "", fournanceListed: true, isDefaultListed: true),
        new("라이트코인", "LTC", 5000.0, 115000000, CoinTheme.Layer1, 3, "비트코인의 가벼운 버전", "", isDefaultListed: true),
        new("트럼프", "TRUMP", 8000.0, 115000000, CoinTheme.Meme, 5, "정치 테마 코인", "", fournanceListed: true, isDefaultListed: true),
        new("유에스디티", "USDT", 1350.0, 1000000000, CoinTheme.Stable, 1, "달러 가치 고정", "", bullbitListed: true, isDefaultListed: true),
        new("유에스디씨", "USDC", 1350.0, 1000000000, CoinTheme.Stable, 1, "투명한 담보 스테이블", "", bullbitListed: true, isDefaultListed: true),


        // =========================================================
        // 2. [랜덤 상장] (Bullbit: O, Default: X) - 50% 확률 등장 8종
        // =========================================================
        new("솔라나", "SOL", 130000.0, 115000000, CoinTheme.Layer1, 3, "고성능 레이어1", "", isDefaultListed: false),
        new("트론", "TRX", 280, 1000000000, CoinTheme.Layer1, 2, "고효율 네트워크", "", bullbitListed: true, isDefaultListed: false),
        new("아비트럼", "ARB", 345, 1000000000, CoinTheme.Layer2, 4, "이더리움 레이어2", "", bullbitListed: true, isDefaultListed: false),
        new("에이브", "AAVE", 180000.0, 115000000, CoinTheme.DeFi, 3, "탈중앙 대출", "", isDefaultListed: false),
        new("톤", "TON", 2000.0, 115000000, CoinTheme.Layer1, 3, "텔레그램 생태계", "", isDefaultListed: false),
        new("사인", "SIGN", 110.0, 115000000, CoinTheme.ZK, 4, "소셜 플랫폼", "", isDefaultListed: false),
        new("애니메코인", "ANIME", 80.0, 115000000, CoinTheme.Meme, 4, "서브컬쳐 NFT", "", isDefaultListed: false),
        new("무브먼트", "MOVE", 578.5, 115000000, CoinTheme.Meme, 5, "M2E 프로젝트", "", isDefaultListed: false),
        new("페페", "PEPE", 0.18, 115000000, CoinTheme.Meme, 4, "개구리 밈 코인", "", isDefaultListed: false),


        // =========================================================
        // 3. [이벤트 전용] (Bullbit: X) - 초기 상장 절대 불가 3종
        // =========================================================
        new("앱토스", "APT", 1000, 115000000, CoinTheme.Layer1, 3, "차세대 메인넷", "", bullbitListed: false),
        new("수이", "SUI", 300, 115000000, CoinTheme.Layer1, 3, "무브 언어 체인", "", bullbitListed: false),
        new("펭구코인", "PENGU", 10.0, 115000000, CoinTheme.Meme, 4, "귀여운 펭구", "", bullbitListed: false),
        new("초코코인", "CHK", 300, 115000000, CoinTheme.Meme, 4, "인기있는 밈코인", "", bullbitListed: false)
    };
}