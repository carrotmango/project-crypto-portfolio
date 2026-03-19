using System;
using UnityEngine;

public class TaxManager : MonoBehaviour {
    public static TaxManager Instance;

    [Header("Tax Status")]
    public double unpaidTaxAmount = 0;   // 현재 납부해야 할 세금
    public DateTime taxDueDate;          // 납부 기한
    public bool isTaxBillIssued = false; // 고지서 발송 여부

    [Header("Snapshot (지난 분기까지의 누적 기록)")]
    [SerializeField] private double prevBullbitProfit = 0;
    [SerializeField] private double prevFournancePnL = 0;
    [SerializeField] private long prevSalary = 0;
    [SerializeField] private long prevAlba = 0;
    [SerializeField] private long prevRealEstate = 0;
    [SerializeField] private long prevLottoWin = 0;
    [SerializeField] private long prevGambleNet = 0;

    public enum TaxPeriodType { Monthly, Quarterly }
    [Header("Tax Settings (세금 주기 설정)")]
    public TaxPeriodType taxPeriod = TaxPeriodType.Quarterly;

    private void Awake() {
        if (Instance == null) Instance = this;
    }

    private void Start() {
        if (CoinManager.Instance != null) {
            CoinManager.Instance.OnTimeAdvanced += CheckTaxPeriod;
        }
    }

    private void OnDestroy() {
        if (CoinManager.Instance != null) {
            CoinManager.Instance.OnTimeAdvanced -= CheckTaxPeriod;
        }
    }

    // 매일 시간이 흐를 때마다 체크
    private void CheckTaxPeriod(DateTime currentDate) {
        // 1. 강제 징수 체크
        if (isTaxBillIssued && unpaidTaxAmount > 0) {
            if (currentDate > taxDueDate) {
                EnforceTaxCollection();
            }
            return;
        }

        // 2. 고지서 발행일 체크
        if (!isTaxBillIssued && IsTaxMonth(currentDate)) {
            CalculateAndIssueBill(currentDate);
        }
    }

    //  세달 주기용
    //private bool IsTaxMonth(DateTime date) {
    //    return date.Day == 1 && (date.Month == 1 || date.Month == 4 || date.Month == 7 || date.Month == 10);
    //}

    // 한달 주기
    private bool IsTaxMonth(DateTime date) {
        if (date.Day != 1) return false; // 무조건 1일에만 체크

        if (taxPeriod == TaxPeriodType.Monthly) {
            return true; // 매달 1일이면 무조건 true
        } else {
            // Quarterly (분기별: 1, 4, 7, 10월)
            return date.Month == 1 || date.Month == 4 || date.Month == 7 || date.Month == 10;
        }
    }

    // 소득 계산 및 고지서 발행
    private void CalculateAndIssueBill(DateTime currentDate) {
        if (PlayerManager.Instance == null) return;
        var pm = PlayerManager.Instance;

        // --- [A. 소득 변동분 계산] ---
        double incomeBullbit = pm.realizedProfit - prevBullbitProfit;

        double currentFournancePnL = pm.fournanceRealizedPnL;
        double incomeFournanceUSD = currentFournancePnL - prevFournancePnL;
        double incomeFournanceKRW = incomeFournanceUSD * GlobalEconomyManager.UsdToKrw;

        long incomeSalary = pm.totalSalaryReceived - prevSalary;
        long incomeAlba = pm.totalPartTimeJobIncome - prevAlba;
        long incomeEstate = pm.totalRealEstateIncome - prevRealEstate;
        long incomeLotto = pm.totalLotteryWonAmount - prevLottoWin;

        long currentGambleNet = pm.totalGambleEarned - pm.totalGambleSpent;
        long incomeGamble = currentGambleNet - prevGambleNet;

        // --- [B. 총 소득 합산] ---
        double totalQuarterIncome = incomeBullbit + incomeFournanceKRW + incomeSalary
                                  + incomeAlba + incomeEstate + incomeLotto + incomeGamble;

        Debug.Log($"[Tax 정산] 현물:{incomeBullbit:N0} / 선물:{incomeFournanceKRW:N0} / 노동:{incomeSalary + incomeAlba:N0} / 오락실:{incomeGamble:N0}");

        // --- [C. 스냅샷 갱신] ---
        prevBullbitProfit = pm.realizedProfit;
        prevFournancePnL = currentFournancePnL;
        prevSalary = pm.totalSalaryReceived;
        prevAlba = pm.totalPartTimeJobIncome;
        prevRealEstate = pm.totalRealEstateIncome;
        prevLottoWin = pm.totalLotteryWonAmount;
        prevGambleNet = currentGambleNet;

        // --- [D. 세금 확정] ---
        if (totalQuarterIncome > 0) {
            double rate = GlobalEconomyManager.TaxRate / 100.0;
            unpaidTaxAmount = Math.Floor(totalQuarterIncome * rate);

            int daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
            taxDueDate = new DateTime(currentDate.Year, currentDate.Month, daysInMonth);

            isTaxBillIssued = true;
            Debug.Log($"[Tax] 고지서 발송! 소득:{totalQuarterIncome:N0} -> 세액:{unpaidTaxAmount:N0}");
        } else {
            unpaidTaxAmount = 0;
            isTaxBillIssued = false;
            Debug.Log($"[Tax] 이번 분기는 적자이거나 소득이 없습니다. (소득: {totalQuarterIncome:N0})");
        }

        if (TaxPanel.Instance != null) TaxPanel.Instance.RefreshTaxUI();
    }
    public bool PayTax() {
        if (unpaidTaxAmount <= 0) return false;

        // 잔액 체크는 TaxPanel UI 레벨에서 먼저 하겠지만, 여기서 한 번 더 안전장치
        if (PlayerManager.Instance.satoshiBankCash < unpaidTaxAmount) {
            return false; // 납부 실패
        }

        // 1. 돈 차감
        PlayerManager.Instance.ChangeSatoshiMoney(-unpaidTaxAmount);

        // 2. ★ [핵심 수정] 기록은 무조건 한글 원본 데이터 고정! (출금)
        // 번역된 텍스트 대신 "소득세", "출금" 이라는 고정된 단어로 보내주세요.
        TransactionManager.Instance.AddRecord("소득세", unpaidTaxAmount, "출금", "사토시 현금");

        Debug.Log($"[Tax] 정상 납부 완료: {unpaidTaxAmount:N0}원");

        // 3. 상태 초기화
        ClearTax();
        return true;
    }
    private void EnforceTaxCollection() {
        double penaltyTax = unpaidTaxAmount * 1.1; // 10% 가산세

        Debug.LogWarning($"[Tax] 강제 징수 실행! 원금:{unpaidTaxAmount:N0} -> 징수금:{penaltyTax:N0}");

        if (PlayerManager.Instance != null) {
            // 1. 돈 차감 (마통 가능)
            PlayerManager.Instance.ChangeSatoshiMoney(-penaltyTax);

            // 2. ★ [핵심 수정] 기록은 무조건 한글 원본 데이터 고정! (출금)
            TransactionManager.Instance.AddRecord("소득세(강제징수)", penaltyTax, "출금", "사토시 현금");
        }

        ClearTax();
    }

    // 내부 상태 초기화용 (외부에서 직접 호출하기보단 PayTax나 Enforce 내부에서 씀)
    public void ClearTax() {
        unpaidTaxAmount = 0;
        isTaxBillIssued = false;
        if (TaxPanel.Instance != null) TaxPanel.Instance.RefreshTaxUI();
    }
}