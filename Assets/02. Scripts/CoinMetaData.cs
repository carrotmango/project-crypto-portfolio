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
    private string nameKey;
    private string descKey;

    public string Name => LocalizationManager.GetText(nameKey);
    public string Description => LocalizationManager.GetText(descKey);
    public string Symbol;
    public double InitialPrice;
   public long CirculatingSupply; 
    public long MaxSupply;
    public CoinTheme Theme;
    public ProofType Proof;        // [추가] 합의 알고리즘 및 증명 방식

    public CoinClass Class;

    public string ContractAddress; // [추가] 계약 주소 (Native 또는 0x...)
    public int VolatilityLevel; // 1 (안정) ~ 5 (매우 변동) ~ 10추가 매우매우 변동
    public bool BullbitListed;
    public bool FournanceListed;
    public bool IsDefaultListed;

    public int HalvingCycleMonths;
    public int UnlockCycleMonths;
    public long DailyMintAmount;


    public CoinMetaData(string name, string symbol, double price, long circulatingSupply, long maxSupply,
                CoinTheme theme, ProofType proof, CoinClass coinClass, int volatility, int halvingCycle = 0, int unlockCycle = 0, long dailyMint = 0, string description = "",
                string ca = "Native", bool bullbitListed = true, bool fournanceListed = false, bool isDefaultListed = true) {

        nameKey = $"COIN_NAME_{symbol}";
        descKey = $"COIN_DESC_{symbol}";

        Symbol = symbol;
        InitialPrice = price;
        CirculatingSupply = circulatingSupply;
        MaxSupply = maxSupply;
        Theme = theme;
        Proof = proof;
        Class = coinClass;
        ContractAddress = ca;
        VolatilityLevel = Mathf.Clamp(volatility, 1, 8);
        HalvingCycleMonths = halvingCycle;
        UnlockCycleMonths = unlockCycle;
        DailyMintAmount = dailyMint;
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
            4, 0, 500, "중앙은행의 통제 없이 블록체인 네트워크로 운영되는 최초의 암호화폐이자, '디지털 금'으로 불리는 가장 안전한 자산입니다", "Native", fournanceListed: true, isDefaultListed: true),

        new("이더리움", "ETH", 1000000, 115000000, 115000000, CoinTheme.Layer1, ProofType.PoS, CoinClass.Alt, 2,
            0, 0, 0, "스마트 컨트랙트를 최초로 도입하여 다양한 디앱(DApp)과 토큰 생태계를 구축할 수 있게 만든 세계 최대의 프로그래밍 가능 블록체인 플랫폼입니다.", "Native", fournanceListed: true, isDefaultListed: true),

        new("쓰로스", "XRS", 300, 7000000000, 10000000000, CoinTheme.Layer1, ProofType.PoS, CoinClass.Alt, 2,
            0, 0, 0, "기존 은행 시스템을 대체하기 위해 설계된 초고속 국제 송금 네트워크 코인으로, 기관 투자자들의 큰 관심을 받고 있습니다.", "Native", isDefaultListed: true),

        new("도지코인", "DOGE", 100, 80000000000, 80000000000, CoinTheme.Meme, ProofType.PoW, CoinClass.Alt, 5,
            0, 0, 0, "인터넷 밈(Meme)에서 출발하여 거대한 커뮤니티의 힘으로 성장한 원조 강아지 테마 코인입니다. 특정 억만장자의 사랑을 받기도 합니다.", "Native", fournanceListed: true, isDefaultListed: true),

        new("라이트코인", "LTC", 5000.0, 74000000, 115000000, CoinTheme.Layer1, ProofType.PoW, CoinClass.Alt, 3,
            3, 0, 1000, "비트코인의 소스 코드를 바탕으로 생성 속도와 수수료를 개선하여, 일상적인 소액 결제에 특화된 '디지털 은'을 표방합니다.", "Native", isDefaultListed: true),

        new("프레지던트", "PRSDNT", 8000.0, 46000000, 115000000, CoinTheme.Meme, ProofType.Token, CoinClass.Alt, 5,
            0, 0, 0, "글로벌 선거와 정치적 이슈에 따라 가격이 요동치는 대표적인 정치 테마(PoliFi) 밈 코인입니다.", "0x576...4b1", fournanceListed: true, isDefaultListed: true),

        new("유에스티티", "USTT", 1350.0, 23412000000, 23412000000, CoinTheme.Stable, ProofType.Token, CoinClass.Stable, 1,
            0, 0, 0, "1코인이 1달러의 가치를 유지하도록 설계되어, 암호화폐 시장에서 기축통화 역할을 하는 세계 1위 스테이블 코인입니다.", "0xdAC...1ec", bullbitListed: true, isDefaultListed: true),

        new("유에스씨씨", "USCC", 1350.0, 18642000000, 18642000000, CoinTheme.Stable, ProofType.Token, CoinClass.Stable, 1,
            0, 0, 0, "철저한 규제 준수와 투명한 현금성 자산 담보를 바탕으로 발행되어, 높은 신뢰도를 자랑하는 안전한 스테이블 코인입니다.", "0xA0b...eb6", bullbitListed: true, isDefaultListed: true),

        new("러너", "RUN", 578.5, 20000000, 115000000, CoinTheme.Meme, ProofType.Token,CoinClass.Alt,
            5, 0, 2, 0, "사용자의 운동 데이터를 추적하여 걷거나 뛸 때마다 수익을 창출하는 웹3 라이프스타일(M2E) 생태계의 핵심 보상 토큰입니다", "0x771...bc2", bullbitListed: true, isDefaultListed: true),

        new("프로그", "FROG", 0.18, 115000000, 115000000, CoinTheme.Meme, ProofType.Token,CoinClass.Alt, 4, 0, 0, 0, "강아지 밈 코인들의 독주를 막기 위해 인터넷의 상징적인 초록색 개구리를 모티브로 탄생한 반항적인 밈 코인입니다.", "0x698...92c",  bullbitListed: true, isDefaultListed: true),
        new("시나나", "SNA", 130000.0, 85000000, 115000000, CoinTheme.Layer1, ProofType.PoS,CoinClass.Alt, 3, 0, 0, 500, "초당 수만 건의 트랜잭션을 처리하는 압도적인 속도와 저렴한 수수료를 무기로, 기존 플랫폼들의 한계를 돌파한 차세대 레이어1 블록체인입니다.", "Native", bullbitListed: true, isDefaultListed: true),
        new("오타쿠코인", "OTAKU", 80.0, 90000000, 115000000, CoinTheme.Meme, ProofType.Token,CoinClass.Alt, 4, 0, 0, 0, "애니메이션, 코믹스 등 서브컬처 팬덤을 하나로 연결하고, 독점적인 디지털 굿즈(NFT) 거래를 지원하는 생태계 코인입니다.", "0x882...fa1", bullbitListed: true, isDefaultListed: true),
        new("수파트럼", "STR", 345, 250000000, 1000000000, CoinTheme.Layer2, ProofType.Token,CoinClass.Alt, 4, 0, 2, 0, "이더리움의 확장성을 높이는 주요 레이어2 솔루션입니다.", "0x912...62b", bullbitListed: true, isDefaultListed: true),
        new("인텔리전스", "INT", 500, 250000000, 1000000000, CoinTheme.AI, ProofType.Token,CoinClass.Alt, 4, 0, 2, 0, "전 세계인의 홍채를 스캔해 고유한 디지털 ID를 부여하고, AI 시대에 대비한 보편적 기본소득을 지급하는 혁신적인 프로젝트입니다.", "0x163...54q", bullbitListed: true, isDefaultListed: true),
        new("가스트", "GHST", 180000.0, 115000000, 115000000, CoinTheme.DeFi, ProofType.PoS,CoinClass.Alt, 3, 0, 0, 0, "가상 자산을 담보로 예치하고 대출을 받을 수 있으며, 예치자에게 유동성 공급 보상을 제공하는 선두적인 탈중앙화 금융(DeFi) 프로토콜입니다.", "Native", bullbitListed: true, isDefaultListed: true),

        // =========================================================
        // 2. [랜덤 상장] - isDefaultListed: false 추가
        // =========================================================
        new("티렉스", "TRAX", 280, 870000000, 1000000000, CoinTheme.Layer1, ProofType.PoS,CoinClass.Alt, 2, 0, 0, 1000, "고효율 네트워크로 송금 등의 효율적인 네트워크 입니다.", "Native", bullbitListed: true, isDefaultListed: false),
        new("탄", "TAN", 2000.0, 34000000, 115000000, CoinTheme.Layer1, ProofType.PoS,CoinClass.Alt, 3, 0, 0, 200, "수억 명의 유저를 보유한 글로벌 메신저 앱과 직접 연동되어, 채팅창 내에서 송금과 디앱 구동이 가능한 거대한 생태계 코인입니다.", "Native", isDefaultListed: false),
        new("오렌지코인", "ORNG", 110.0, 50000000, 115000000, CoinTheme.ZK, ProofType.Token,CoinClass.Alt, 4, 0, 3, 0, "사용자의 소셜 미디어 활동과 영향력을 데이터화하여 보상을 제공하고, 프라이버시를 완벽히 보호하는 차세대 웹3 소셜 플랫폼 코인입니다.", "0x43a...221", isDefaultListed: false),



        // =========================================================
        // 3. [이벤트 전용] - bullbitListed: false, isDefaultListed: false 추가
        // =========================================================
        new("아파트", "APT", 1000, 15000000, 115000000, CoinTheme.Layer1, ProofType.PoS,CoinClass.Alt, 3, 0, 1, 0, "세계적인 IT 대기업 출신 개발자들이 모여 독자적인 합의 알고리즘으로 구축한, 극강의 안정성과 속도를 자랑하는 차세대 메인넷입니다.", "Native", bullbitListed: false, isDefaultListed: false),
        new("워터", "WAT", 300, 12000000, 115000000, CoinTheme.Layer1, ProofType.PoS,CoinClass.Alt, 3, 0, 1, 0, "자산 지향적인 'RUN' 프로그래밍 언어를 도입하여, 병렬 처리로 트랜잭션 지연을 완전히 없앤 혁신적인 레이어1 블록체인입니다.", "Native", bullbitListed: false, isDefaultListed: false),
        new("펭귄코인", "PENGU", 10.0, 5000000, 115000000, CoinTheme.Meme, ProofType.Token,CoinClass.Alt, 4, 0, 2, 0, "단순한 밈을 넘어 귀여운 캐릭터 IP를 기반으로 오프라인 장난감 시장까지 진출하며 충성도 높은 팬덤을 구축한 펭귄 밈 코인입니다", "6tzX...k5L", bullbitListed: false, isDefaultListed: false),
        new("초코코인", "CHK", 300, 20000000, 115000000, CoinTheme.Meme, ProofType.Token, CoinClass.Alt,4, 0, 3, 0, "달콤한 초콜릿처럼 시장에 소소한 즐거움을 주기 위해 탄생했으며, 주기적인 에어드랍 이벤트로 개미 투자자들의 사랑을 받는 코인입니다.", "4vcX...zLq", bullbitListed: false, isDefaultListed: false),
        new("버츄얼랜드", "VIRTUAL", 1200, 250000000, 1000000000, CoinTheme.AI, ProofType.Token, CoinClass.Alt, 3, 0, 2, 0, "텍스트, 음성, 시각 정보를 이해하는 다중 모달 AI 에이전트를 사용자가 직접 생성하고 토큰화하여 수익을 창출하는 메타버스 플랫폼입니다.", "0x52a1...3wE", bullbitListed: false, isDefaultListed: false),
        new("아이돌", "IDOL", 1000, 50000000, 115000000, CoinTheme.RWA, ProofType.Token,CoinClass.Alt, 5, 0, 2, 0, "실제 음원 저작권(RWA)을 조각 투자 방식으로 토큰화하여, 팬들이 직접 아티스트를 발굴하고 수익을 공유받을 수 있는 음악 생태계 코인입니다.", "0x742...1e3", bullbitListed: false, isDefaultListed: false),
        new("보라거위", "PGOOSE", 150, 500000000, 1000000000, CoinTheme.Meme, ProofType.Token,CoinClass.Alt, 5, 0, 2, 0, "진지한 투자에 지친 사람들을 위해 순수하게 재미와 커뮤니티의 결속력만으로 뭉쳐 폭발적으로 성장한 보라색 거위 밈 코인입니다.", "4vzX...bqm", bullbitListed: false, isDefaultListed: false),
        new("달", "MOON", 1500, 5206000000, 5206000000, CoinTheme.Governance, ProofType.Token,CoinClass.Alt, 5, 0, 2, 0, "자체 스테이블 코인의 가치 유지를 위해 알고리즘으로 발행량이 조절되는 혁신적인 거버넌스 코인입니다.", "Native", bullbitListed: false, isDefaultListed: false),
    };
}