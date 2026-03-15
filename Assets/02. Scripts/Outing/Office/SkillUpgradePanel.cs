using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillUpgradePanel : MonoBehaviour {

    [Header("Skill 1: Work Efficiency (업무 효율)")]
    public TextMeshProUGUI skillNameLabel;    // "업무 효율 Lv.1"
    public TextMeshProUGUI skillEffectLabel;  // "급여 상승률: 10%"
    public TextMeshProUGUI costLabel;         // "비용: 1,000,000"
    public Button upgradeButton;              // 강화 버튼
    public TextMeshProUGUI buttonText;        // 버튼 텍스트

    // ==========================================
    // [NEW] Skill 2 UI 변수 추가
    // ==========================================
    [Header("Skill 2: Jackpot Chance (대박 성과)")]
    public TextMeshProUGUI skill2NameLabel;   // "대박 성과 Lv.1"
    public TextMeshProUGUI skill2EffectLabel; // "2배 확률: 10%"
    public TextMeshProUGUI skill2CostLabel;   // "비용: 3,000,000"
    public Button skill2UpgradeButton;        // Skill 2 강화 버튼
    public TextMeshProUGUI skill2ButtonText;  // Skill 2 버튼 텍스트

    private OfficePanelController parentController;

    private void Awake() {
        // 메인 화면 갱신을 위해 컨트롤러는 찾아둡니다.
        parentController = FindFirstObjectByType<OfficePanelController>(FindObjectsInactive.Include);
    }

    private void OnEnable() {
        RefreshUI();

        // Skill 1 버튼 이벤트 연결
        if (upgradeButton != null) {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnClickUpgrade);
        }

        // [NEW] Skill 2 버튼 이벤트 연결
        if (skill2UpgradeButton != null) {
            skill2UpgradeButton.onClick.RemoveAllListeners();
            skill2UpgradeButton.onClick.AddListener(OnClickUpgradeSkill2);
        }
    }

    // UI 갱신 (핵심)
    public void RefreshUI() {
        if (OfficeManager.Instance == null) return;
        OfficeManager office = OfficeManager.Instance;

        // 공통 화폐 단위 (원 / Won)
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");

        // ------------------------------------------------
        // 1. Skill 1: 업무 효율
        // ------------------------------------------------
        int lv1 = office.SalarySkillLevel;
        float eff1 = office.GetSalaryEfficiencyPercent();
        int cost1 = office.GetSkillUpgradeCost();
        bool isMax1 = (lv1 >= OfficeManager.MAX_SALARY_SKILL_LEVEL);

        if (skillNameLabel != null)
            skillNameLabel.text = string.Format(LocalizationManager.GetText("LBL_SKILL1_NAME"), lv1);

        if (skillEffectLabel != null)
            skillEffectLabel.text = string.Format(LocalizationManager.GetText("LBL_SKILL1_EFFECT"), eff1.ToString("F1"));

        if (isMax1) {
            if (costLabel != null) costLabel.text = LocalizationManager.GetText("LBL_SKILL_MAX_LV");
            if (buttonText != null) buttonText.text = LocalizationManager.GetText("BTN_SKILL_MAX");
            if (upgradeButton != null) upgradeButton.interactable = false;
        } else {
            if (costLabel != null)
                costLabel.text = string.Format(LocalizationManager.GetText("LBL_SKILL_COST"), cost1.ToString("N0"), unit);
            if (buttonText != null)
                buttonText.text = LocalizationManager.GetText("BTN_SKILL_UPGRADE");
            if (upgradeButton != null) upgradeButton.interactable = true;
        }

        // ------------------------------------------------
        // 2. Skill 2: 대박 성과 (★수정된 부분)
        // ------------------------------------------------
        int lv2 = office.CriticalSkillLevel;
        float chance2 = office.GetCriticalChance();
        long cost2 = office.GetCritUpgradeCost();

        // ★ 중요: 현재 레벨에 맞는 배율(2배, 3배, 4배, 5배)을 가져옵니다.
        long multiplier = office.GetCurrentCritMultiplier();

        bool isMax2 = (lv2 >= OfficeManager.MAX_CRIT_LEVEL);

        if (skill2NameLabel != null)
            skill2NameLabel.text = string.Format(LocalizationManager.GetText("LBL_SKILL2_NAME"), lv2);

        // ★ 텍스트 표시 로직 변경 ("2배" 고정 -> "{multiplier}배" 동적 표시)
        if (skill2EffectLabel != null) {
            string colorCode = "#FFAA00"; // 기본 주황 (2배)
            if (multiplier == 3) colorCode = "#FF5500"; // 진한 주황 (3배)
            if (multiplier == 4) colorCode = "#FF0000"; // 빨강 (4배)
            if (multiplier >= 5) colorCode = "#FF00FF"; // 보라/마젠타 (5배)

            string chanceStr = (chance2 >= 100f) ? "100%" : $"{chance2:F1}%";

            // Format: {0} = 배율, {1} = 색상코드, {2} = 확률문자열
            skill2EffectLabel.text = string.Format(LocalizationManager.GetText("LBL_SKILL2_EFFECT"), multiplier, colorCode, chanceStr);
        }

        if (isMax2) {
            if (skill2CostLabel != null) skill2CostLabel.text = LocalizationManager.GetText("LBL_SKILL_MAX_LV");
            if (skill2ButtonText != null) skill2ButtonText.text = LocalizationManager.GetText("BTN_SKILL_MAX");
            if (skill2UpgradeButton != null) skill2UpgradeButton.interactable = false;
        } else {
            if (skill2CostLabel != null)
                skill2CostLabel.text = string.Format(LocalizationManager.GetText("LBL_SKILL_COST"), cost2.ToString("N0"), unit);
            if (skill2ButtonText != null)
                skill2ButtonText.text = LocalizationManager.GetText("BTN_SKILL_UPGRADE");
            if (skill2UpgradeButton != null) skill2UpgradeButton.interactable = true;
        }
    }

    // Skill 1 강화 버튼 클릭 시
    public void OnClickUpgrade() {
        if (OfficeManager.Instance == null) return;

        // 1. 돈 확인
        if (!OfficeManager.Instance.CanUpgradeSalarySkill()) {
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowConfirm(LocalizationManager.GetText("MSG_INSUFFICIENT_FUNDS"));
            }
            return;
        }

        // 2. 강화 시도
        bool success = OfficeManager.Instance.TryUpgradeSalarySkill();

        if (success) {
            RefreshUI();
            if (parentController != null) parentController.RefreshAll();
        }
    }

    // [NEW] Skill 2 강화 버튼 클릭 시
    public void OnClickUpgradeSkill2() {
        if (OfficeManager.Instance == null) return;

        // 1. 돈 확인 (OfficeManager 비용 계산 함수 호출)
        long cost = OfficeManager.Instance.GetCritUpgradeCost();

        // PlayerManager의 돈을 확인해야 함
        if (PlayerManager.Instance.satoshiBankCash < cost) {
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowConfirm(LocalizationManager.GetText("MSG_INSUFFICIENT_FUNDS"));
            }
            return;
        }

        // 2. 강화 시도
        bool success = OfficeManager.Instance.TryUpgradeCritSkill();

        if (success) {
            // 3. UI 갱신
            RefreshUI();

            // 메인 오피스 정보 갱신
            if (parentController != null) {
                parentController.RefreshAll();
            }
        }
    }
}