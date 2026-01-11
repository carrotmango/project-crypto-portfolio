using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OfficePanelController : MonoBehaviour {

    [Header("Main Panels")]
    public GameObject officeMainPanel;    // 버튼 5개가 있는 첫 화면
    public GameObject statusPanel;        // 상세 상태창 패널
    public GameObject skillUpgradePanel;  // 스킬 강화 패널
    public GameObject glossaryPanel;      // 용어 정리 패널
    public GameObject dialoguePanel;      // 대화 패널
    public GameObject messageLogPanel;    // 메시지 로그 패널

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
        ShowMainOffice(); // 켜질 때 항상 메인 버튼 화면으로 초기화
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

    // =========================
    // 패널 전환 시스템 (Navigation)
    // =========================

    // 모든 서브 패널을 끄고 메인 화면만 보여주는 함수
    public void ShowMainOffice() {
        if (officeMainPanel != null) officeMainPanel.SetActive(true);

        if (statusPanel != null) statusPanel.SetActive(false);
        if (skillUpgradePanel != null) skillUpgradePanel.SetActive(false);
        if (glossaryPanel != null) glossaryPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (messageLogPanel != null) messageLogPanel.SetActive(false);
    }

    // 상태창 열기 (상태 버튼에 연결)
    public void OpenStatusPanel() {
        if (officeMainPanel != null)
        if (statusPanel != null) {
            statusPanel.SetActive(true);
            // StatusPanelController가 있다면 데이터 갱신 호출
            statusPanel.GetComponent<StatusPanelController>()?.UpdateAssetFromStatus();
        }
    }

    // 상태창에서 뒤로가기 버튼 (뒤로 버튼에 연결)
    public void OnClickStatusBack() {
        ShowMainOffice();
    }

    // 기타 패널 오픈 함수들
    public void OpenSkillUpgrade() { if (officeMainPanel != null) officeMainPanel.SetActive(false); skillUpgradePanel?.SetActive(true); }
    public void OpenGlossary() { if (officeMainPanel != null) officeMainPanel.SetActive(false); glossaryPanel?.SetActive(true); }
    public void OpenDialogue() { if (officeMainPanel != null) officeMainPanel.SetActive(false); dialoguePanel?.SetActive(true); }
    public void OpenMessageLog() { if (officeMainPanel != null) officeMainPanel.SetActive(false); messageLogPanel?.SetActive(true); }


    // =========================
    // 데이터 리프레시 로직 (기존 유지)
    // =========================

    void SyncDay() {
        if (CoinManager.Instance != null) {
            lastShownSurvivalDay = CoinManager.Instance.survivalDays;
        }
    }

    public void RefreshAll() {
        // StatusPanelController가 별도 관할이므로 있으면 호출만 해줌
        if (statusPanel != null && statusPanel.activeSelf) {
            statusPanel.GetComponent<StatusPanelController>()?.UpdateAssetFromStatus();
        }

        RefreshName();
        RefreshCapital();
        RefreshNextCapitalGoal();
        RefreshSalaryInfo();
        RefreshBankCash();
    }

    void RefreshBankCash() {
        if (bankCashLabel == null || PlayerManager.Instance == null) return;
        bankCashLabel.text = $"은행 잔액: {PlayerManager.Instance.satoshiBankCash:N0}원";
    }

    void RefreshName() {
        if (userNameLabel == null || PlayerManager.Instance == null) return;
        userNameLabel.text = "";
    }

    void RefreshCapital() {
        if (capitalAmountLabel == null || OfficeManager.Instance == null) return;
        capitalAmountLabel.text = $"{OfficeManager.Instance.companyCapital:N0}원";
    }

    public void OpenDepositPanel() {
        if (DepositPanel != null) {
            DepositPanel.SetActive(true);
        }
    }

    void RefreshNextCapitalGoal() {
        if (OfficeManager.Instance == null || nextCapitalGoalLabel == null || nextCapitalRewardLabel == null) return;

        OfficeManager office = OfficeManager.Instance;
        long nextGoal = office.GetNextCapitalMilestone();

        if (nextGoal < 0) {
            nextCapitalGoalLabel.text = "모든 자본 목표 달성";
            nextCapitalRewardLabel.text = "-";
            return;
        }

        nextCapitalGoalLabel.text = $"{nextGoal:N0}원";
        nextCapitalRewardLabel.text = GetCapitalRewardName(office.capitalMilestoneIndex);
    }

    string GetCapitalRewardName(int index) {
        switch (index) {
            case 0: return "보상: 아르바이트 급여 강화";
            case 1: return "보상: 미정";
            case 2: return "보상: 선물 거래소 개방";
            default: return "-";
        }
    }

    void RefreshSalaryInfo() {
        if (OfficeManager.Instance == null || salaryInfoLabel == null || nextSalaryLabel == null) return;

        OfficeManager office = OfficeManager.Instance;
        int salary = office.currentMonthlySalary;
        float cycleDays = office.GetSalaryCycleDays();
        float remainDaysRaw = office.GetDaysUntilNextSalary();
        int remainDaysInt = Mathf.CeilToInt(remainDaysRaw);

        if (remainDaysInt == 0 && remainDaysRaw > 0f) remainDaysInt = 1;

        salaryInfoLabel.text = $"당신의 급여: {salary:N0}원 / {cycleDays:0.0}d";
        nextSalaryLabel.text = $"다음 급여까지: {remainDaysInt}일";

        RefreshSalarySkillUI();
    }

    void RefreshSalarySkillUI() {
        if (OfficeManager.Instance == null) return;

        OfficeManager office = OfficeManager.Instance;
        int level = office.salarySkillLevel;
        float percent = office.GetSalaryEfficiencyPercent();

        if (salarySkillLevelLabel != null) salarySkillLevelLabel.text = $"업무 효율 Lv{level}";
        if (salarySkillEffectLabel != null) salarySkillEffectLabel.text = $"급여 지급 금액 상승: {percent:0.#}%";

        if (salarySkillCostLabel != null) {
            if (level >= OfficeManager.MAX_SALARY_SKILL_LEVEL) salarySkillCostLabel.text = "비용: MAX";
            else salarySkillCostLabel.text = $"비용: {office.GetSkillUpgradeCost():N0}원";
        }

        if (salarySkillUpgradeButton != null) salarySkillUpgradeButton.interactable = office.CanUpgradeSalarySkill();
    }

    public void OnClickUpgradeSalarySkill() {
        if (OfficeManager.Instance == null) return;
        OfficeManager.Instance.TryUpgradeSalarySkill();
        RefreshAll();
    }
}