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
    public Button allInRepayButton;

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

        // [추가] 인풋필드 값이 변할 때마다 체크하는 이벤트 리스너 등록
        if (repayInputField != null) {
            repayInputField.onValueChanged.AddListener(OnRepayInputChanged);
        }
    }

    public void OnRepayInputChanged(string input) {
        if (string.IsNullOrEmpty(input)) return;

        // 1. 콤마 제거 후 숫자로 변환
        string raw = input.Replace(",", "");
        if (!long.TryParse(raw, out long enteredAmount)) return;

        // 2. 내 현재 은행 잔고 확인
        long myMaxCash = (long)PlayerManager.Instance.satoshiBankCash;

        // 3. 갚아야 할 총 부채 확인
        long totalDebt = GetTotalDebtAmount();

        // 4. 입력값이 내 잔고 혹은 빚 총액보다 크다면 보정
        long limit = Math.Min(myMaxCash, totalDebt);

        if (enteredAmount > limit) {
            // 제한값으로 다시 설정 (천단위 콤마 포함)
            repayInputField.text = limit.ToString("N0");

            // 커서가 맨 뒤로 가게 설정 (입력 편의성)
            repayInputField.caretPosition = repayInputField.text.Length;
        }
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

        // [핵심 수정] 기록은 무조건 한글 원본 고정! (입금)
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

        long penaltyInterest = 0;
        if (!hasPaidFirstInterest) {
            double monthlyRate = GetCurrentAnnualRate() / 12.0 / 100.0;
            penaltyInterest = (long)(currentLoanPrincipal * monthlyRate);
        }

        long actualRepayTotal = (long)Math.Min((double)requestAmount, myCash);

        if (!hasPaidFirstInterest && actualRepayTotal < penaltyInterest) {
            Debug.LogWarning("상환 금액이 최소 이자보다 적습니다.");
            return;
        }

        if (!hasPaidFirstInterest) {
            PlayerManager.Instance.satoshiBankCash -= penaltyInterest;

            // [핵심 수정] 조기 상환 이자 지불 - 한글 원본 고정! (출금)
            TransactionManager.Instance.AddRecord("대출이자(조기상환)", penaltyInterest, "출금", "사토시 현금");

            actualRepayTotal -= penaltyInterest;
            hasPaidFirstInterest = true;
        }

        long repayToPrincipal = (long)Math.Min((double)actualRepayTotal, currentLoanPrincipal);
        PlayerManager.Instance.satoshiBankCash -= repayToPrincipal;
        currentLoanPrincipal -= repayToPrincipal;

        // [핵심 수정] 원금 상환 지불 - 한글 원본 고정! (출금)
        TransactionManager.Instance.AddRecord("대출상환", repayToPrincipal, "출금", "사토시 현금");

        if (currentLoanPrincipal <= 0) {
            overdueCount = 0;
            hasPaidFirstInterest = false;
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
        string unit = LocalizationManager.GetText("UNIT_CURRENCY"); // 공통 단위

        if (!hasLoan) {
            if (requestDescriptionText != null) {
                float rate = GetCurrentAnnualRate();
                long limit = GetMaxLoanLimit();
                // [수정] 대출 안내 문구 현지화
                requestDescriptionText.text = string.Format(LocalizationManager.GetText("MSG_LOAN_DESC"), rate.ToString("F1"), limit.ToString("N0"), unit);
            }
        } else {
            if (currentDebtText != null) {
                long totalDebt = GetTotalDebtAmount();
                long penalty = totalDebt - (long)currentLoanPrincipal;

                // [수정] 조기 완납 이자 문구 현지화
                string extraMsg = "";
                if (penalty > 0) {
                    extraMsg = string.Format(LocalizationManager.GetText("LBL_LOAN_EARLY_FEE"), penalty.ToString("N0"), unit);
                }

                // [수정] 현재 남은 대출금 문구 현지화
                currentDebtText.text = string.Format(LocalizationManager.GetText("LBL_LOAN_REMAINING_DEBT"), totalDebt.ToString("N0"), unit, extraMsg);
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

        string unit = LocalizationManager.GetText("UNIT_CURRENCY");

        // 알림 문구는 UI용이므로 현지화 유지
        string notiTitle = LocalizationManager.GetText("SMS_NOTI_LOAN_TITLE");
        string notiMsg = string.Format(LocalizationManager.GetText("SMS_NOTI_LOAN_MSG"), interest.ToString("N0"), unit);

        // [핵심 수정] 정기 이자 지불 - 한글 원본 고정! (출금)
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

    public void OnAllInRepayClick() {
        if (currentLoanPrincipal <= 0 || repayInputField == null) return;

        // 1. 내가 갚아야 할 총액 (원금 + 필요시 조기상환 이자)
        long totalDebt = GetTotalDebtAmount();

        // 2. 사토시 은행 내 잔고
        long myCash = (long)PlayerManager.Instance.satoshiBankCash;

        // 3. 둘 중 작은 금액을 선택 (빚보다 돈이 많으면 빚만큼만, 돈이 적으면 전재산만큼)
        long finalAmount = Math.Min(totalDebt, myCash);

        if (finalAmount <= 0) {
            Debug.LogWarning("상환할 수 있는 잔액이 없습니다.");
            return;
        }

        // 4. 인풋필드에 값 넣기 (천단위 콤마 포함)
        repayInputField.text = finalAmount.ToString("N0");

        Debug.Log($"[대출상환] 자동 입력 완료: {finalAmount:N0}원 (잔고: {myCash:N0}원)");
    }
}