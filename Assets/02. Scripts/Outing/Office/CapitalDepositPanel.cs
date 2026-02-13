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

        // [MAX 체크] 목표 금액이 -1(없음)이나 0 이하로 나오면 최종 단계임
        bool isMaxLevel = (nextGoal <= 0);

        // 1. UI 상태 처리 (진행중 vs 졸업)
        if (isMaxLevel) {
            // == [졸업: 경제적 자유인] ==
            if (rankLabel != null) rankLabel.text = "현재 등급: 경제적 자유인";

            if (depositButton != null) depositButton.interactable = false;
            if (inputField != null) {
                inputField.text = "자유를 얻었습니다.";
                inputField.interactable = false;
            }

            if (targetCapitalLabel != null) targetCapitalLabel.text = "모든 목표 달성";
            if (progressBar != null) progressBar.fillAmount = 0f;
            if (percentLabel != null) percentLabel.text = "MAX";

            myRankIndex = 999; // 시각적 올 클리어 처리
        } else {
            // == [진행 중] ==
            if (rankLabel != null) rankLabel.text = $"현재 등급: {office.GetCurrentTitle()}";

            if (depositButton != null) depositButton.interactable = true;
            if (inputField != null) inputField.interactable = true;

            if (targetCapitalLabel != null) targetCapitalLabel.text = $"목표금액: {nextGoal:N0}";

            float progress = (float)currentCap / nextGoal;
            if (progressBar != null) progressBar.fillAmount = progress;
            if (percentLabel != null) percentLabel.text = $"납입율: {progress * 100:F0}%";
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

        if (availableCashLabel != null) availableCashLabel.text = $"가능: {myCash:N0}원";
        if (currentCapitalLabel != null) currentCapitalLabel.text = $"납입금액: {currentCap:N0}";
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
        // (안전하게 매니저 배열의 마지막 값을 가져옵니다)
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
            // (선택사항) 여기서도 잔액만큼만 최대로 넣게 하려면: amount = (long)PlayerManager.Instance.satoshiBankCash;
            return;
        }

        // 5. 실제 처리
        PlayerManager.Instance.satoshiBankCash -= amount;
        OfficeManager.Instance.AddCapital(amount);

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