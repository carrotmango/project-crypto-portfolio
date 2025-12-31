using System;
using UnityEngine;

public class OfficeManager : MonoBehaviour {

    public static OfficeManager Instance;

    // 회사 자본 (납입금, 출금 불가)
    public long companyCapital = 0;

    // 대표 기본 급여
    public int baseSalary = 500_000;
    public int currentMonthlySalary = 0;

    // 자본 목표
    public int capitalMilestoneIndex = 0;

    public readonly long[] capitalMilestones = {
        10_000_000,
        25_000_000,
        50_000_000
    };

    // 마지막 급여 지급일 (게임 시간 기준)
    public DateTime lastSalaryPaidDate;

    // =========================
    // 업무 효율 (급여 스킬)
    // =========================
    [Header("Work Efficiency")]
    public int salarySkillLevel = 0;
    public const int MAX_SALARY_SKILL_LEVEL = 30;

    [Header("Upgrade Cost")]
    public int baseSkillCost = 1_000_000;     // Lv1
    public int linearCostStep = 500_000;      // Lv1~14
    public float lateCostMultiplier = 1.6f;   // Lv15+

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (CoinManager.Instance != null && lastSalaryPaidDate == default) {
            lastSalaryPaidDate = CoinManager.Instance.CurrentDateTime;
        }

        RecalculateSalary();
    }

    // =========================
    // 회사 자본
    // =========================
    public void AddCapital(long amount) {
        if (amount <= 0) return;

        companyCapital += amount;
        RecalculateSalary();
        CheckCapitalMilestone();
    }

    // =========================
    // 급여 계산
    // =========================
    public int CalculateMonthlySalary() {
        float capitalBonus = companyCapital * 0.001f; // 0.1%
        float efficiencyMultiplier =
            1f + (GetSalaryEfficiencyPercent() / 100f);

        float total =
            (baseSalary + capitalBonus) * efficiencyMultiplier;

        return Mathf.FloorToInt(total);
    }

    public void RecalculateSalary() {
        currentMonthlySalary = CalculateMonthlySalary();
    }

    // =========================
    // 업무 효율 효과 (1% ~ 300%)
    // =========================
    public float GetSalaryEfficiencyPercent() {
        if (salarySkillLevel <= 0) return 0f;

        float min = 1f;
        float max = 300f;
        float t =
            (salarySkillLevel - 1f) /
            (MAX_SALARY_SKILL_LEVEL - 1f);

        return Mathf.Lerp(min, max, t);
    }

    // =========================
    // 스킬 업그레이드 비용
    // =========================
    public int GetSkillUpgradeCost() {
        if (salarySkillLevel >= MAX_SALARY_SKILL_LEVEL)
            return 0;

        int nextLevel = salarySkillLevel + 1;

        // Lv1 ~ Lv14 (선형)
        if (nextLevel <= 14) {
            return baseSkillCost + (nextLevel - 1) * linearCostStep;
        }

        // Lv15+
        int base14Cost =
            baseSkillCost + 13 * linearCostStep;

        int over = nextLevel - 14;

        return Mathf.FloorToInt(
            base14Cost * Mathf.Pow(lateCostMultiplier, over)
        );
    }

    // =========================
    // 업그레이드 가능 여부
    // =========================
    public bool CanUpgradeSalarySkill() {
        if (salarySkillLevel >= MAX_SALARY_SKILL_LEVEL)
            return false;

        int cost = GetSkillUpgradeCost();
        return PlayerManager.Instance.satoshiBankCash >= cost;
    }

    // =========================
    // 업그레이드 실행
    // =========================
    public bool TryUpgradeSalarySkill() {
        if (!CanUpgradeSalarySkill())
            return false;

        int cost = GetSkillUpgradeCost();

        PlayerManager.Instance.satoshiBankCash -= cost;
        salarySkillLevel++;

        RecalculateSalary();

        Debug.Log(
            $"[회사] 업무 효율 레벨업 → Lv.{salarySkillLevel} (소모 {cost:N0})"
        );

        return true;
    }

    // =========================
    // 급여 지급
    // =========================
    public float GetSalaryCycleDays() {
        return 2f;
    }

    public float GetDaysUntilNextSalary() {
        if (CoinManager.Instance == null) return 0f;

        DateTime now = CoinManager.Instance.CurrentDateTime;
        DateTime nextPayTime =
            lastSalaryPaidDate.AddDays(GetSalaryCycleDays());

        double remainDays = (nextPayTime - now).TotalDays;
        return Mathf.Max(0f, (float)remainDays);
    }

    public void TryPaySalary() {
        if (CoinManager.Instance == null) return;
        if (GetDaysUntilNextSalary() > 0f) return;

        if (currentMonthlySalary <= 0) return;

        PlayerManager.Instance.satoshiBankCash += currentMonthlySalary;

        DailyIncomeManager.Instance.AddSalary(
            currentMonthlySalary,
            "대표 급여"
        );

        lastSalaryPaidDate = CoinManager.Instance.CurrentDateTime;

        Debug.Log(
            $"[회사] 대표 급여 지급 +{currentMonthlySalary:N0}"
        );
    }

    // =========================
    // 자본 목표
    // =========================
    void CheckCapitalMilestone() {
        while (
            capitalMilestoneIndex < capitalMilestones.Length &&
            companyCapital >= capitalMilestones[capitalMilestoneIndex]
        ) {
            OnCapitalMilestoneReached(capitalMilestoneIndex);
            capitalMilestoneIndex++;
        }
    }

    void OnCapitalMilestoneReached(int index) {
        switch (index) {
            case 0:
                Debug.Log("자본 목표 달성: 1천만 → 아르바이트 급여 강화 해금");
                break;
            case 1:
                Debug.Log("자본 목표 달성: 2천5백만");
                break;
            case 2:
                Debug.Log("자본 목표 달성: 5천만 → 선물 거래소 개방");
                break;
        }
    }

    public long GetNextCapitalMilestone() {
        if (capitalMilestoneIndex >= capitalMilestones.Length)
            return -1;

        return capitalMilestones[capitalMilestoneIndex];
    }
}
