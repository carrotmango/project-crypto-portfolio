using System;

/// 진행 중인 캔들 (실시간으로 자라는 봉)
/// - displayPrice를 받아 OHLC를 갱신
/// - 봉 마감 전까지 High/Low 계속 갱신

public class RuntimeCandle {

    public TradeType TradeFlag = TradeType.None;
    public double Open { get; private set; }
    public double High { get; private set; }
    public double Low { get; private set; }
    public double Close { get; private set; }

    public bool IsInitialized { get; private set; }
    public bool IsClosed { get; private set; }
    public DateTime Timestamp { get; set; }


    /// 새 봉 시작 (Open 설정)
    public void Start(double startPrice) {
        Open = startPrice;
        High = startPrice;
        Low = startPrice;
        Close = startPrice;

        IsInitialized = true;
        IsClosed = false;

        if (CoinManager.Instance != null) {
            Timestamp = CoinManager.Instance.CurrentDateTime;
        }
    }

    public RuntimeCandle() {
    }

    /// 매 프레임 호출: 현재 displayPrice 반영
    public void UpdatePrice(double displayPrice) {
        if (!IsInitialized || IsClosed)
            return;

        Close = displayPrice;

        if (displayPrice > High)
            High = displayPrice;

        if (displayPrice < Low)
            Low = displayPrice;
    }

    /// 봉 마감 (다음 단계에서 사용)
    public void CloseCandle() {
        IsClosed = true;
    }
    public RuntimeCandle(double open, double high, double low, double close) {
        Open = open;
        High = high;
        Low = low;
        Close = close;

        IsInitialized = true;
        IsClosed = true;

        if (CoinManager.Instance != null) {
            Timestamp = CoinManager.Instance.CurrentDateTime;
        }
    }

    // 기존 생성자는 그대로 두고, 이 함수를 추가합니다.
    public void SetupAsActive(double open, double high, double low, double close) {
        Open = open;
        High = high;
        Low = low;
        Close = close;

        IsInitialized = true;
        IsClosed = false; // 핵심: 닫히지 않은 상태로 설정

        if (CoinManager.Instance != null) {
            Timestamp = CoinManager.Instance.CurrentDateTime;
        }
    }
}
