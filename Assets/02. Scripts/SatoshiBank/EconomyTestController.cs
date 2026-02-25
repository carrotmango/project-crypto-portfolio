using UnityEngine;
using System.Linq;
using System; // 목록 분류를 위해 꼭 필요합니다!

public class EconomyTestController : MonoBehaviour {

    [Header("차트 갱신 설정")]
    public float updateInterval = 0.5f;

    void Update() {
        if (Input.GetKeyDown(KeyCode.T)) {

            if (CoinManager.Instance != null) {
                // 이제 Normal이 아니라 SuperFast로 쏩니다!
                CoinManager.Instance.SetTimeSpeed(TimeSpeed.test);
                Debug.Log("<color=red>초고속 테스트 모드 시작!</color>");
            }
        }
        if (Input.GetKeyDown(KeyCode.K)) {
            if (EffectManager.Instance != null) {
                // JSON에 있는 키값을 넣으시면 됩니다.
                string testKey = "trump_president_02";
                EffectManager.Instance.Apply(testKey);
                Debug.Log($"<color=#00FF00>[시나리오 테스트]</color> <b>{testKey}</b> 발동됨!");
            }
        }
        if (Input.GetKeyDown(KeyCode.L)) {
            if (CoinManager.Instance != null) {
                var cm = CoinManager.Instance;
                var now = cm.CurrentDateTime;
                var globalPhase = cm.CurrentMarket;

                string log = $"<color=white>=== [페이즈 지배구조 리포트] ===</color>\n";
                log += $"<b>현재 시간:</b> {now:MM/dd HH:mm}\n";
                log += $"<b>👑 전체 마켓 페이즈:</b> <color=yellow>{globalPhase}</color>\n";
                log += "--------------------------------------------------\n";

                // 상장된 코인만 체크
                var activeCoins = cm.coins.Where(c => c.IsListed && !c.IsDelisted).ToList();

                foreach (var coin in activeCoins) {
                    // [핵심 로직 검증]
                    // 현재 시간이 오버라이드 종료 시간보다 이전인가?
                    bool isOverrideActive = now < coin.PhaseOverrideEndTime;

                    if (isOverrideActive) {
                        // 1. 독립 페이즈 적용 중 (시장 무시)
                        TimeSpan left = coin.PhaseOverrideEndTime - now;
                        log += $"<b>{coin.Symbol}</b> : <color=cyan><b>[독립 행동]</b> {coin.CurrentPhaseOverride}</color> " +
                               $"(남은 시간: {left.TotalHours:F1}h / {coin.PhaseOverrideEndTime:HH:mm}까지)\n";
                    } else {
                        // 2. 오버라이드 없음 -> 시장 페이즈 적용 중
                        log += $"<b>{coin.Symbol}</b> : <color=grey>[시장 순응] {globalPhase} (Global)</color>\n";
                    }
                }
                Debug.Log(log);
            }
        }

        // -----------------------------------------------------------
        // [M키] 코인 등급(Class) 및 세그먼트(Segment) 분류 현황 출력
        // -----------------------------------------------------------
        if (Input.GetKeyDown(KeyCode.M)) {
            if (CoinManager.Instance != null) {
                var coins = CoinManager.Instance.coins;

                // 등급별 분류
                var majorList = coins.Where(c => c.Class == CoinClass.Major).Select(c => c.Symbol);
                var altList = coins.Where(c => c.Class == CoinClass.Alt).Select(c => c.Symbol);
                var stableList = coins.Where(c => c.Class == CoinClass.Stable).Select(c => c.Symbol);
                var trashList = coins.Where(c => c.Class == CoinClass.Trash).Select(c => c.Symbol);

                string report = "<color=white>[코인 신분 등급 리포트]</color>\n" +
                                $"<color=yellow><b>● MAJOR:</b></color> {string.Join(", ", majorList)}\n" +
                                $"<color=cyan><b>● ALT:</b></color> {string.Join(", ", altList)}\n" +
                                $"<color=grey><b>● STABLE:</b></color> {string.Join(", ", stableList)}\n" +
                                $"<color=red><b>● TRASH:</b></color> {string.Join(", ", trashList)}\n\n" +
                                "<color=orange>[Total1 적용 대상 (Trash 제외)]</color>\n" +
                                string.Join(", ", coins.Where(c => c.Class != CoinClass.Trash && c.Class != CoinClass.Stable).Select(c => c.Symbol));

                Debug.Log(report);
            }
        }

        if (Input.GetKeyDown(KeyCode.R)) {

            if (CoinManager.Instance != null) {
                CoinManager.Instance.SetTimeSpeed(TimeSpeed.Normal);
                Debug.Log("<color=cyan>정상 속도 복구</color>");
            }
        }
        if (Input.GetKeyDown(KeyCode.Y)) {
            if (CoinManager.Instance != null) {
                var phase = CoinManager.Instance.CurrentMarket;
                var currentTime = CoinManager.Instance.CurrentDateTime;
                Debug.Log($"<color=yellow>[시장 상태]</color> 현재 페이즈: <b>{phase}</b> | 현재 시간: <b>{currentTime:MM/dd HH:mm}</b>");
            }
        }
        HandlePhaseHotkeys();

        // [U키] 코인 목록 디버그 (랜덤 제외 코인 포함)
        if (Input.GetKeyDown(KeyCode.U)) {
            if (CoinManager.Instance != null) {
                var allManagerCoins = CoinManager.Instance.coins;

                // 1. 활성 상장 (IsListed가 true이고 상폐가 아님)
                var activeCoins = allManagerCoins
                    .Where(c => c.IsListed && !c.IsDelisted)
                    .Select(c => c.Symbol).ToList();

                // 2. 랜덤 제외 (Manager 리스트엔 있지만 IsListed가 false인 경우)
                var randomExcludedCoins = allManagerCoins
                    .Where(c => !c.IsListed)
                    .Select(c => c.Symbol).ToList();

                // 3. 상폐 또는 거래 정지 (IsListed는 true인데 IsDelisted가 true)
                var delistedCoins = allManagerCoins
                    .Where(c => c.IsListed && c.IsDelisted)
                    .Select(c => c.Symbol).ToList();

                // 4. DB에는 있으나 Manager에 생성조차 안 된 코인 (이벤트 전용 등)
                var managerSymbols = allManagerCoins.Select(c => c.Symbol).ToHashSet();
                var dbOnlyCoins = CoinMetaDatabase.AllCoins
                    .Where(meta => !managerSymbols.Contains(meta.Symbol))
                    .Select(meta => meta.Symbol).ToList();

                // 문자열 변환 (리스트 개수 체크 포함)
                string activeStr = activeCoins.Count > 0 ? string.Join(", ", activeCoins) : "없음";
                string excludedStr = randomExcludedCoins.Count > 0 ? string.Join(", ", randomExcludedCoins) : "없음";
                string delistedStr = delistedCoins.Count > 0 ? string.Join(", ", delistedCoins) : "없음";
                string dbOnlyStr = dbOnlyCoins.Count > 0 ? string.Join(", ", dbOnlyCoins) : "없음";

                // 디버그 로그 출력
                Debug.Log($"<color=lime>[코인 상세 현황]</color>\n" +
                          $"<color=cyan><b>● 활성 상장 ({activeCoins.Count}):</b></color> {activeStr}\n" +
                          $"<color=#FFA500><b>● 랜덤 제외 ({randomExcludedCoins.Count}):</b></color> {excludedStr}\n" +
                          $"<color=red><b>● 거래 정지/상폐 ({delistedCoins.Count}):</b></color> {delistedStr}\n" +
                          $"<color=grey><b>● DB 전용/이벤트 ({dbOnlyCoins.Count}):</b></color> {dbOnlyStr}");
            }
        }

        if (Input.GetKeyDown(KeyCode.P)) {
            if (CoinManager.Instance != null && ChartPatternEngine.Instance != null) {
                // 상장된 모든 코인을 클래스(신분) 순으로 정렬해서 가져옵니다.
                var listedCoins = CoinManager.Instance.coins
                    .Where(c => c.IsListed && !c.IsDelisted)
                    .OrderBy(c => c.Class) // Major -> Alt -> Stable -> Trash 순 정렬
                    .ToList();

                string log = $"<color=orange>[차트 패턴 및 등급 현황] 시장 페이즈: {CoinManager.Instance.CurrentMarket}</color>\n";

                foreach (var coin in listedCoins) {
                    // 패턴 정보 가져오기 (감지된 패턴 이름 포함)
            
                        string patternInfo = ChartPatternEngine.Instance.GetPatternDebugInfo(coin.Symbol);

                    // 클래스(신분)별 강조 색상 지정
                    string classColor = coin.Class switch {
                        CoinClass.Major => "yellow",  // 대장주는 황금색
                        CoinClass.Alt => "cyan",    // 알트는 청록색
                        CoinClass.Stable => "grey",   // 스테이블은 회색
                        CoinClass.Trash => "#FF00FF", // 잡코인(Trash)은 보라색
                        _ => "white"
                    };

                    // 등락률 계산 및 색상 (코인판 국룰: 상승 빨강, 하락 파랑)
                    double change = (coin.InitialPrice > 0) ? (coin.CurrentPrice - coin.InitialPrice) / coin.InitialPrice * 100.0 : 0;
                    string changeColor = change >= 0 ? "#FF4500" : "#1E90FF"; // 선명한 빨강 / 선명한 파랑

                    // 로그 한 줄 완성: [신분] 심볼: 패턴정보 | 현재변동률
                    log += $"[<color={classColor}>{coin.Class}</color>] <b>{coin.Symbol}</b>: {patternInfo} | 수익률: <color={changeColor}>{change:F2}%</color>\n";
                }

                Debug.Log(log);
            } else {
                Debug.LogWarning("CoinManager 또는 ChartPatternEngine이 없습니다.");
            }
        }
    }
    private void HandlePhaseHotkeys() {
        if (CoinManager.Instance == null) return;

        // 1~9번 키 입력 체크
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetMarket(MarketPhase.MegaBull);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SetMarket(MarketPhase.SuperBull);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SetMarket(MarketPhase.BigBull);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SetMarket(MarketPhase.Bull);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SetMarket(MarketPhase.MildBull);
        else if (Input.GetKeyDown(KeyCode.Alpha6)) SetMarket(MarketPhase.Sideways);
        else if (Input.GetKeyDown(KeyCode.Alpha7)) SetMarket(MarketPhase.MildBear);
        else if (Input.GetKeyDown(KeyCode.Alpha8)) SetMarket(MarketPhase.Bear);
        else if (Input.GetKeyDown(KeyCode.Alpha9)) SetMarket(MarketPhase.MegaBear);
    }

    private void SetMarket(MarketPhase phase) {
        // [주의] 현재 롤백된 구조에서는 CoinManager.CurrentMarket만 바꿔주면 
        // 다음 GenerateNextPrice 호출 시 적용됩니다.
        CoinManager.Instance.CurrentMarket = phase;

        // 지수 계산을 위해 OnMarketUpdated를 호출해주면 UI가 즉각 반응합니다.
        // CoinManager.Instance.OnMarketUpdated?.Invoke(); 

        Debug.Log($"<color=#FF00FF>[테스트 제어]</color> 시장 페이즈가 강제로 변경됨: <b>{phase}</b>");
    }
}