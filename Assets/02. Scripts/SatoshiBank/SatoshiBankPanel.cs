using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // 버튼 제어를 위해 추가

public class SatoshiBankPanel : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI userNameLabel;

    [Header("Loan Info UI")]
    [SerializeField] private TextMeshProUGUI loanBalanceText;    // 대출 잔액 TMP
    [SerializeField] private TextMeshProUGUI estimatedInterestText; // 예상이자(금리) TMP
    [SerializeField] private TextMeshProUGUI nextPaymentDateText;   // 다음 납부일 TMP
    [SerializeField] private Button repayPanelOpenButton;       // 납부하기 버튼 (패널 여는 용도)

    [Header("Deposit Info UI")]
    [SerializeField] private TextMeshProUGUI depositMainInfoText;   // "가입 상품 없음" 또는 "예금액: XXX" 표기용
    [SerializeField] private TextMeshProUGUI depositDetailText;     // 남은 기간 및 예상 수익 표기용
    [SerializeField] private Button cancelDepositButton;

    public static SatoshiBankPanel Instance; // 싱글톤 선언

    private void Awake() {
        if (Instance == null) Instance = this;
    }

    void Start() {
        PlayerManager.Instance.OnPlayerNameChanged += RefreshName;
        RefreshName();
        RefreshLoanStatus();
        RefreshDepositStatus();
    }

    // 패널이 켜질 때마다 최신 정보로 갱신
    void OnEnable() {
        RefreshLoanStatus();   // 대출 정보 갱신
        RefreshDepositStatus(); // 예금 정보 갱신

        if (ExchangeChartManager.Instance != null) {
            ExchangeChartManager.Instance.DrawChart();
        }
    }

    void OnDestroy() {
        if (PlayerManager.Instance != null) {
            PlayerManager.Instance.OnPlayerNameChanged -= RefreshName;
        }
    }

    void RefreshName() {
        // [수정] 인사말 현지화 적용
        if (userNameLabel != null) {
            string greetingFormat = LocalizationManager.GetText("MSG_BANK_GREETING");
            userNameLabel.text = string.Format(greetingFormat, PlayerManager.Instance.playerName);
        }
    }

    // [핵심] 대출 상태 및 UI 텍스트 갱신 함수
    public void RefreshLoanStatus() {
        if (LoanManager.Instance == null) return;

        double principal = LoanManager.Instance.currentLoanPrincipal;
        bool hasLoan = principal > 0;
        string unit = LocalizationManager.GetText("UNIT_CURRENCY"); // 원 / Won

        // 1. 대출 잔액 표시 (hasLoan 체크 강화)
        if (loanBalanceText != null) {
            loanBalanceText.text = hasLoan ? $"{principal:N0}{unit}" : LocalizationManager.GetText("LBL_LOAN_NONE");
        }

        // 2. 상세 정보 갱신
        if (hasLoan) {
            float annualRate = LoanManager.Instance.GetCurrentAnnualRate();
            double monthlyRate = annualRate / 12.0 / 100.0;
            long interest = (long)(principal * monthlyRate);

            if (estimatedInterestText != null) {
                estimatedInterestText.text = string.Format(LocalizationManager.GetText("LBL_LOAN_EST_INTEREST"), interest.ToString("N0"), unit, annualRate.ToString("F1"));
            }

            if (nextPaymentDateText != null) {
                string formattedDate = LoanManager.Instance.nextPaymentDate.ToString("MM/dd/yyyy");
                nextPaymentDateText.text = string.Format(LocalizationManager.GetText("LBL_LOAN_NEXT_PAY"), formattedDate);
            }
        } else {
            if (estimatedInterestText != null) estimatedInterestText.text = " ";
            if (nextPaymentDateText != null) nextPaymentDateText.text = " ";
        }

        // 버튼 활성화
        if (repayPanelOpenButton != null) repayPanelOpenButton.gameObject.SetActive(hasLoan);

        CoinManager.Instance.UpdateCashText();
    }

    // 납부하기 버튼에 연결할 함수 (기존 LoanManager UI 갱신 호출)
    public void OnClickOpenRepayPanel() {
        if (LoanManager.Instance != null) {
            LoanManager.Instance.RefreshLoanUI();
            // 여기서 실제로 상환 패널(loanRepayGroup)을 띄우는 추가 로직이 필요할 수 있습니다.
        }
    }
    public void OnClickOpenProductPanel() {
        if (ProductManager.Instance != null) {
            // 1. 최신 예금 한도를 계산해서 UI에 반영합니다.
            ProductManager.Instance.RefreshDepositLimit();

            // 2. 예금 상품 패널 오브젝트를 활성화합니다.
            if (ProductManager.Instance.productPanel != null) {
                ProductManager.Instance.productPanel.SetActive(true);
                Debug.Log("예금 상품 패널 오픈");
            }
        }
    }
    public void RefreshDepositStatus() {
        if (ProductManager.Instance == null) return;

        bool hasDeposit = ProductManager.Instance.myDeposits.Count > 0;
        string unit = LocalizationManager.GetText("UNIT_CURRENCY"); // 원 / Won

        if (!hasDeposit) {
            if (depositMainInfoText != null)
                depositMainInfoText.text = LocalizationManager.GetText("LBL_DEPOSIT_NONE");

            if (depositDetailText != null) depositDetailText.text = "";
            if (cancelDepositButton != null) cancelDepositButton.gameObject.SetActive(false);
        } else {
            if (cancelDepositButton != null) cancelDepositButton.gameObject.SetActive(true);

            // 가장 최근 가입 상품 정보 가져오기
            var data = ProductManager.Instance.myDeposits[ProductManager.Instance.myDeposits.Count - 1];

            if (depositMainInfoText != null) {
                depositMainInfoText.text = string.Format(
                    LocalizationManager.GetText("LBL_DEPOSIT_MAIN_INFO"),
                    data.principal.ToString("N0"),
                    unit,
                    data.durationMonth,
                    data.interestRate.ToString("F1")
                );
            }

            if (depositDetailText != null) {
                // 남은 기간 계산
                TimeSpan remaining = data.expireDate - CoinManager.Instance.CurrentDateTime;
                int remainingDays = Mathf.Max(0, remaining.Days);

                // 예상 수익 계산
                long expectedProfit = (long)(data.principal * (data.interestRate / 100.0) * (data.durationMonth / 12.0));

                depositDetailText.text = string.Format(
                    LocalizationManager.GetText("LBL_DEPOSIT_DETAIL_INFO"),
                    remainingDays,
                    expectedProfit.ToString("N0"),
                    unit
                );
            }
        }
    }

    // 중도 해지 버튼에 연결할 함수
    public void OnClickCancelDeposit() {
        // TODO: 중도 해지 시 원금만 돌려주거나 패널티를 주는 로직 추가 가능
        Debug.Log("중도 해지 프로세스 시작");
    }
}