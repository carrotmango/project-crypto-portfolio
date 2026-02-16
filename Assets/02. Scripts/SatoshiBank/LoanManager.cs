using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public class LoanManager : MonoBehaviour {
    public static LoanManager Instance;

    [Header("Loan State")]
    public double currentLoanPrincipal = 0; // 내부 관리용 원금
    public DateTime nextPaymentDate;        // 다음 이자 징수일
    public int overdueCount = 0;            // 연체 횟수
    public bool hasPaidFirstInterest = false; // 최소 1회 이자 납부 여부

    [Header("UI State")]
    private bool isPanelOpen = false;

    [Header("UI Reference")]
    public GameObject loanRequestGroup;
    public GameObject loanRepayGroup;
    public TMP_InputField repayInputField;

    [Header("Text UI")]
    [SerializeField] private TextMeshProUGUI requestDescriptionText;
    [SerializeField] private TextMeshProUGUI currentDebtText; // "현재 남은 대출금" 표시용

    [Header("MAIN LOAN UI")]
    public Button mainLoanOpenButton;
    public Image targetImage0;
    public Image targetImage30;
    public TextMeshProUGUI targetText30;

    private void Awake() {
        if (Instance == null) Instance = this;
    }

    public long GetMaxLoanLimit() {
        long baseLimit = 3_000_000;
        float ltvRatio = 0.6f + (OfficeManager.Instance.currentRankIndex * 0.05f);
        ltvRatio = Mathf.Min(ltvRatio, 0.8f);

        long capital = OfficeManager.Instance.companyCapital;
        return baseLimit + (long)(capital * ltvRatio);
    }

    public float GetCurrentAnnualRate() {
        float marketRate = (float)GlobalEconomyManager.BaseInterestRate;
        float defaultSpread = 10.0f;
        float rankDiscount = OfficeManager.Instance.currentRankIndex * 0.5f;

        return marketRate + defaultSpread - rankDiscount;
    }

    // ★ [추가] 현재 갚아야 할 총 대출금(원금 + 필요 시 1달치 이자) 계산
    public long GetTotalDebtAmount() {
        if (currentLoanPrincipal <= 0) return 0;

        long total = (long)currentLoanPrincipal;

        // 아직 이자를 한 번도 안 냈다면 최소 1달치 이자 합산
        if (!hasPaidFirstInterest) {
            double monthlyRate = GetCurrentAnnualRate() / 12.0 / 100.0;
            total += (long)(currentLoanPrincipal * monthlyRate);
        }

        return total;
    }

    public void OpenLoanPanel() {
        RefreshLoanUI(true);
    }

    public void OnApproveLoan() {
        if (currentLoanPrincipal > 0) return;

        long amount = GetMaxLoanLimit();
        currentLoanPrincipal = amount;
        hasPaidFirstInterest = false;

        nextPaymentDate = CoinManager.Instance.CurrentDateTime.AddDays(30);

        PlayerManager.Instance.satoshiBankCash += (double)amount;
        TransactionManager.Instance.AddRecord("대출실행", amount, "입금", "사토시 현금");

        RefreshLoanUI();
        RefreshSatoshiBankUI();
        ClosePanel();
    }

    public void OnRepayButtonClick() {
        if (repayInputField == null) return;
        ExecuteRepay(repayInputField.text);
    }

    private void ExecuteRepay(string inputAmount) {
        if (currentLoanPrincipal <= 0) return;

        string raw = inputAmount.Replace(",", "");
        if (!long.TryParse(raw, out long requestAmount) || requestAmount <= 0) return;

        double myCash = PlayerManager.Instance.satoshiBankCash;

        // 1. 현재 갚아야 할 페널티 이자 계산
        long penaltyInterest = 0;
        if (!hasPaidFirstInterest) {
            double monthlyRate = GetCurrentAnnualRate() / 12.0 / 100.0;
            penaltyInterest = (long)(currentLoanPrincipal * monthlyRate);
        }

        // 2. 실제 상환 로직 결정
        // 유저가 입력한 금액에서 이자를 먼저 까고 남은 걸 원금에서 뺍니다.
        long actualRepayTotal = (long)Math.Min((double)requestAmount, myCash);

        // 상환액이 이자보다 적으면 상환 불가 (최소 이자는 내야 함)
        if (!hasPaidFirstInterest && actualRepayTotal < penaltyInterest) {
            Debug.LogWarning("상환 금액이 최소 이자보다 적습니다.");
            return;
        }

        // 3. 지불 처리
        if (!hasPaidFirstInterest) {
            // 이자 먼저 처리
            PlayerManager.Instance.satoshiBankCash -= penaltyInterest;
            TransactionManager.Instance.AddRecord("대출이자(조기상환)", penaltyInterest, "출금", "사토시 현금");
            actualRepayTotal -= penaltyInterest;
            hasPaidFirstInterest = true;
        }

        // 남은 금액으로 원금 상환
        long repayToPrincipal = (long)Math.Min((double)actualRepayTotal, currentLoanPrincipal);
        PlayerManager.Instance.satoshiBankCash -= repayToPrincipal;
        currentLoanPrincipal -= repayToPrincipal;
        TransactionManager.Instance.AddRecord("대출상환", repayToPrincipal, "출금", "사토시 현금");

        if (currentLoanPrincipal <= 0) {
            overdueCount = 0;
            hasPaidFirstInterest = false;
            Debug.Log("대출 전액 상환 완료");
        }

        RefreshLoanUI();
        RefreshSatoshiBankUI();
        repayInputField.text = "";
        ClosePanel();
    }

    public void RefreshLoanUI(bool forceOpen = false) {
        bool hasLoan = currentLoanPrincipal > 0;

        if (mainLoanOpenButton != null) {
            mainLoanOpenButton.interactable = !hasLoan;
            if (hasLoan) {
                SetAlpha(targetImage0, 0f);
                SetAlpha(targetImage30, 30f / 255f);
                SetAlpha(targetText30, 30f / 255f);
            } else {
                SetAlpha(targetImage0, 1f);
                SetAlpha(targetImage30, 1f);
                SetAlpha(targetText30, 1f);
            }
        }

        if (forceOpen || isPanelOpen) {
            isPanelOpen = true;
            if (loanRequestGroup != null) loanRequestGroup.SetActive(!hasLoan);
            if (loanRepayGroup != null) loanRepayGroup.SetActive(hasLoan);
        }

        UpdateUITexts(hasLoan);
    }

    private void SetAlpha(Graphic graphic, float alpha) {
        if (graphic == null) return;
        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    private void UpdateUITexts(bool hasLoan) {
        if (!hasLoan) {
            if (requestDescriptionText != null) {
                float rate = GetCurrentAnnualRate();
                long limit = GetMaxLoanLimit();
                requestDescriptionText.text = $"대출 금리 <color=#FFD700>{rate:F1}%</color>로 <color=#00FF00>{limit:N0}원</color>을 대출 받으시겠습니까?\n\n" +
                                              "대출 이자는 다음달부터 사토시 은행으로 자동 청구 됩니다.";
            }
        } else {
            if (currentDebtText != null) {
                // ★ 합산된 총 대출금 가져오기
                long totalDebt = GetTotalDebtAmount();
                long penalty = totalDebt - (long)currentLoanPrincipal;

                string extraMsg = penalty > 0
                    ? $"\n<size=70%><color=#FF0000>(조기 완납 이자 {penalty:N0}원 포함)</color></size>"
                    : "";

                currentDebtText.text = $"현재 남은 대출금: <color=#FF5555>{totalDebt:N0}원</color>{extraMsg}";
            }
        }
    }

    public void CheckLoanTick() {
        if (currentLoanPrincipal <= 0) return;

        if (CoinManager.Instance.CurrentDateTime >= nextPaymentDate) {
            ProcessInterest();
            nextPaymentDate = nextPaymentDate.AddDays(30);
        }
    }

    private void ProcessInterest() {
        double monthlyRate = GetCurrentAnnualRate() / 12.0 / 100.0;
        long interest = (long)Math.Round((decimal)currentLoanPrincipal * (decimal)monthlyRate);

        if (interest <= 0) return;

        PlayerManager.Instance.satoshiBankCash -= interest;
        hasPaidFirstInterest = true;

        string notiTitle = "대출 이자 정산";
        string notiMsg = $"이자 {interest:N0}원이 정산되었습니다.";

        TransactionManager.Instance.AddRecord("대출이자", interest, "출금", "사토시 현금");

        if (GlobalNotificationManager.Instance != null) {
            GlobalNotificationManager.Instance.ShowNotification("Bank", notiTitle, notiMsg, null);
        }

        CoinManager.Instance.UpdateCashText();
        RefreshLoanUI();
        RefreshSatoshiBankUI();
    }

    public void ClosePanel() {
        if (loanRequestGroup != null) loanRequestGroup.SetActive(false);
        if (loanRepayGroup != null) loanRepayGroup.SetActive(false);
        isPanelOpen = false;
    }

    private void RefreshSatoshiBankUI() {
        SatoshiBankPanel bankPanel = FindAnyObjectByType<SatoshiBankPanel>();
        if (bankPanel != null) bankPanel.RefreshLoanStatus();
    }
}