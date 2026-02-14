using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public class LoanManager : MonoBehaviour {
    public static LoanManager Instance;

    [Header("Loan State")]
    public double currentLoanPrincipal = 0; // 빌린 원금
    public DateTime nextPaymentDate;        // 다음 이자 징수일
    public int overdueCount = 0;            // 연체 횟수 (기록용)

    [Header("UI State")]
    private bool isPanelOpen = false;       // 현재 패널이 명시적으로 열려 있는지 여부

    [Header("UI Reference")]
    public GameObject loanRequestGroup;    // 대출 전 UI 그룹
    public GameObject loanRepayGroup;      // 대출 후 UI 그룹
    public TMP_InputField repayInputField; // 상환 금액 입력창

    [Header("Text UI")]
    [SerializeField] private TextMeshProUGUI requestDescriptionText; // 대출 안내 문구
    [SerializeField] private TextMeshProUGUI currentDebtText;        // 상환 패널용 남은 원금 표시

    [Header("MAIN LOAN UI")]
    public Button mainLoanOpenButton;
    public Image targetImage0;       // 알파값 0으로 만들 이미지
    public Image targetImage30;      // 알파값 30으로 만들 이미지
    public TextMeshProUGUI targetText30; // 알파값 30으로 만들 텍스트

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

    // [중요] 외부 버튼에서 패널을 열 때 반드시 이 함수를 호출하세요
    public void OpenLoanPanel() {
        RefreshLoanUI(true);
    }

    // 3. 대출 승인 버튼 연결용
    public void OnApproveLoan() {
        if (currentLoanPrincipal > 0) return;

        long amount = GetMaxLoanLimit();
        currentLoanPrincipal = amount;

        // 테스트용 3일 주기 설정
        nextPaymentDate = CoinManager.Instance.CurrentDateTime.AddDays(3);

        PlayerManager.Instance.satoshiBankCash += (double)amount;
        TransactionManager.Instance.AddRecord("대출실행", amount, "입금", "사토시 현금");

        Debug.Log($"대출 승인: {amount:N0}원");

        RefreshLoanUI();
        RefreshSatoshiBankUI();
        ClosePanel();
    }

    // 4. 상환 버튼 연결용
    public void OnRepayButtonClick() {
        if (repayInputField == null) return;
        ExecuteRepay(repayInputField.text);
    }

    private void ExecuteRepay(string inputAmount) {
        if (currentLoanPrincipal <= 0) return;

        string raw = inputAmount.Replace(",", "");
        if (!long.TryParse(raw, out long amount) || amount <= 0) return;

        long actualRepay = (long)Math.Min((double)amount, PlayerManager.Instance.satoshiBankCash);
        actualRepay = (long)Math.Min((double)actualRepay, currentLoanPrincipal);

        PlayerManager.Instance.satoshiBankCash -= actualRepay;
        currentLoanPrincipal -= actualRepay;

        TransactionManager.Instance.AddRecord("대출상환", actualRepay, "출금", "사토시 현금");

        if (currentLoanPrincipal <= 0) {
            overdueCount = 0;
            Debug.Log("대출 전액 상환 완료");
        }

        RefreshLoanUI();
        RefreshSatoshiBankUI();

        repayInputField.text = "";
        ClosePanel();
    }

    public void RefreshLoanUI(bool forceOpen = false) {
        bool hasLoan = currentLoanPrincipal > 0;

        // [핵심] 대출이 이미 있다면 메인 화면의 대출 버튼 비활성화
        if (mainLoanOpenButton != null) {
            mainLoanOpenButton.interactable = !hasLoan;

            // --- 알파값 조정 로직 추가 ---
            if (hasLoan) {
                // 대출이 있을 때 (버튼이 꺼질 때) 디자인 적용
                SetAlpha(targetImage0, 0f);
                SetAlpha(targetImage30, 30f / 255f); // 0~1 사이 값으로 변환
                SetAlpha(targetText30, 30f / 255f);
            } else {
                // 대출이 없을 때 (원래대로 복구 - 필요 시)
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

    // 알파값 조절을 편하게 하기 위한 헬퍼 함수
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
                currentDebtText.text = $"현재 남은 대출 원금: <color=#FF5555>{currentLoanPrincipal:N0}원</color>";
            }
        }
    }

    // 6. 주기 체크 (3일 테스트 모드)
    public void CheckLoanTick() {
        if (currentLoanPrincipal <= 0) return;

        if (CoinManager.Instance.CurrentDateTime >= nextPaymentDate) {
            ProcessInterest();
            // 정산된 기준 날짜에서 정확히 3일을 더함
            nextPaymentDate = nextPaymentDate.AddDays(3);
            Debug.Log($"이자 정산 완료. 다음 예정일: {nextPaymentDate:yyyy-MM-dd}");
        }
    }

    private void ProcessInterest() {
        // 1. 이자 계산 (이자율 기반)
        double monthlyRate = GetCurrentAnnualRate() / 12.0 / 100.0;
        long interest = (long)Math.Round((decimal)currentLoanPrincipal * (decimal)monthlyRate);

        if (interest <= 0) return;

        // 2. [핵심] 원금은 절대 건드리지 않고 잔고에서 무조건 차감
        // 잔고가 0보다 작아져도 유니티 double/long은 마이너스 값을 지원합니다.
        PlayerManager.Instance.satoshiBankCash -= interest;

        // 알림 및 명세서 구성
        string notiTitle = "대출 이자 정산";
        string notiMsg = $"이자 {interest:N0}원이 정산되었습니다.";
        string fullBody = "[대출 이자 명세서]\n\n" +
                          $"■ 발생 이자: -{interest:N0}원\n" +
                          $"■ 대출 원금: {currentLoanPrincipal:N0}원\n" +
                          $"■ 현재 잔고: {PlayerManager.Instance.satoshiBankCash:N0}원\n";

        // 3. 기록 및 UI 갱신
        TransactionManager.Instance.AddRecord("대출이자", interest, "출금", "사토시 현금");

        if (GlobalNotificationManager.Instance != null) {
            GlobalNotificationManager.Instance.ShowNotification(
                "Bank", notiTitle, notiMsg,
                () => {
                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowSMSResult($"발신인: 사토시 은행\n\n{fullBody}");
                }
            );
        }

        CoinManager.Instance.UpdateCashText();
        RefreshLoanUI();
        RefreshSatoshiBankUI();
    }

    public void ClosePanel() {
        if (loanRequestGroup != null) loanRequestGroup.SetActive(false);
        if (loanRepayGroup != null) loanRepayGroup.SetActive(false);

        isPanelOpen = false; // 상태 초기화
        Debug.Log("대출/상환 패널 모두 종료");
    }

    private void RefreshSatoshiBankUI() {
        SatoshiBankPanel bankPanel = FindAnyObjectByType<SatoshiBankPanel>();
        if (bankPanel != null) {
            bankPanel.RefreshLoanStatus();
        }
    }
}