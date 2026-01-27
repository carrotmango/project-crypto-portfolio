using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OfficePanelController : MonoBehaviour {

    [Header("Main Panels")]
    public GameObject officeMainPanel;    // 배경 및 기본 UI
    public GameObject officeMainButton;   // Hierarchy의 'Buttons' (메인 버튼 5개 그룹)
    public GameObject statusPanel;        // Hierarchy의 'UserStatusPanel'
    public GameObject capitalDepositPanel;
    public GameObject skillUpgradePanel;
    public GameObject glossaryPanel;      // Hierarchy의 'GlossaryPanel' (에어드랍 버튼 등이 있는 곳)
    public GameObject dialoguePanel;
    public GameObject messageLogPanel;

    [Header("FAQ Sub Content")]
    public GameObject glossariesParent;   // Hierarchy의 'Glossaries' (상세 내용들의 부모)
    public GameObject airdropContent;     // Hierarchy의 'Glossaries -> Airdrops'

    [Header("User Info UI")]
    public TextMeshProUGUI userNameLabel;
    public TextMeshProUGUI bankCashLabel;
    public TextMeshProUGUI capitalAmountLabel;

    [Header("Capital & Salary Goal")]
    public TextMeshProUGUI nextCapitalGoalLabel;
    public TextMeshProUGUI nextCapitalRewardLabel;
    public TextMeshProUGUI salaryInfoLabel;
    public TextMeshProUGUI nextSalaryLabel;

    [Header("Work Efficiency Skill")]
    public TextMeshProUGUI salarySkillLevelLabel;
    public TextMeshProUGUI salarySkillEffectLabel;
    public TextMeshProUGUI salarySkillCostLabel;
    public Button salarySkillUpgradeButton;

    [Header("Etc Panels")]
    public GameObject DepositPanel;

    private int lastShownSurvivalDay = -1;

    private void OnEnable() {
        // 패널이 활성화될 때(탭 전환 등) 무조건 초기 메인 상태로 리셋
        ShowMainOffice();
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

    // =========================
    // 패널 내비게이션 시스템
    // =========================

    // 모든 서브 창을 끄고 사무실 초기 버튼(Buttons) 상태로 복구
    public void ShowMainOffice() {
        // 1. 모든 상세/서브 패널 비활성화
        if (statusPanel != null) statusPanel.SetActive(false);
        if (skillUpgradePanel != null) skillUpgradePanel.SetActive(false);
        if (glossaryPanel != null) glossaryPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (messageLogPanel != null) messageLogPanel.SetActive(false);
        if(capitalDepositPanel != null) capitalDepositPanel.SetActive(false);

        // Glossary 관련 상세 내용들도 전부 끔
        if (glossariesParent != null) glossariesParent.SetActive(false);
        if (airdropContent != null) airdropContent.SetActive(false);

        // 2. 오피스 메인 요소 활성화 (버튼 소생 포인트)
        if (officeMainPanel != null) officeMainPanel.SetActive(true);
        if (officeMainButton != null) officeMainButton.SetActive(true);
    }

    // 상태창(UserStatusPanel) 오픈
    public void OpenStatusPanel() {
        if (officeMainButton != null) officeMainButton.SetActive(false); // 메인 버튼 숨기기
        if (statusPanel != null) {
            statusPanel.SetActive(true);
            statusPanel.GetComponent<StatusPanelController>()?.UpdateAssetFromStatus();
        }
    }

    // FAQ(GlossaryPanel) 오픈
    public void OpenGlossary() {
        if (officeMainButton != null) officeMainButton.SetActive(false); // 메인 버튼 숨기기
        if (glossaryPanel != null) {
            glossaryPanel.SetActive(true);
        }
    }

    public void OpenCapitalDeposit() {
        if (officeMainButton != null) officeMainButton.SetActive(false); // 메인 버튼 숨기기
        if (capitalDepositPanel != null) {
            capitalDepositPanel.SetActive(true);
        }
    }

    public void OpenSkillUpgrade() {
        if (officeMainButton != null) officeMainButton.SetActive(false); // 메인 버튼 숨기기
        if (skillUpgradePanel != null) {
            skillUpgradePanel.SetActive(true);
        }
    }

    // FAQ 내부: 에어드랍 버튼 클릭 시
    public void OpenGlossaryAirdrop() {
        // FAQ 메인 리스트 끄기
        if (glossaryPanel != null) glossaryPanel.SetActive(false);

        // Glossaries 부모와 Airdrops 상세 내용 켜기
        if (glossariesParent != null) glossariesParent.SetActive(true);
        if (airdropContent != null) airdropContent.SetActive(true);
    }

    // FAQ 상세(에어드랍 등)에서 다시 FAQ 리스트(GlossaryPanel)로 돌아가기
    public void BackToGlossaryMain() {
        if (airdropContent != null) airdropContent.SetActive(false);
        if (glossariesParent != null) glossariesParent.SetActive(false);

        if (glossaryPanel != null) glossaryPanel.SetActive(true);
    }

    // 공통 뒤로가기 (상태창, FAQ 메인 등에서 오피스 첫 화면으로 복귀)
    public void OnClickBack() {
        ShowMainOffice();
    }

    // =========================
    // 데이터 갱신 로직 (기존 유지)
    // =========================

    public void RefreshAll() {
        if (statusPanel != null && statusPanel.activeSelf) {
            statusPanel.GetComponent<StatusPanelController>()?.UpdateAssetFromStatus();
        }
        RefreshName();
        RefreshCapital();
        RefreshNextCapitalGoal();
        RefreshSalaryInfo();
        RefreshBankCash();
    }

    void SyncDay() {
        if (CoinManager.Instance != null) lastShownSurvivalDay = CoinManager.Instance.survivalDays;
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
        long salary = office.currentMonthlySalary;
        float cycleDays = office.GetSalaryCycleDays();
        float remainDaysRaw = office.GetDaysUntilNextSalary();
        int remainDaysInt = Mathf.CeilToInt(remainDaysRaw);
        if (remainDaysInt == 0 && remainDaysRaw > 0f) remainDaysInt = 1;
        salaryInfoLabel.text = $"현재 급여: \n {salary:N0}원 / {cycleDays:0.0}d";
        nextSalaryLabel.text = $"다음 급여까지: {remainDaysInt}일";
        RefreshSalarySkillUI();
    }

    void RefreshSalarySkillUI() {
        if (OfficeManager.Instance == null) return;
        OfficeManager office = OfficeManager.Instance;
        int level = office.SalarySkillLevel;
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

    public void OpenDepositPanel() {
        if (DepositPanel != null) DepositPanel.SetActive(true);
    }
}