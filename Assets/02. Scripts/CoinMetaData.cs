using UnityEngine;


public enum CoinClass {
    Major,  // 비트, 이더, 리플 등 (Major, Alt1 주도)
    Alt,    // 일반 알트 (시장 흐름 따름)
    Stable, // 스테이블 (시장 영향 X)
    Trash   // 잡코인 (시장 무시, 독자 생존)
}

[System.Serializable]
public enum ProofType { PoW, PoS, Token }
public class CoinMetaData {
    public string Name;
    public string Symbol;
    public double InitialPrice;
   public long CirculatingSupply; 
    public long MaxSupply;
    public CoinTheme Theme;
    public ProofType Proof;        // [추가] 합의 알고리즘 및 증명 방식

    public CoinClass Class;

    public string ContractAddress; // [추가] 계약 주소 (Native 또는 0x...)
    public int VolatilityLevel; // 1 (안정) ~ 5 (매우 변동) ~ 10추가 매우매우 변동
    public string Description;
    public bool BullbitListed;
    public bool FournanceListed;
    public bool IsDefaultListed;

    public int HalvingCycleMonths;
    public int UnlockCycleMonths;
    public long DailyMintAmount;


    public CoinMetaData(string name, string symbol, double price, long circulatingSupply, long maxSupply,
            CoinTheme theme, ProofType proof, CoinClass coinClass, int volatility, int halvingCycle = 0, int unlockCycle = 0, long dailyMint = 0, string description = "",
            string ca = "Native", bool bullbitListed = true, bool fournanceListed = false, bool isDefaultListed = true) {
        Name = name;
        Symbol = symbol;
        InitialPrice = price;
        CirculatingSupply = circulatingSupply;
        MaxSupply = maxSupply;
        Theme = theme;
        Proof = proof;              // [할당]
        Class = coinClass;
        ContractAddress = ca;       // [할당]
        VolatilityLevel = Mathf.Clamp(volatility, 1, 8);
        HalvingCycleMonths = halvingCycle; // 
        UnlockCycleMonths = unlockCycle;   // 
        DailyMintAmount = dailyMint;
        Description = description;
        BullbitListed = bullbitListed;
        FournanceListed = fournanceListed;
        IsDefaultListed = isDefaultListed;
    }

    public string GetRiskGrade() {
        return VolatilityLevel switch {
            1 => "S+",
            2 => "S",
            3 => "A",
            4 => "B",
            5 => "C",
            6 => "D",
            7 => "E",
            8 => "F",
            _ => "F"
        };
    }

