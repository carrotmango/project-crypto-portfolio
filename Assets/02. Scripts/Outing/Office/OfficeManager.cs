using System;
using UnityEngine;

public class OfficeManager : MonoBehaviour {

    public static OfficeManager Instance;

    [Header("Company Stats")]
    public long companyCapital = 0;
    public long baseSalary = 1_000_000;
    public long currentMonthlySalary = 0;

    [Header("Rank System")]
    public int currentRankIndex = 0;
    public readonly long[] capitalMilestones = {
        10_000_000,      // 0->1 (주임)
        50_000_000,      // 1->2 (대리)
        100_000_000,     // 2->3 (차장)
        1_000_000_000,   // 3->4 (부장)
        5_000_000_000,   // 4->5 (이사회 멤버)
        10_000_000_000   // 5->6 (경제적 자유인)
    };

    public event Action OnSalaryChanged;
    public event Action OnPromotion;

    [Header("Time Settings")]
    public DateTime nextSalaryDate;

    [Header("Skill 1: Work Efficiency")]
    [SerializeField] private int salarySkillLevel = 0;
    public int SalarySkillLevel => salarySkillLevel;
    public const int MAX_SALARY_SKILL_LEVEL = 30;

    [Header("Skill 1 Cost")]
    public int baseSkillCost = 1_000_000;
    public float costMultiplier = 1.25f;

    // ==========================================
    // [수정] Skill 2: 대박 성과 (배율 떡상)
    // ==========================================
    [Header("Skill 2: Critical Hit (Multi-plier)")]
    public int CriticalSkillLevel = 0; // 0부터 시작 (0% 확률)
    public const int MAX_CRIT_LEVEL = 30;

    [Header("Skill 2 Cost Settings")]
    public int critBaseCost = 3_000_000;
    public float critCostMultiplier = 1.5f;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (CoinManager.Instance != null && nextSalaryDate == default) {
            nextSalaryDate = CoinManager.Instance.CurrentDateTime.AddDays(7);
        }

