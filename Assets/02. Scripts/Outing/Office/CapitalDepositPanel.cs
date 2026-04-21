using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CapitalDepositPanel : MonoBehaviour {

    [Header("Input UI")]
    public TMP_InputField inputField;
    public Button depositButton;
    public Button closeButton;

    [Header("Info Display UI")]
    public TextMeshProUGUI availableCashLabel;
    public TextMeshProUGUI currentCapitalLabel;
    public TextMeshProUGUI targetCapitalLabel;
    public TextMeshProUGUI percentLabel;
    public Image progressBar;

    // 납입 등급 표시용 라벨
    public TextMeshProUGUI rankLabel;

    [Header("Rank Visuals (Scroll View)")]
    // 등급 박스 이미지 (0:인턴 ~ 6:경제적 자유인)
    public Image[] rankImages;
    // 화살표 이미지 (0:인턴->주임 ~ 5:이사회->경제적자유인)
    public Image[] rankArrows;

    public Color activeColor = new Color(0, 1, 0, 1);   // 달성 시: 형광 초록
    public Color inactiveColor = new Color(1, 1, 1, 1); // 미달성 시: 흰색

    private OfficePanelController parentController;

    private void Awake() {
        parentController = FindFirstObjectByType<OfficePanelController>(FindObjectsInactive.Include);

        if (inputField != null) {
            inputField.onValueChanged.AddListener(OnInputAmountChanged);
        }
    }

    public void OnInputAmountChanged(string input) {
        if (string.IsNullOrEmpty(input)) return;

        // 1. 콤마 제거 후 숫자로 변환
        string raw = input.Replace(",", "");
        if (!long.TryParse(raw, out long enteredAmount)) return;

        // 2. 제한값(Limit) 계산
        // A: 내 사토시 은행 잔고
        long myCash = (long)PlayerManager.Instance.satoshiBankCash;

        // B: 졸업까지 남은 금액 (최종 목표 - 현재 납입금)
        long finalGoal = 10_000_000_000; // 기본 100억
        if (OfficeManager.Instance.capitalMilestones != null && OfficeManager.Instance.capitalMilestones.Length > 0) {
            finalGoal = OfficeManager.Instance.capitalMilestones[OfficeManager.Instance.capitalMilestones.Length - 1];
        }
        long spaceLeft = finalGoal - OfficeManager.Instance.companyCapital;

        // 3. 둘 중 더 작은 값을 최대 한도로 설정 (내 돈 vs 필요한 돈)
        long maxLimit = Math.Max(0, Math.Min(myCash, spaceLeft));

        // 4. 입력값이 한도를 넘으면 즉시 최대치로 보정
        if (enteredAmount > maxLimit) {
            inputField.text = maxLimit.ToString("N0");
            inputField.caretPosition = inputField.text.Length;
            Debug.Log($"[자본납입] 입력 제한: 최대 {maxLimit:N0}원");
        }
    }

    private void OnEnable() {
        InitPanel();

        depositButton.onClick.RemoveAllListeners();
        depositButton.onClick.AddListener(OnClickDeposit);

        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnClickClose);
        }
    }

    void InitPanel() {
        if (inputField != null) inputField.text = "";
        RefreshUI();
    }

    void RefreshUI() {
        if (PlayerManager.Instance == null || OfficeManager.Instance == null) return;

        OfficeManager office = OfficeManager.Instance;
        long myCash = (long)PlayerManager.Instance.satoshiBankCash;
        long currentCap = office.companyCapital;
        long nextGoal = office.GetNextCapitalMilestone();
        int myRankIndex = office.currentRankIndex;
        string unit = LocalizationManager.GetText("UNIT_CURRENCY"); // 공통 화폐 단위

        // [MAX 체크] 목표 금액이 -1(없음)이나 0 이하로 나오면 최종 단계임
        bool isMaxLevel = (nextGoal <= 0);

        // 1. UI 상태 처리 (진행중 vs 졸업)
        if (isMaxLevel) {
            // == [졸업: 경제적 자유인] ==
            if (rankLabel != null)
                rankLabel.text = LocalizationManager.GetText("LBL_OFFICE_CURRENT_RANK_MAX");

            if (depositButton != null) depositButton.interactable = false;
            if (inputField != null) {
                inputField.text = LocalizationManager.GetText("LBL_DEPOSIT_FREE");
                inputField.interactable = false;
            }

            if (targetCapitalLabel != null)
                targetCapitalLabel.text = LocalizationManager.GetText("LBL_DEPOSIT_ALL_CLEARED");

            if (progressBar != null) progressBar.fillAmount = 0f;
            if (percentLabel != null) percentLabel.text = "MAX";

            myRankIndex = 999; // 시각적 올 클리어 처리
        } else {
            // == [진행 중] ==
            if (rankLabel != null)
                rankLabel.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_DEPOSIT_RANK"), office.GetCurrentTitle());

            if (depositButton != null) depositButton.interactable = true;
            if (inputField != null) inputField.interactable = true;

            if (targetCapitalLabel != null)
                targetCapitalLabel.text = string.Format(LocalizationManager.GetText("LBL_DEPOSIT_TARGET_CASH"), nextGoal.ToString("N0"));

            float progress = (float)currentCap / nextGoal;
            if (progressBar != null) progressBar.fillAmount = progress;
            if (percentLabel != null)
                percentLabel.text = string.Format(LocalizationManager.GetText("LBL_OFFICE_DEPOSIT_RATE"), (progress * 100).ToString("F0"));
        }

        // 2. 등급 박스 색칠
        if (rankImages != null) {
            for (int i = 0; i < rankImages.Length; i++) {
                if (rankImages[i] == null) continue;
                rankImages[i].color = (i <= myRankIndex) ? activeColor : inactiveColor;
            }
        }

        // 3. 화살표 색칠
        if (rankArrows != null) {
            for (int i = 0; i < rankArrows.Length; i++) {
                if (rankArrows[i] == null) continue;
                rankArrows[i].color = (i < myRankIndex) ? activeColor : inactiveColor;
            }
        }

        // 4. 보유 현금 및 현재 납입 금액 갱신
        if (availableCashLabel != null)
            availableCashLabel.text = string.Format(LocalizationManager.GetText("LBL_BANK_CASH_DISPLAY"), myCash.ToString("N0"), unit);
        if (currentCapitalLabel != null)
            currentCapitalLabel.text = string.Format(LocalizationManager.GetText("LBL_DEPOSIT_CURRENT_CAP"), currentCap.ToString("N0"));
    }

    void OnClickDeposit() {
        if (inputField == null) return;
        if (OfficeManager.Instance == null) return;

        string rawText = inputField.text.Replace(",", "");

        if (!long.TryParse(rawText, out long amount)) {
            Debug.LogWarning("숫자를 입력해주세요.");
            return;
        }

        if (amount <= 0) return;

        // 1. 최종 목표 금액 (100억) 가져오기
        long finalGoal = 10_000_000_000;
        if (OfficeManager.Instance.capitalMilestones != null && OfficeManager.Instance.capitalMilestones.Length > 0) {
            finalGoal = OfficeManager.Instance.capitalMilestones[OfficeManager.Instance.capitalMilestones.Length - 1];
        }

        // 2. 남은 공간 계산 (100억 - 현재 납입금)
        long currentCap = OfficeManager.Instance.companyCapital;
        long spaceLeft = finalGoal - currentCap;

        // 이미 꽉 찼으면 종료
        if (spaceLeft <= 0) return;

        // 3. [핵심] 입력 금액이 남은 공간보다 크면, 남은 공간만큼만 넣게 자름 (Clamping)
        if (amount > spaceLeft) {
            amount = spaceLeft;
            Debug.Log($"[납입] 초과 금액 절사됨. 실제 납입액: {amount:N0}");
        }

        // 4. 내 지갑 잔액 체크 (잘린 amount 기준으로 체크)
        if (amount > PlayerManager.Instance.satoshiBankCash) {
            Debug.LogWarning("잔액이 부족합니다.");
            return;
        }

        // 5. 실제 처리
        PlayerManager.Instance.satoshiBankCash -= amount;
        OfficeManager.Instance.AddCapital(amount);

        // [수정] 거래 내역 현지화
        string logDesc = LocalizationManager.GetText("LOG_CAPITAL_INJECT");
        string logType = LocalizationManager.GetText("LOG_WITHDRAW");
        string logAsset = LocalizationManager.GetText("LOG_SATOSHI_CASH");

        TransactionManager.Instance.AddRecord(logDesc, amount, logType, logAsset);

        RefreshUI();
        inputField.text = ""; // 입력창 비움

        if (parentController != null) {
            parentController.RefreshAll();
        }
    }

    void OnClickClose() {
        if (parentController != null) {
            parentController.ShowMainOffice();
        } else {
            gameObject.SetActive(false);
        }
    }
}