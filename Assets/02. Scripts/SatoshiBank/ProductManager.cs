using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class ProductManager : MonoBehaviour {
    [System.Serializable]
    public class DepositData {
        public long principal;      // 예금 원금
        public int durationMonth;   // 가입 기간 (1, 3, 6, 12)
        public double interestRate; // 가입 당시 금리
        public DateTime joinDate;   // 가입일
        public DateTime expireDate; // 만기일
    }

    // ProductManager 클래스 내부에 리스트 선언
    public System.Collections.Generic.List<DepositData> myDeposits = new System.Collections.Generic.List<DepositData>();

    public static ProductManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI depositLimitText;     // 예금 한도 표시
    public Slider amountSlider;                  // 금액 조절 슬라이더
    public TextMeshProUGUI currentSelectAmountText; // 슬라이더 현재 금액 표시용
    public TextMeshProUGUI warningPopupText;     // 10만원 미만 경고 문구

    [Header("Deposit Info Lines")]
    public TextMeshProUGUI line1Month;
    public TextMeshProUGUI line3Month;
    public TextMeshProUGUI line6Month;
    public TextMeshProUGUI line12Month;

    [Header("Panel Reference")]
    public GameObject productPanel;

    [Header("UI Visual Feedback")]
    public Button openProductPanelButton;
    public Image targetImage0;           // 알파 0 (가입 시 사라질 요소)
    public Image targetImage30;          // 알파 30 (가입 시 희미해질 요소)
    public TextMeshProUGUI targetText30; // 알파 30 (가입 시 희미해질 텍스트)

    [Header("Cancel Panel UI")]
    public GameObject cancelConfirmPanel;       // "정말 해지하시겠습니까?" 팝업 패널
    public TextMeshProUGUI cancelDescriptionText; // 해지 경고 문구용 텍스트

    private long currentLimit = 10_000_000;

    private void Awake() {
        if (Instance == null) Instance = this;
        if (warningPopupText != null) warningPopupText.canvasRenderer.SetAlpha(0f); // 초기 투명도 설정
    }

    private void Start() {
        amountSlider.onValueChanged.AddListener(delegate { OnSliderChanged(); }); // 슬라이더 이벤트 연결
        RefreshDepositLimit();
        UpdateDepositUI();
        RefreshProductUI();

        StartCoroutine(CheckDepositExpiryRoutine());
    }

    // 예금 한도 갱신: 직급 및 회사 자본금 기반
    public void RefreshDepositLimit() {
        long baseLimit = 10_000_000;
        int rankBonus = OfficeManager.Instance.currentRankIndex * 5_000_000;
        long capitalBonus = (long)(OfficeManager.Instance.companyCapital * 0.1f);

        currentLimit = baseLimit + rankBonus + capitalBonus;

        depositLimitText.text = $"예금 한도: {currentLimit:N0}원";
        amountSlider.maxValue = currentLimit;
        amountSlider.minValue = 0;
    }

    public void OpenCancelConfirmPanel() {
        if (myDeposits.Count <= 0) return;

        // 현재 가입된 첫 번째 예금 기준으로 정보 표시 (복수 예금일 경우 index 처리가 필요할 수 있음)
        var deposit = myDeposits[0];
        long refundAmount = (long)(deposit.principal * 0.9f);

        if (cancelDescriptionText != null) {
            cancelDescriptionText.text = $"정말 상품을 중도해지하시겠습니까?\n\n" +
                                         $"<color=#FF5555>해지 시 예금 이자는 돌려받지 못하며\n" +
                                         $"가입 금액의 90%인 </color><color=#00FF00>{refundAmount:N0}원</color><color=#FF5555>만 환불 됩니다.</color>";
        }

        cancelConfirmPanel.SetActive(true);
    }

    public void ConfirmCancelDeposit() {
        if (myDeposits.Count <= 0) return;

        DepositData target = myDeposits[0];

        // --- 수정된 부분: 90% 계산 시 오차 방지 ---
        // principal이 long이므로 0.9m(decimal)을 곱해 정확한 금액 산출 후 반올림
        long refundAmount = (long)Math.Round((decimal)target.principal * 0.9m);
        // ------------------------------------------

        // 자금 환급 및 기록
        PlayerManager.Instance.satoshiBankCash += (double)refundAmount;
        TransactionManager.Instance.AddRecord("예금 중도해지", refundAmount, "입금", "사토시 현금");

        // 데이터 삭제 및 UI 갱신
        myDeposits.RemoveAt(0);
        RefreshProductUI();

        if (SatoshiBankPanel.Instance != null) {
            SatoshiBankPanel.Instance.RefreshDepositStatus();
        }

        CoinManager.Instance.UpdateCashText();
        cancelConfirmPanel.SetActive(false);

        Debug.Log($"예금 해지 완료: {refundAmount:N0}원 환불됨 (원금의 90%)");
    }

    // 3. 해지 취소 (팝업 내 '취소' 버튼에 연결)
    public void CloseCancelPanel() {
        cancelConfirmPanel.SetActive(false);
    }

    public void RefreshProductUI() {
        // 1. 가입된 예금이 있는지 체크
        bool hasDeposit = myDeposits.Count > 0;

        // 2. 패널을 여는 버튼 자체를 잠금
        if (openProductPanelButton != null) {
            openProductPanelButton.interactable = !hasDeposit;
        }

        // 3. 행님이 말씀하신 디자인 피드백 적용
        if (hasDeposit) {
            // [가입 중] 디자인: 버튼 안의 요소들이 희미해짐
            SetAlpha(targetImage0, 0f);
            SetAlpha(targetImage30, 30f / 255f);
            SetAlpha(targetText30, 30f / 255f);
        } else {
            // [가입 가능] 디자인: 다시 원래대로 (targetImage0은 100)
            SetAlpha(targetImage0, 100f / 255f);
            SetAlpha(targetImage30, 1f);
            SetAlpha(targetText30, 1f);
        }
    }

    private void SetAlpha(Graphic graphic, float alpha) {
        if (graphic == null) return;
        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    public void OnSliderChanged() {
        long val = (long)(amountSlider.value / 10000) * 10000; // 1만원 단위 절삭
        currentSelectAmountText.text = $"{val:N0}원";
        UpdateDepositUI();
    }

    private void UpdateDepositUI() {
        long amount = (long)(amountSlider.value / 10000) * 10000;
        double marketRate = GlobalEconomyManager.BaseInterestRate; // 시장 금리 참조

        // 기간별 텍스트 라인 갱신
        line1Month.text = FormatDepositLine(1, marketRate + 0.5, amount);
        line3Month.text = FormatDepositLine(3, marketRate * 1.5, amount);
        line6Month.text = FormatDepositLine(6, marketRate * 2.2, amount);
        line12Month.text = FormatDepositLine(12, marketRate * 3.5, amount);
    }

    // 대시(-) 개수 조절 및 포맷팅
    private string FormatDepositLine(int month, double rate, long amount) {
        // 1. UI에 표시될 금리와 동일하게 소수점 첫째 자리에서 반올림 처리
        // 예: 14.887% -> 14.9%
        double roundedRate = Math.Round(rate, 1);

        // 2. 반올림된 금리를 기준으로 수익 계산 (decimal을 써서 오차 차단)
        // 수익금 = (금액 * 반올림된 이율 / 100) * (개월수 / 12)
        decimal annualProfit = (decimal)amount * (decimal)roundedRate / 100m;
        long expectedProfit = (long)Math.Round((double)annualProfit * (month / 12.0));

        // 3. 텍스트 포맷팅
        string profitStr = $"+{expectedProfit:N0}원";

        // 자릿수에 따른 가변 대시 로직 
        int dashCount = 20 - (profitStr.Length - 5);
        dashCount = Mathf.Max(dashCount, 5);

        string dashes = new string('-', dashCount);
        string monthLabel = month >= 12 ? "12개월" : $"{month}개월";

        // roundedRate를 사용하여 텍스트 표기 (F1)
        return $"{monthLabel} ({roundedRate:F1}%) {dashes} {profitStr}";
    }

    // [버튼 클릭 이벤트 연결용 함수]
    // [버튼 클릭 이벤트 연결용 함수]
    public void JoinDeposit(int month) {
        long amount = (long)(amountSlider.value / 10000) * 10000;

        // 1. 가입 조건 체크
        if (amount < 100_000) {
            StopAllCoroutines();
            StartCoroutine(ShowWarningRoutine());
            return;
        }

        if (PlayerManager.Instance.satoshiBankCash < amount) {
            Debug.Log("사토시 은행 잔고가 부족합니다.");
            return;
        }

        // 2. 실제 가입 데이터 생성 및 저장 (이 부분이 핵심!)
        double marketRate = GlobalEconomyManager.BaseInterestRate;
        double currentRate = 0;

        // 기간별 금리 계산 로직 (UpdateDepositUI와 동일하게 맞춤)
        if (month == 1) currentRate = marketRate + 0.5;
        else if (month == 3) currentRate = marketRate * 1.5;
        else if (month == 6) currentRate = marketRate * 2.2;
        else if (month == 12) currentRate = marketRate * 3.5;

        // 화면에 보이는 것과 똑같이 반올림 적용
        currentRate = Math.Round(currentRate, 1);

        DepositData newDeposit = new DepositData {
            principal = amount,
            durationMonth = month,
            interestRate = currentRate,
            joinDate = CoinManager.Instance.CurrentDateTime,
            // 한 달을 30일로 계산하여 만기일 설정
            expireDate = CoinManager.Instance.CurrentDateTime.AddDays(month * 30)
        };

        // 리스트에 추가 (이제 기록이 남습니다!)
        myDeposits.Add(newDeposit);

        // 3. 자금 차감 및 기록
        PlayerManager.Instance.satoshiBankCash -= amount;
        TransactionManager.Instance.AddRecord($"{month}개월 예금 가입", amount, "출금", "사토시 현금");

        // 4. UI 갱신 및 패널 닫기
        if (SatoshiBankPanel.Instance != null) {
            SatoshiBankPanel.Instance.RefreshDepositStatus();
        }

        CoinManager.Instance.UpdateCashText();
        RefreshProductUI();
        ClosePanel();

        Debug.Log($"{month}개월 예금 {amount:N0}원 가입 성공! (이율: {currentRate}%)");
    }

    private IEnumerator ShowWarningRoutine() {
        warningPopupText.text = "최소 예금 금액은 10만원입니다."; // 경고 문구 설정
        warningPopupText.canvasRenderer.SetAlpha(1f);
        yield return new WaitForSecondsRealtime(2f); // 현실 시간 2초 대기
        warningPopupText.CrossFadeAlpha(0f, 0.5f, true);
    }

    public void ClosePanel() {
        if (productPanel != null) {
            productPanel.SetActive(false);
            Debug.Log("예금 가입 패널 비활성화 완료");
        } else {
            // 혹시 연결을 안 했을 경우를 대비해 경고를 띄웁니다.
            Debug.LogWarning("DepositPanelObject가 할당되지 않았습니다!");
        }
    }

    private IEnumerator CheckDepositExpiryRoutine() {
        while (true) {
            // [수정] 현실 시간 기준으로 2초마다 체크 (게임 일시정지 영향 안 받음)
            CheckAndProcessExpiry();
            yield return new WaitForSecondsRealtime(2f);
        }
    }

    public void CheckAndProcessExpiry() {
        if (myDeposits.Count <= 0) return;

        DateTime currentGameTime = CoinManager.Instance.CurrentDateTime;

        for (int i = myDeposits.Count - 1; i >= 0; i--) {
            // 만기일이 되었거나 지났다면 정산
            if (currentGameTime >= myDeposits[i].expireDate) {
                ProcessDepositExpiry(i);
            }
        }
    }

    private void ProcessDepositExpiry(int index) {
        DepositData deposit = myDeposits[index];

        // 1. 만기 이자 계산 (정확한 금액을 위해 decimal 사용)
        decimal rate = (decimal)deposit.interestRate;
        decimal principal = (decimal)deposit.principal;
        decimal annualProfit = principal * rate / 100m;
        long interestProfit = (long)Math.Round((double)annualProfit * (deposit.durationMonth / 12.0));

        long totalPayout = deposit.principal + interestProfit;

        // 2. 입금 처리
        PlayerManager.Instance.satoshiBankCash += (double)totalPayout;
        TransactionManager.Instance.AddRecord($"{deposit.durationMonth}개월 상품 만기", totalPayout, "입금", "사토시 현금");

        // 3. SMS 알림 구성
        string fullBody = "[상품 만기 완료]\n\n" +
                          $"{deposit.durationMonth}개월 상품이 정산되었습니다.\n\n" +
                          $"■ 가입 원금: {deposit.principal:N0}원\n" +
                          $"■ 확정 이자: +{interestProfit:N0}원\n" +
                          $"■ 총 입금액: {totalPayout:N0}원\n";

        if (GlobalNotificationManager.Instance != null) {
            GlobalNotificationManager.Instance.ShowNotification(
                "Bank",
                "예금 만기 완료",
                $"{deposit.durationMonth}개월 예금이 만기되어 {totalPayout:N0}원이 입금되었습니다.",
                () => {
                    if (UIManager.Instance != null) {
                        UIManager.Instance.ShowSMSResult($"발신인: 사토시 은행\n\n{fullBody}");
                    }
                }
            );
        }

        // 4. 데이터 삭제 및 UI 갱신
        myDeposits.RemoveAt(index);
        RefreshProductUI(); // 메인 화면 버튼 다시 활성화 및 알파값 복구

        if (SatoshiBankPanel.Instance != null) {
            SatoshiBankPanel.Instance.RefreshDepositStatus();
        }

        CoinManager.Instance.UpdateCashText();
        Debug.Log($"{deposit.durationMonth}개월 예금 만기 처리 완료: {totalPayout:N0}원 입금");
    }
}