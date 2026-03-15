public enum CoinEventType {
    Listing = 0,   // 신규 상장
    Delisting = 1, // 상폐
    Relisting = 2, // 재상장
    Rename = 3,    // 이름 심볼 변경
    Halt = 4,      // 거래 정지 (매수/매도 막힘)
    Resume = 5,    // 거래 재개 (정지 풀림),
    Warning = 6,    // 위험 지정
    RemoveWarning = 7 // 위험 해제
}
public enum CoinAlertType {
    None = -1,    // 없음 (기본)
    Warning = 1,  // 유의 (나중에 쓸 것)
    Danger = 2    // 위험 (지금 쓸 것)
}