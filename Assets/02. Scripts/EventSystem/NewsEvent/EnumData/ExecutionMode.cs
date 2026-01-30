public enum ExecutionMode {
    None = 0,

    RandomInWindow = 1,     // 기간 내 독립 실행 (랜덤 / 확정 가능)
    AfterPrerequisite = 2  // 선행 이벤트 이후 실행 (의존성)
}
