using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OfficePanelController : MonoBehaviour {

    [Header("User")]
    public TextMeshProUGUI userNameLabel;

    [Header("Capital")]
    public TextMeshProUGUI capitalAmountLabel;
    public TextMeshProUGUI nextCapitalGoalLabel;
    public TextMeshProUGUI nextCapitalRewardLabel;

    [Header("Salary")]
    public TextMeshProUGUI salaryInfoLabel;
    public TextMeshProUGUI nextSalaryLabel;

    [Header("Panel")]
    public GameObject DepositPanel;

    [Header("Bank")]
    public TextMeshProUGUI bankCashLabel;


    // 하루 변경 감지용
    private int lastShownSurvivalDay = -1;

    // =========================
    // 업무 효율 UI
    // =========================
    [Header("Work Efficiency")]
    public TextMeshProUGUI salarySkillLevelLabel;
    public TextMeshProUGUI salarySkillEffectLabel;
    public TextMeshProUGUI salarySkillCostLabel;
    public Button salarySkillUpgradeButton;

    private void OnEnable() {
        RefreshAll();
        SyncDay();
    }

    private void Start() {
        RefreshAll();
        SyncDay();
    }

    private void Update() {
        if (CoinManager.Instance == null) return;

        int currentDay = CoinManager.Instance.survivalDays;
        if (currentDay != lastShownSurvivalDay) {
            lastShownSurvivalDay = currentDay;
            RefreshSalaryInfo();
        }
    }

    void SyncDay() {
        if (CoinManager.Instance != null) {
            lastShownSurvivalDay = CoinManager.Instance.survivalDays;
        }
    }

    public void RefreshAll() {
        RefreshName();
        RefreshCapital();
        RefreshNextCapitalGoal();
        RefreshSalaryInfo();
        RefreshBankCash();
    }

    void RefreshBankCash() {
        if (bankCashLabel == null || PlayerManager.Instance == null) return;

        bankCashLabel.text =
            $"은행 잔액: {PlayerManager.Instance.satoshiBankCash:N0}원";
    }


    // =========================
    // 기본 정보
    // =========================
    void RefreshName() {
        if (userNameLabel == null || PlayerManager.Instance == null) return;

        //userNameLabel.text =
        //    $"안녕하세요, {PlayerManager.Instance.playerName}님";
        userNameLabel.text =
    $"";
    }

    void RefreshCapital() {
        if (capitalAmountLabel == null || OfficeManager.Instance == null) return;

        capitalAmountLabel.text =
            $"{OfficeManager.Instance.companyCapital:N0}원";
    }

    public void OpenDepositPanel() {
        if (DepositPanel != null) {
            DepositPanel.SetActive(true);
        }
    }

    // =========================
    // 자본 목표
    // =========================
    void RefreshNextCapitalGoal() {
        if (OfficeManager.Instance == null ||
            nextCapitalGoalLabel == null ||
            nextCapitalRewardLabel == null) {
            return;
        }

        OfficeManager office = OfficeManager.Instance;
        long nextGoal = office.GetNextCapitalMilestone();

        if (nextGoal < 0) {
            nextCapitalGoalLabel.text = "모든 자본 목표 달성";
            nextCapitalRewardLabel.text = "-";
            return;
        }

        nextCapitalGoalLabel.text = $"{nextGoal:N0}원";
        nextCapitalRewardLabel.text =
            GetCapitalRewardName(office.capitalMilestoneIndex);
    }

    string GetCapitalRewardName(int index) {
        switch (index) {
            case 0: return "보상: 아르바이트 급여 강화";
            case 1: return "보상: 미정";
            case 2: return "보상: 선물 거래소 개방";
            default: return "-";
        }
    }

    // =========================
    // 급여 정보
    // =========================
    void RefreshSalaryInfo() {
        if (OfficeManager.Instance == null ||
            salaryInfoLabel == null ||
            nextSalaryLabel == null) {
            return;
        }

        OfficeManager office = OfficeManager.Instance;

        int salary = office.currentMonthlySalary;
        float cycleDays = office.GetSalaryCycleDays();

        float remainDaysRaw = office.GetDaysUntilNextSalary();
        int remainDaysInt = Mathf.CeilToInt(remainDaysRaw);

        if (remainDaysInt == 0 && remainDaysRaw > 0f) {
            remainDaysInt = 1;
        }

        salaryInfoLabel.text =
            $"당신의 급여: {salary:N0}원 / {cycleDays:0.0}d";

        nextSalaryLabel.text =
            $"다음 급여까지: {remainDaysInt}일";

        RefreshSalarySkillUI();
    }

    // =========================
    // 업무 효율 UI
    // =========================
    void RefreshSalarySkillUI() {
        if (OfficeManager.Instance == null) return;

        OfficeManager office = OfficeManager.Instance;

        int level = office.salarySkillLevel;
        float percent = office.GetSalaryEfficiencyPercent();

        if (salarySkillLevelLabel != null) {
            salarySkillLevelLabel.text = $"업무 효율 Lv{level}";
        }

        if (salarySkillEffectLabel != null) {
            salarySkillEffectLabel.text =
                $"급여 지급 금액 상승: {percent:0.#}%";
        }

        if (salarySkillCostLabel != null) {
            if (level >= OfficeManager.MAX_SALARY_SKILL_LEVEL) {
                salarySkillCostLabel.text = "비용: MAX";
            } else {
                salarySkillCostLabel.text =
                    $"비용: {office.GetSkillUpgradeCost():N0}원";
            }
        }

        if (salarySkillUpgradeButton != null) {
            salarySkillUpgradeButton.interactable =
                office.CanUpgradeSalarySkill();
        }
    }

    // =========================
    // 버튼
    // =========================
    public void OnClickUpgradeSalarySkill() {
        if (OfficeManager.Instance == null) return;

        OfficeManager.Instance.TryUpgradeSalarySkill();
        RefreshAll();
    }
}