    // [추가] UI 텍스트 컬러 반환
    public Color GetGradeColor() {
        return VolatilityLevel switch {
            1 => new Color(0.2f, 0.8f, 1f), // 하늘색 (S+)
            2 => new Color(0.4f, 1f, 0.4f), // 연두색 (S)
            3 or 4 => Color.white,           // 흰색 (A, B)
            5 or 6 => Color.yellow,          // 노란색 (C, D)
            _ => new Color(1f, 0.3f, 0.3f)  // 빨간색 (E, F)
        };
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
    Governance
}

public static class CoinMetaDatabase {
    public static readonly CoinMetaData[] AllCoins = new CoinMetaData[] {
        
        // =========================================================
        // 1. [확정 상장] 메이저 8종 
        // =========================================================
        new("비트코인", "BTC", 50000000, 19600000, 21000000, CoinTheme.Layer1, ProofType.PoW, CoinClass.Major, 1,
            4, 0, 500, "중앙은행 없이 블록체인 기술로 운영되는 가장 안전한 디지털 금", "Native", fournanceListed: true, isDefaultListed: true),

        new("이더리움", "ETH", 1000000, 115000000, 115000000, CoinTheme.Layer1, ProofType.PoS, CoinClass.Alt, 2,
            0, 0, 0, "스마트 컨트랙트를 지원하는 프로그래밍 가능 블록체인 플랫폼", "Native", fournanceListed: true, isDefaultListed: true),

        new("리플", "XRP", 300, 7000000000, 10000000000, CoinTheme.Layer1, ProofType.PoS, CoinClass.Alt, 2,
            0, 0, 0, "국제 송금 프로젝트", "Native", isDefaultListed: true),

        new("도지코인", "DOGE", 100, 80000000000, 80000000000, CoinTheme.Meme, ProofType.PoW, CoinClass.Alt, 5,
            0, 0, 0, "원조 밈 코인", "Native", fournanceListed: true, isDefaultListed: true),

        new("라이트코인", "LTC", 5000.0, 74000000, 115000000, CoinTheme.Layer1, ProofType.PoW, CoinClass.Alt, 3,
            3, 0, 1000, "비트코인의 가벼운 버전", "Native", isDefaultListed: true),

        new("트럼프", "TRUMP", 8000.0, 46000000, 115000000, CoinTheme.Meme, ProofType.Token, CoinClass.Alt, 5,
            0, 0, 0, "정치 테마 코인", "0x576...4b1", fournanceListed: true, isDefaultListed: true),

        new("유에스디티", "USDT", 1350.0, 23412000000, 23412000000, CoinTheme.Stable, ProofType.Token, CoinClass.Stable, 1,
            0, 0, 0, "달러 가치 고정", "0xdAC...1ec", bullbitListed: true, isDefaultListed: true),

        new("유에스디씨", "USDC", 1350.0, 18642000000, 18642000000, CoinTheme.Stable, ProofType.Token, CoinClass.Stable, 1,
            0, 0, 0, "투명한 담보 스테이블", "0xA0b...eb6", bullbitListed: true, isDefaultListed: true),

        new("무브먼트", "MOVE", 578.5, 20000000, 115000000, CoinTheme.Meme, ProofType.Token,CoinClass.Alt,
            5, 0, 2, 0, "Move-to-Earn 개념의 운동 기반 보상 토큰", "0x771...bc2", bullbitListed: true, isDefaultListed: true),

        new("페페", "PEPE", 0.18, 115000000, 115000000, CoinTheme.Meme, ProofType.Token,CoinClass.Alt, 4, 0, 0, 0, "개구리 밈 코인", "0x698...92c",  bullbitListed: true, isDefaultListed: true),
        new("솔라나", "SOL", 130000.0, 85000000, 115000000, CoinTheme.Layer1, ProofType.PoS,CoinClass.Alt, 3, 0, 0, 500, "고성능 처리 속도를 자랑하는 차세대 레이어1 블록체인", "Native", bullbitListed: true, isDefaultListed: true),
        new("애니메코인", "ANIME", 80.0, 90000000, 115000000, CoinTheme.Meme, ProofType.Token,CoinClass.Alt, 4, 0, 0, 0, "서브컬쳐 NFT", "0x882...fa1", bullbitListed: true, isDefaultListed: true),
        new("아비트럼", "ARB", 345, 250000000, 1000000000, CoinTheme.Layer2, ProofType.Token,CoinClass.Alt, 4, 0, 2, 0, "이더리움의 확장성을 높이는 주요 레이어2 솔루션", "0x912...62b", bullbitListed: true, isDefaultListed: true),
        new("월드코인", "WLD", 500, 250000000, 1000000000, CoinTheme.AI, ProofType.Token,CoinClass.Alt, 4, 0, 2, 0, "글로벌 디지털 ID와 기본소득을 위한 AI 연동 프로젝트", "0x163...54q", bullbitListed: true, isDefaultListed: true),
        new("에이브", "AAVE", 180000.0, 115000000, 115000000, CoinTheme.DeFi, ProofType.PoS,CoinClass.Alt, 3, 0, 0, 0, "탈중앙화 대출 및 예금을 제공하는 DeFi 프로토콜", "Native", bullbitListed: true, isDefaultListed: true),

        // =========================================================
        // 2. [랜덤 상장] - isDefaultListed: false 추가
        // =========================================================
        new("트론", "TRX", 280, 870000000, 1000000000, CoinTheme.Layer1, ProofType.PoS,CoinClass.Alt, 2, 0, 0, 1000, "고효율 네트워크", "Native", bullbitListed: true, isDefaultListed: false),
        new("톤", "TON", 2000.0, 34000000, 115000000, CoinTheme.Layer1, ProofType.PoS,CoinClass.Alt, 3, 0, 0, 200, "텔레그램 생태계", "Native", isDefaultListed: false),
        new("사인", "SIGN", 110.0, 50000000, 115000000, CoinTheme.ZK, ProofType.Token,CoinClass.Alt, 4, 0, 3, 0, "소셜 플랫폼", "0x43a...221", isDefaultListed: false),



        // =========================================================
        // 3. [이벤트 전용] - bullbitListed: false, isDefaultListed: false 추가
        // =========================================================
        new("앱토스", "APT", 1000, 15000000, 115000000, CoinTheme.Layer1, ProofType.PoS,CoinClass.Alt, 3, 0, 1, 0, "메타의 Diem 프로젝트 출신 개발자가 만든 차세대 메인넷", "Native", bullbitListed: false, isDefaultListed: false),
        new("수이", "SUI", 300, 12000000, 115000000, CoinTheme.Layer1, ProofType.PoS,CoinClass.Alt, 3, 0, 1, 0, "Move 프로그래밍 언어를 사용한 고성능 레이어1 블록체인", "Native", bullbitListed: false, isDefaultListed: false),
        new("펭구코인", "PENGU", 10.0, 5000000, 115000000, CoinTheme.Meme, ProofType.Token,CoinClass.Alt, 4, 0, 2, 0, "귀여운 펭구", "6tzX...k5L", bullbitListed: false, isDefaultListed: false),
        new("초코코인", "CHK", 300, 20000000, 115000000, CoinTheme.Meme, ProofType.Token, CoinClass.Alt,4, 0, 3, 0, "인기있는 밈코인", "4vcX...zLq", bullbitListed: false, isDefaultListed: false),
        new("버츄얼 프로토콜", "VIRTUAL", 1200, 250000000, 1000000000, CoinTheme.AI, ProofType.Token, CoinClass.Alt, 3, 0, 2, 0, "다중 모달 AI 에이전트를 생성, 토큰화, 공동 소유 및 수익화할 수 있는 플랫폼", "0x52a1...3wE", bullbitListed: false, isDefaultListed: false),
        new("아이돌", "IDOL", 1000, 50000000, 115000000, CoinTheme.RWA, ProofType.Token,CoinClass.Alt, 5, 0, 2, 0, "누구나 음악을 만들고, 수익을 내게 만들어 주는 토큰", "0x742...1e3", bullbitListed: false, isDefaultListed: false),
        new("보라거위", "PGOOSE", 150, 500000000, 1000000000, CoinTheme.Meme, ProofType.Token,CoinClass.Alt, 5, 0, 2, 0, "커뮤니티 문화로 성장한 보라색 거위 밈 코인", "4vzX...bqm", bullbitListed: false, isDefaultListed: false),
        new("달", "MOON", 1500, 5206000000, 5206000000, CoinTheme.Governance, ProofType.Token,CoinClass.Alt, 5, 0, 2, 0, "스테이블 코인과 연동되어 있는 거버넌스 코인", "Native", bullbitListed: false, isDefaultListed: false),
    };
}