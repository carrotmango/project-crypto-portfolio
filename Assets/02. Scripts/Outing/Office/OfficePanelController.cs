using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class OfficePanelController : MonoBehaviour {

    [Header("Main Panels")]
    public GameObject officeMainPanel;
    public GameObject officeMainButton;

    [Header("Sub Panels")]
    public GameObject homePanel;
    public GameObject capitalDepositPanel;
    public GameObject skillUpgradePanel;
    public GameObject questPanel;
    public GameObject researchPanel;


    [Header("--- HomePanel UI Bindings ---")]
    public TextMeshProUGUI homeNameLabel;
    public TextMeshProUGUI homeTitleLabel;
    public TextMeshProUGUI homeCurrentSalary;
    public TextMeshProUGUI homeSalaryCycle;
    public TextMeshProUGUI homeNextSalaryDay;
    public TextMeshProUGUI homeNextTitleLabel;
    public Image homeDepositBar;
    public TextMeshProUGUI homeDepositPercent;
    public TextMeshProUGUI homeQuestText;
    public TextMeshProUGUI welcomeMessage;

    [Header("Skill 1 Info")]
    public TextMeshProUGUI homeSkillLevel;  // 업무 효율 레벨
    public TextMeshProUGUI homeSkillEffect; // 업무 효율 효과

    // ==========================================
    // [NEW] Skill 2 정보 표시용 변수
    // ==========================================
    [Header("Skill 2 Info (New)")]
    public TextMeshProUGUI homeSkill2Level;  // "대박 성과 Lv.N"
    public TextMeshProUGUI homeSkill2Effect; // "N배 확률: X%"

    private int lastShownSurvivalDay = -1;

    [Header("Quest Notification")]
    public GameObject questNotificationBadge; // 초록색 원형 이미지 (GameObject)
    public TextMeshProUGUI questNotificationCountText; // 숫자 텍스트

    // 이 함수를 호출해서 배지를 껐다 켜거나 숫자를 갱신합니다.
    public void UpdateQuestBadge(int count) {
        if (questNotificationBadge == null) return;

        if (count > 0) {
            questNotificationBadge.SetActive(true);
            if (questNotificationCountText != null) {
                questNotificationCountText.text = count.ToString();
            }
        } else {
            questNotificationBadge.SetActive(false);
        }
    }

    private void OnEnable() {
        if (PlayerManager.Instance != null) {
            PlayerManager.Instance.OnPlayerNameChanged += RefreshHomePanel;
        }
        ShowMainOffice();
    }

    private void OnDisable() {
        if (PlayerManager.Instance != null) {
            PlayerManager.Instance.OnPlayerNameChanged -= RefreshHomePanel;
        }
    }

    private void Start() {
        ShowMainOffice();
    }

    private void Update() {
        if (CoinManager.Instance == null) return;

        int currentDay = CoinManager.Instance.survivalDays;
        if (currentDay != lastShownSurvivalDay) {
            lastShownSurvivalDay = currentDay;
            RefreshHomePanel();
        }
    }

    public void EnterOffice() {
        ShowMainOffice();
    }

    // =========================
    // 패널 내비게이션 (수정됨)
    // =========================
    public void ShowMainOffice() {
        CloseAllSubPanels(); // 모든 서브 패널 닫기

        if (homePanel != null) homePanel.SetActive(true);
        if (officeMainButton != null) officeMainButton.SetActive(true);

        RefreshHomePanel();
        SetWelcomeMessage();
    }

    public void OpenCapitalDeposit() {
        CloseAllSubPanels();

        if (homePanel != null) homePanel.SetActive(false);
        if (capitalDepositPanel != null) {
            capitalDepositPanel.SetActive(true);
        }
    }

    public void OpenQuestPanel() {
        CloseAllSubPanels();

        if (homePanel != null) homePanel.SetActive(false);

        if (questPanel != null) {
            questPanel.SetActive(true);


        }
    }

    public void OpenResearchPanel() {
        CloseAllSubPanels();

        if (homePanel != null) homePanel.SetActive(false);

        if (researchPanel != null) {
            researchPanel.SetActive(true);

            // ★ [추가] 리서치 패널 컨트롤러를 찾아서 '메인(뉴스)' 탭을 강제로 열게 합니다.
            var researchCtrl = researchPanel.GetComponent<ResearchPanelController>();
            if (researchCtrl != null) {
                researchCtrl.ShowMainPanel();
            }
        }
    }

    public void OpenSkillUpgrade() {
        // ★ [수정] 열려있는 다른 팝업들(납입 패널 등) 먼저 닫기 (중첩 방지)
        CloseAllSubPanels();

        if (homePanel != null) homePanel.SetActive(false);

        if (skillUpgradePanel != null) {
            skillUpgradePanel.SetActive(true);
        }
    }


    // 공통 뒤로가기
    public void OnClickBack() {
        ShowMainOffice();
    }

    void CloseAllSubPanels() {
        if (capitalDepositPanel != null) capitalDepositPanel.SetActive(false);
        if (skillUpgradePanel != null) skillUpgradePanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
        if (researchPanel != null) researchPanel.SetActive(false);
    }

    // =========================
    // 데이터 갱신 (HomePanel)
    // =========================
    public void RefreshHomePanel() {
        if (OfficeManager.Instance == null || PlayerManager.Instance == null) return;

        OfficeManager office = OfficeManager.Instance;
        string unit = LocalizationManager.GetText("UNIT_CURRENCY"); // 공통 화폐 단위 (Won)

        // 1. 사원 정보
        if (homeNameLabel != null)
            homeNameLabel.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_NAME"), PlayerManager.Instance.playerName);
        if (homeTitleLabel != null)
            homeTitleLabel.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_TITLE"), PlayerManager.Instance.PlayerTitle);

        // 2. 급여 정보
        if (homeCurrentSalary != null)
            homeCurrentSalary.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_SALARY"), office.currentMonthlySalary.ToString("N0"), unit);
        if (homeSalaryCycle != null)
            homeSalaryCycle.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_SALARY_CYCLE"), office.GetSalaryCycleDays());

        if (homeNextSalaryDay != null) {
            int dDay = Mathf.CeilToInt(office.GetDaysUntilNextSalary());
            homeNextSalaryDay.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_NEXT_SALARY"), dDay);
        }

        // 3. 자본 납입 진척도
        long currentCap = office.companyCapital;
        long nextGoal = office.GetNextCapitalMilestone();
        bool isMaxLevel = (nextGoal <= 0);

        if (!isMaxLevel) {
            float progress = (float)currentCap / nextGoal;
            if (homeDepositBar != null) homeDepositBar.fillAmount = progress;
            if (homeDepositPercent != null)
                homeDepositPercent.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_DEPOSIT_RATE"), (progress * 100).ToString("F0"));
            if (homeNextTitleLabel != null)
                homeNextTitleLabel.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_DEPOSIT_RANK"), office.GetCurrentTitle());
        } else {
            if (homeDepositBar != null) homeDepositBar.fillAmount = 1f;
            if (homeDepositPercent != null)
                homeDepositPercent.text = LocalizationManager.GetText("LBL_OFFICE_MAX_GRAD");
            if (homeNextTitleLabel != null)
                homeNextTitleLabel.text = LocalizationManager.GetText("LBL_OFFICE_CURRENT_RANK_MAX");
        }

        // 4. 역량 정보 (Skill 1)
        if (homeSkillLevel != null)
            homeSkillLevel.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_SKILL1_LV"), office.SalarySkillLevel);
        if (homeSkillEffect != null)
            homeSkillEffect.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_SKILL1_EFF"), office.GetSalaryEfficiencyPercent().ToString("F1"));

        // 5. 역량 정보 (Skill 2)
        if (homeSkill2Level != null)
            homeSkill2Level.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_SKILL2_LV"), office.CriticalSkillLevel);

        if (homeSkill2Effect != null) {
            float chance = office.GetCriticalChance();
            long multiplier = office.GetCurrentCritMultiplier();
            string chanceStr = (chance >= 100f) ? "100%" : $"{chance:F1}%";

            homeSkill2Effect.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_SKILL2_EFF"), multiplier, chanceStr);
        }

        // 6. 추가 임무
        if (homeQuestText != null)
            homeQuestText.text = LocalizationManager.GetText("LBL_OFFICE_NO_QUEST");
    }

    // 랜덤 메시지
    void SetWelcomeMessage() {
        if (PlayerManager.Instance == null || welcomeMessage == null) return;

        string pName = PlayerManager.Instance.playerName;
        if (string.IsNullOrWhiteSpace(pName))
            pName = LocalizationManager.GetText("LBL_OFFICE_DEFAULT_EMP");

        string pTitle = PlayerManager.Instance.PlayerTitle;
        string[] quotes;

        if (OfficeManager.Instance != null && OfficeManager.Instance.currentRankIndex >= 6) {
            // 만렙 멘트
            quotes = new string[] {
                string.Format(LocalizationManager.GetText("QUOTE_MAX_1"), pName),
                LocalizationManager.GetText("QUOTE_MAX_2"),
                string.Format(LocalizationManager.GetText("QUOTE_MAX_3"), pTitle)
            };
        } else {
            // 일반 멘트
            quotes = new string[] {
                string.Format(LocalizationManager.GetText("QUOTE_NORMAL_1"), pTitle, pName),
                string.Format(LocalizationManager.GetText("QUOTE_NORMAL_2"), pTitle, pName),
                string.Format(LocalizationManager.GetText("QUOTE_NORMAL_3"), pTitle),
                string.Format(LocalizationManager.GetText("QUOTE_NORMAL_4"), pName)
            };
        }

        System.Random rnd = new System.Random(System.Guid.NewGuid().GetHashCode());
        welcomeMessage.text = quotes[rnd.Next(quotes.Length)];
    }

    public void RefreshAll() {
        RefreshHomePanel();
    }
}