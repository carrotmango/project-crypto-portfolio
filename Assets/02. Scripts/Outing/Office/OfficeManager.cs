using System;
using UnityEngine;

public class OfficeManager : MonoBehaviour {

    public static OfficeManager Instance;

    [Header("Company Stats")]
    // 회사 자본 (납입금, 출금 불가)
    public long companyCapital = 0;
    // 대표 기본 급여
    public long baseSalary = 500_000;
    public long currentMonthlySalary = 0;

    [Header("Capital Milestones")]
    public int capitalMilestoneIndex = 0;
    public readonly long[] capitalMilestones = {
        10_000_000,
        25_000_000,
        50_000_000
    };

    [Header("Time Settings")]
    // 마지막 급여 지급일 (게임 시간 기준)
    public DateTime nextSalaryDate;

    // =========================
    // 업무 효율 (급여 스킬)
    // =========================
    [Header("Work Efficiency (Skill)")]
    [SerializeField] private int salarySkillLevel = 0; // 현재 레벨
    public int SalarySkillLevel => salarySkillLevel;  // 외부 참조용 프로퍼티
    public const int MAX_SALARY_SKILL_LEVEL = 30;

    [Header("Upgrade Cost Settings")]
    public int baseSkillCost = 1_000_000; // 시작가 100만 원
    public float costMultiplier = 1.25f; // 레벨당 비용 상승률

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 만약 바로 주고 싶으면 AddDays(7) 삭제
        if (CoinManager.Instance != null && nextSalaryDate == default) {
            nextSalaryDate = CoinManager.Instance.CurrentDateTime.AddDays(7);
        }

        RecalculateSalary();
    }

    // =========================
    // 회사 자본 관리
    // =========================
    public void AddCapital(long amount) {
        if (amount <= 0) return;

        companyCapital += amount;
        RecalculateSalary();
        CheckCapitalMilestone();
    }

    // =========================
    // 급여 계산 로직
    // =========================
    public long CalculateMonthlySalary() {
        // 자본금의 0.1%를 보너스로 추가
        double capitalBonus = companyCapital * 0.001f;

        // 업무 효율 퍼센트 적용 (1.0 + 효율%)
        float efficiencyMultiplier = 1f + (GetSalaryEfficiencyPercent() / 100f);

        double total = (baseSalary + capitalBonus) * efficiencyMultiplier;

        return (long)total;
    }

    public void RecalculateSalary() {
        currentMonthlySalary = CalculateMonthlySalary();
    }

    // =========================
    // 업무 효율 곡선 (Sqrt 곡선: 초반 상승폭 극대화)
    // =========================
    public float GetSalaryEfficiencyPercent() {
        return CalculateEfficiencyAtLevel(salarySkillLevel);
    }

    // 특정 레벨에서의 효율을 계산하는 내부 함수 (UI 미리보기용으로 활용 가능)
    public float CalculateEfficiencyAtLevel(int level) {
        if (level <= 0) return 0f;

        float progress = (float)level / MAX_SALARY_SKILL_LEVEL;
        float curve = Mathf.Sqrt(progress); // 루트 함수 사용

        return curve * 300f; // 만렙 시 300%
    }

    // =========================
    // 업그레이드 비용 계산
    // =========================
    public int GetSkillUpgradeCost() {
        if (salarySkillLevel >= MAX_SALARY_SKILL_LEVEL)
            return 0;

        // 시작가 100만 원부터 지수적으로 상승
        double cost = baseSkillCost * Math.Pow(costMultiplier, salarySkillLevel);

        // 만 단위에서 깔끔하게 끊기
        return (int)(Math.Floor(cost / 10000) * 10000);
    }

    // =========================
    // 업그레이드 실행부
    // =========================
    public bool CanUpgradeSalarySkill() {
        if (salarySkillLevel >= MAX_SALARY_SKILL_LEVEL)
            return false;

        return PlayerManager.Instance.satoshiBankCash >= GetSkillUpgradeCost();
    }

    public bool TryUpgradeSalarySkill() {
        if (!CanUpgradeSalarySkill())
            return false;

        int cost = GetSkillUpgradeCost();

        // 비용 지불 및 레벨업
        PlayerManager.Instance.satoshiBankCash -= cost;
        salarySkillLevel++;

        RecalculateSalary();

        Debug.Log(
            $"[회사] 업무 효율 강화 성공!\n" +
            $"현재 레벨: Lv.{salarySkillLevel} (보너스: {GetSalaryEfficiencyPercent():F1}%)\n" +
            $"다음 레벨 비용: {GetSkillUpgradeCost():N0}원"
        );

        return true;
    }

    // =========================
    // 급여 지급 시스템
    // =========================
    public float GetSalaryCycleDays() {
        return 7f; // 7일 주기
    }

    public float GetDaysUntilNextSalary() {
        if (CoinManager.Instance == null) return 0f;

        DateTime now = CoinManager.Instance.CurrentDateTime;

        // 현재 시간과 예정일의 차이를 계산
        double remainDays = (nextSalaryDate - now).TotalDays;

        // 음수가 나오면(지급 시간 지남) 0으로 표시
        return Mathf.Max(0f, (float)remainDays);
    }

    public void TryPaySalary() {
        if (CoinManager.Instance == null) return;

        DateTime now = CoinManager.Instance.CurrentDateTime;

        // 예정일이 지났는지 체크 (Date끼리 비교하거나 TotalDays로 체크)
        if (now >= nextSalaryDate) {

            if (currentMonthlySalary > 0) {
                PlayerManager.Instance.satoshiBankCash += currentMonthlySalary;
                DailyIncomeManager.Instance.AddSalary(currentMonthlySalary, "대표 급여");

                Debug.Log($"[회사] 대표 급여 지급 완료: +{currentMonthlySalary:N0}원");
            }

            // 지급된 시간 기준이 아니라 '예정일' 기준으로 7일을 더함
            // 이렇게 해야 밀리지 않고 부동산과 주기가 똑같이 돌아감
            nextSalaryDate = nextSalaryDate.AddDays(7);
        }
    }

    // =========================
    // 자본금 마일스톤 (목표 달성)
    // =========================
    private void CheckCapitalMilestone() {
        while (
            capitalMilestoneIndex < capitalMilestones.Length &&
            companyCapital >= capitalMilestones[capitalMilestoneIndex]
        ) {
            OnCapitalMilestoneReached(capitalMilestoneIndex);
            capitalMilestoneIndex++;
        }
    }

    private void OnCapitalMilestoneReached(int index) {
        switch (index) {
            case 0:
                Debug.Log("자본 목표 1천만 달성: 아르바이트 급여 강화 해금");
                break;
            case 1:
                Debug.Log("자본 목표 2천5백만 달성");
                break;
            case 2:
                Debug.Log("자본 목표 5천만 달성: 선물 거래소 개방");
                break;
        }
    }

    public long GetNextCapitalMilestone() {
        if (capitalMilestoneIndex >= capitalMilestones.Length)
            return -1;

        return capitalMilestones[capitalMilestoneIndex];
    }
}