        RecalculateSalary();
    }

    public void AddCapital(long amount) {
        if (amount <= 0) return;
        companyCapital += amount;
        CheckPromotion();
        RecalculateSalary();
    }

    // ===================================================
    // [핵심 로직 변경] 확률 및 배율
    // ===================================================

    // 1. 크리티컬 확률 (0% ~ 100%)
    public float GetCriticalChance() {
        if (CriticalSkillLevel <= 0) return 0f;
        if (CriticalSkillLevel >= MAX_CRIT_LEVEL) return 100.0f;

        // Lv.1 ~ Lv.30 구간을 3.3% ~ 100%로 매핑
        // 간단하게 (현재레벨 / 30) * 100
        return ((float)CriticalSkillLevel / MAX_CRIT_LEVEL) * 100.0f;
    }

    // 2. 현재 레벨에 따른 배율 반환 (구간별 적용)
    public long GetCurrentCritMultiplier() {
        if (CriticalSkillLevel <= 0) return 1; // 스킬 없으면 1배

        if (CriticalSkillLevel >= 30) return 5; // 30렙(만렙): 5배
        if (CriticalSkillLevel >= 20) return 4; // 20~29렙: 4배
        if (CriticalSkillLevel >= 10) return 3; // 10~19렙: 3배

        return 2; // 1~9렙: 2배
    }

    // 3. 업그레이드 비용 계산
    public long GetCritUpgradeCost() {
        if (CriticalSkillLevel >= MAX_CRIT_LEVEL) return 0;

        // 0렙일 때(1렙 갈때)는 baseCost 그대로
        // 1렙 이상부터는 승수로 증가
        double cost = critBaseCost * Math.Pow(critCostMultiplier, CriticalSkillLevel);

        // 10만원 단위 절삭
        return (long)(Math.Floor(cost / 100000) * 100000);
    }

    // 4. 업그레이드 실행
    public bool TryUpgradeCritSkill() {
        if (CriticalSkillLevel >= MAX_CRIT_LEVEL) return false;

        long cost = GetCritUpgradeCost();

        if (PlayerManager.Instance.satoshiBankCash < cost) return false;

        PlayerManager.Instance.satoshiBankCash -= cost;
        CriticalSkillLevel++;
        TransactionManager.Instance.AddRecord("스킬강화", cost, "출금", "사토시 현금");
        return true;
    }

    // ===================================================
    // [수정] 클릭 수익 계산 (새로운 배율 적용)
    // ===================================================
    public long CalculateClickMoney(out bool isCritical) {
        // 기본 수익
        long clickBaseAmount = 10000;
        float efficiency = GetSalaryEfficiencyPercent();
        long finalBase = (long)(clickBaseAmount * (1 + efficiency / 100f));

        // 크리티컬 판정
        float chance = GetCriticalChance();

        // 확률 0%면 바로 리턴
        if (chance <= 0f) {
            isCritical = false;
            return finalBase;
        }

        // 확률 100% 이상 (만렙 등)
        if (chance >= 100f) {
            isCritical = true;
            return finalBase * GetCurrentCritMultiplier();
        }

        // 확률 계산
        float randomVal = UnityEngine.Random.Range(0f, 100f);
        if (randomVal < chance) {
            isCritical = true;
            return finalBase * GetCurrentCritMultiplier(); // 구간별 배율 적용
        } else {
            isCritical = false;
            return finalBase;
        }
    }

    // ... (이하 기존 직급, 급여, 승진 로직 동일) ...

    private void CheckPromotion() {
        bool isPromoted = false;
        while (currentRankIndex < capitalMilestones.Length &&
               companyCapital >= capitalMilestones[currentRankIndex]) {
            currentRankIndex++;
            Debug.Log($"[축 승진] {GetCurrentTitle()} 달성!");
            isPromoted = true;
        }

        if (isPromoted) {
            OnPromotion?.Invoke();
            RecalculateSalary();
        }
    }

    public string GetCurrentTitle() => GetTitleName(currentRankIndex);

    public string GetTitleName(int index) {
        switch (index) {
            case 0: return "인턴노예";
            case 1: return "정규직 주임";
            case 2: return "자본주의 대리";
            case 3: return "납입왕 차장";
            case 4: return "보이지 않는 손 부장";
            case 5: return "이사회 멤버";
            case 6: return "경제적 자유인";
            default: return "경제적 자유인";
        }
    }

    public long GetNextCapitalMilestone() {
        if (currentRankIndex >= capitalMilestones.Length) return -1;
        return capitalMilestones[currentRankIndex];
    }

    public long CalculateMonthlySalary() {
        // 1. 기본 베이스 (500,000)
        long calculatedBase = baseSalary;

        if (currentRankIndex >= 1) {
            calculatedBase += 2_000_000;
        }

        // 2. 직급별 배수 (Multiplier) - 보내주신 수치 적용
        long rankMultiplier = 1;

        if (currentRankIndex >= 6) rankMultiplier = 60;       // 경제적 자유인: 60배
        else if (currentRankIndex >= 5) rankMultiplier = 25;  // 이사회: 25배
        else if (currentRankIndex >= 3) rankMultiplier = 4;   // 차장  4배 
        else if (currentRankIndex >= 2) rankMultiplier = 2;   // 대리: 2배

        // 배수 적용
        calculatedBase *= rankMultiplier;

        // 3. 자본금 이자율 (0.1% 고정)
        double interestRate = 0.001;
        double capitalBonus = companyCapital * interestRate;

        // 4. 스킬 1(업무 효율) 적용
        float efficiencyMultiplier = 1f + (GetSalaryEfficiencyPercent() / 100f);

        // 최종 합산
        double total = (calculatedBase + capitalBonus) * efficiencyMultiplier;

        return (long)total;
    }

    public void RecalculateSalary() {
        currentMonthlySalary = CalculateMonthlySalary();
        OnSalaryChanged?.Invoke();
    }

    public float GetSalaryEfficiencyPercent() => CalculateEfficiencyAtLevel(salarySkillLevel);

    public float CalculateEfficiencyAtLevel(int level) {
        if (level <= 0) return 0f;
        float progress = (float)level / MAX_SALARY_SKILL_LEVEL;
        return Mathf.Sqrt(progress) * 300f;
    }

    public int GetSkillUpgradeCost() {
        if (salarySkillLevel >= MAX_SALARY_SKILL_LEVEL) return 0;
        double cost = baseSkillCost * Math.Pow(costMultiplier, salarySkillLevel);
        return (int)(Math.Floor(cost / 10000) * 10000);
    }

    public bool CanUpgradeSalarySkill() {
        if (salarySkillLevel >= MAX_SALARY_SKILL_LEVEL) return false;
        return PlayerManager.Instance.satoshiBankCash >= GetSkillUpgradeCost();
    }

    public bool TryUpgradeSalarySkill() {
        if (!CanUpgradeSalarySkill()) return false;
        int cost = GetSkillUpgradeCost();
        PlayerManager.Instance.satoshiBankCash -= cost;
        salarySkillLevel++;
        RecalculateSalary();
        TransactionManager.Instance.AddRecord("스킬강화", cost, "출금", "사토시 현금");
        return true;
    }

    public float GetSalaryCycleDays() { return 7f; }

    public float GetDaysUntilNextSalary() {
        if (CoinManager.Instance == null) return 0f;
        DateTime now = CoinManager.Instance.CurrentDateTime;
        double remainDays = (nextSalaryDate - now).TotalDays;
        return Mathf.Max(0f, (float)remainDays);
    }

    public void TryPaySalary() {
        if (CoinManager.Instance == null) return;

        DateTime now = CoinManager.Instance.CurrentDateTime;

        // 지급일이 되었는가?
        if (now >= nextSalaryDate) {
            if (currentMonthlySalary > 0) {

                // 1. 기본 월급 가져오기
                long finalPay = currentMonthlySalary;
                bool isJackpot = false;
                long multiplier = 1;

                // 2. 크리티컬(스킬2) 확률 적용
                float chance = GetCriticalChance(); // 현재 확률 (0~100%)

                // 확률이 0보다 클 때만 계산
                if (chance > 0) {
                    // 100% 이상(만렙)이거나, 운 좋게 당첨된 경우
                    if (chance >= 100f || UnityEngine.Random.Range(0f, 100f) < chance) {
                        isJackpot = true;
                        multiplier = GetCurrentCritMultiplier(); // 2배~5배 가져오기
                        finalPay = currentMonthlySalary * multiplier;
                    }
                }

                // 3. 돈 지급
                PlayerManager.Instance.satoshiBankCash += finalPay;

                // 4. 로그 및 기록 (대박 여부에 따라 텍스트 다르게)
                string logTitle = isJackpot ? $"★급여 대박!({multiplier}배)★" : "대표 급여";
                DailyIncomeManager.Instance.AddSalary(finalPay, logTitle);
                PlayerManager.Instance.AddSalary(finalPay);

                // (선택) 대박 터지면 콘솔에 로그 띄우기
                if (isJackpot) {
                    Debug.Log($"<color=red><b>[JACKPOT] 월급 {multiplier}배 당첨! : {finalPay:N0}원</b></color>");
                }
            }

            // 다음 지급일 설정 (7일 뒤)
            nextSalaryDate = nextSalaryDate.AddDays(7);
        }
    }

    public float GetPartTimeJobMultiplier() {
        // 부장(Rank 4)의 기존 구간은 제거되고 여기서 통합 처리됨
        if (currentRankIndex >= 3) return 10.0f;

        // [수정] 대리(Rank 2): 500% (5배)
        if (currentRankIndex >= 2) return 5.0f;

        // 주임(Rank 1): 1.5배 (기존 유지)
        if (currentRankIndex >= 1) return 1.5f;

        // 인턴(Rank 0): 1배
        return 1.0f;
    }

    public long GetGambleLimit() {
        // 1. 경제적 자유인 (Rank 6): 10억
        if (currentRankIndex >= 6) return 1_000_000_000;

        // 2. 부장 (Rank 4) ~ 이사 (Rank 5): 1억
        if (currentRankIndex >= 4) return 100_000_000;

        // 3. 대리 (Rank 2) ~ 차장 (Rank 3): 1천만 원
        if (currentRankIndex >= 2) return 10_000_000;

        // 4. 인턴 ~ 주임: 200만 원
        return 2_000_000;
    }


    public int GetMaxLeverage() {
        if (currentRankIndex >= 6) return 100;
        if (currentRankIndex >= 4) return 20;
        if (currentRankIndex >= 3) return 5;
        return 1;
    }

    public bool IsRealEstateUnlocked() => currentRankIndex >= 2;
    public bool IsFuturesUnlocked() => currentRankIndex >= 3;
}