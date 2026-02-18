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
    public GameObject glossaryPanel;
    public GameObject questPanel;


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

    public void OpenSkillUpgrade() {
        // ★ [수정] 열려있는 다른 팝업들(납입 패널 등) 먼저 닫기 (중첩 방지)
        CloseAllSubPanels();

        if (homePanel != null) homePanel.SetActive(false);

        if (skillUpgradePanel != null) {
            skillUpgradePanel.SetActive(true);
        }
    }

    // FAQ 내부: 에어드랍 버튼 클릭 시
    public void OpenGlossaryAirdrop() {
        // FAQ 패널 내부 전환이므로 CloseAllSubPanels 호출 안 함
        if (glossaryPanel != null) glossaryPanel.SetActive(false);

    }

    // FAQ 상세에서 다시 FAQ 리스트로
    public void BackToGlossaryMain() {
        if (glossaryPanel != null) glossaryPanel.SetActive(true);
    }

    // 공통 뒤로가기
    public void OnClickBack() {
        ShowMainOffice();
    }

    void CloseAllSubPanels() {
        if (capitalDepositPanel != null) capitalDepositPanel.SetActive(false);
        if (skillUpgradePanel != null) skillUpgradePanel.SetActive(false);
        if (glossaryPanel != null) glossaryPanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
    }

    // =========================
    // 데이터 갱신 (HomePanel)
    // =========================
    public void RefreshHomePanel() {
        if (OfficeManager.Instance == null || PlayerManager.Instance == null) return;

        OfficeManager office = OfficeManager.Instance;

        // 1. 사원 정보
        if (homeNameLabel != null) homeNameLabel.text = $"이름: {PlayerManager.Instance.playerName}";
        if (homeTitleLabel != null) homeTitleLabel.text = $"직급: {PlayerManager.Instance.PlayerTitle}";

        // 2. 급여 정보
        if (homeCurrentSalary != null) homeCurrentSalary.text = $"현재 급여: {office.currentMonthlySalary:N0} 원";
        if (homeSalaryCycle != null) homeSalaryCycle.text = $"급여 지급 주기: {office.GetSalaryCycleDays()}일";
        if (homeNextSalaryDay != null) {
            int dDay = Mathf.CeilToInt(office.GetDaysUntilNextSalary());
            homeNextSalaryDay.text = $"다음 급여일까지: D-{dDay}";
        }

        // 3. 자본 납입 진척도
        long currentCap = office.companyCapital;
        long nextGoal = office.GetNextCapitalMilestone();
        bool isMaxLevel = (nextGoal <= 0);

        if (!isMaxLevel) {
            float progress = (float)currentCap / nextGoal;
            if (homeDepositBar != null) homeDepositBar.fillAmount = progress;
            if (homeDepositPercent != null) homeDepositPercent.text = $"납입율: {progress * 100:F0}%";
            if (homeNextTitleLabel != null) homeNextTitleLabel.text = $"납입 등급: {office.GetCurrentTitle()}";
        } else {
            if (homeDepositBar != null) homeDepositBar.fillAmount = 1f;
            if (homeDepositPercent != null) homeDepositPercent.text = "졸업 (MAX)";
            if (homeNextTitleLabel != null) homeNextTitleLabel.text = "현재 등급: 경제적 자유인";
        }

        // 4. 역량 정보 (Skill 1)
        if (homeSkillLevel != null) homeSkillLevel.text = $"업무 효율 Lv.{office.SalarySkillLevel}";
        if (homeSkillEffect != null) homeSkillEffect.text = $"급여 상승률: {office.GetSalaryEfficiencyPercent():F1}%";

        // ==========================================
        // [NEW] 5. 역량 정보 (Skill 2) - 추가됨
        // ==========================================
        if (homeSkill2Level != null)
            homeSkill2Level.text = $"대박 성과 Lv.{office.CriticalSkillLevel}";

        if (homeSkill2Effect != null) {
            float chance = office.GetCriticalChance();
            long multiplier = office.GetCurrentCritMultiplier();

            // 텍스트 표시: "N배 확률: X%"
            if (chance >= 100f)
                homeSkill2Effect.text = $"{multiplier}배 확률: 100%";
            else
                homeSkill2Effect.text = $"{multiplier}배 확률: {chance:F1}%";
        }

        // 6. 추가 임무
        if (homeQuestText != null) homeQuestText.text = "현재 추가임무 없음";
    }

    // 랜덤 메시지
    void SetWelcomeMessage() {
        if (PlayerManager.Instance == null || welcomeMessage == null) return;

        string pName = PlayerManager.Instance.playerName;
        if (string.IsNullOrWhiteSpace(pName)) pName = "사원";
        string pTitle = PlayerManager.Instance.PlayerTitle;

        string[] quotes = {
            $"어서오세요, {pTitle} {pName}님.\n오늘도 미친듯이 벌어서 자본을 납입하십시오.",
            $"{pTitle} {pName}, 쉬고 있습니까?\n당신이 쉴 때도 이자는 쌓입니다.",
            $"자본주의의 꽃은 납입입니다.\n{pTitle}의 품격에 맞는 자본금을 기대하겠습니다.",
            $"일 하십시오 {pName}.\n회사는 당신을 기억하지 않지만, 납입금은 기억합니다."
        };

        if (OfficeManager.Instance != null && OfficeManager.Instance.currentRankIndex >= 6) {
            quotes = new string[] {
                $"경제적 자유를 얻으셨군요, {pName}님.\n이제 당신이 곧 법입니다.",
                $"더 이상 납입할 곳이 없습니다.\n이제 이 세계를 즐기십시오.",
                $"존경합니다. {pTitle}님.\n하지만 일은 계속 하셔야 합니다?"
            };
        }

        System.Random rnd = new System.Random(System.Guid.NewGuid().GetHashCode());
        welcomeMessage.text = quotes[rnd.Next(quotes.Length)];
    }

    public void RefreshAll() {
        RefreshHomePanel();
    }
}