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
    public TextMeshProUGUI bankCashText;

    private long currentLimit = 10_000_000;
    private bool isWarningActive = false;

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

    private void Update() {
        if (bankCashText != null && PlayerManager.Instance != null) {   
            string unit = LocalizationManager.GetText("UNIT_CURRENCY");
            bankCashText.text = string.Format(LocalizationManager.GetText("LBL_BANK_BALANCE_SIMPLE"), PlayerManager.Instance.satoshiBankCash.ToString("N0"), unit);
        }
    }

    // 예금 한도 갱신: 직급 및 회사 자본금 기반
    public void RefreshDepositLimit() {
        long baseLimit = 10_000_000;
        int rankBonus = OfficeManager.Instance.currentRankIndex * 5_000_000;
        long capitalBonus = (long)(OfficeManager.Instance.companyCapital * 0.1f);

        currentLimit = baseLimit + rankBonus + capitalBonus;

        // [수정] 예금 한도 텍스트 현지화
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");
        depositLimitText.text = string.Format(LocalizationManager.GetText("LBL_DEPOSIT_LIMIT"), currentLimit.ToString("N0"), unit);

        amountSlider.maxValue = currentLimit;
        amountSlider.minValue = 0;
    }

    public void OpenCancelConfirmPanel() {
        if (myDeposits.Count <= 0) return;

        var deposit = myDeposits[0];
        long refundAmount = (long)(deposit.principal * 0.9f);

        if (cancelDescriptionText != null) {
            // [수정] 해지 경고 팝업 문구 현지화
            string unit = LocalizationManager.GetText("UNIT_CURRENCY");
            cancelDescriptionText.text = string.Format(LocalizationManager.GetText("MSG_CANCEL_DEPOSIT_DESC"), refundAmount.ToString("N0"), unit);
        }

        cancelConfirmPanel.SetActive(true);
    }

    public void ConfirmCancelDeposit() {
        if (myDeposits.Count <= 0) return;

        DepositData target = myDeposits[0];
        long refundAmount = (long)Math.Round((decimal)target.principal * 0.9m);

        PlayerManager.Instance.satoshiBankCash += (double)refundAmount;

        // [핵심 수정] 기록은 무조건 한글 원본 데이터 고정! (입금)
        TransactionManager.Instance.AddRecord("예금 중도해지", refundAmount, "입금", "사토시 현금");

        myDeposits.RemoveAt(0);
        RefreshProductUI();

        if (SatoshiBankPanel.Instance != null) SatoshiBankPanel.Instance.RefreshDepositStatus();

        CoinManager.Instance.UpdateCashText();
        cancelConfirmPanel.SetActive(false);
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
        double currentBankCash = PlayerManager.Instance.satoshiBankCash;

        // 슬라이더 값이 은행 잔액을 초과할 경우
        if (amountSlider.value > currentBankCash) {
            // 슬라이더 값을 현재 잔액으로 강제 고정
            amountSlider.SetValueWithoutNotify((float)currentBankCash);

            // 경고 팝업 띄우기 (중복 실행 방지)
            if (!isWarningActive) {
                StopCoroutine("ShowWarningRoutine");
                // [수정] 하드코딩된 한국어 경고문을 현지화 키로 변경
                StartCoroutine(ShowWarningRoutine(LocalizationManager.GetText("MSG_WARN_LACK_BALANCE_01")));
            }
        }

        // 1만원 단위 절삭
        long val = (long)(amountSlider.value / 10000) * 10000;

        // [수정] '원' 하드코딩을 현지화된 화폐 단위로 변경
        string unit = LocalizationManager.GetText("UNIT_CURRENCY_2");
        currentSelectAmountText.text = $"{val:N0}{unit}";

        UpdateDepositUI();
    }

    private void UpdateDepositUI() {
        long amount = (long)(amountSlider.value / 10000) * 10000;
        double marketRate = GlobalEconomyManager.BaseInterestRate;

        // [수정] 각 개월 라인에 들어갈 텍스트 포맷 시 단위 전달
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");
        line1Month.text = FormatDepositLine(1, marketRate + 0.5, amount, unit);
        line3Month.text = FormatDepositLine(3, marketRate * 1.5, amount, unit);
        line6Month.text = FormatDepositLine(6, marketRate * 2.2, amount, unit);
        line12Month.text = FormatDepositLine(12, marketRate * 3.5, amount, unit);
    }

    // 대시(-) 개수 조절 및 포맷팅
    private string FormatDepositLine(int month, double rate, long amount, string unit) {
        double roundedRate = Math.Round(rate, 1);
        decimal annualProfit = (decimal)amount * (decimal)roundedRate / 100m;
        long expectedProfit = (long)Math.Round((double)annualProfit * (month / 12.0));

        string profitStr = $"+{expectedProfit:N0}{unit}"; // [수정] 원 -> unit 적용

        int dashCount = 20 - (profitStr.Length - 5);
        dashCount = Mathf.Max(dashCount, 5);

        string dashes = new string('-', dashCount);

        // [수정] 개월 라벨 현지화
        string monthLabel = string.Format(LocalizationManager.GetText("LBL_MONTH_LABEL"), month);

        return $"{monthLabel} ({roundedRate:F1}%) {dashes} {profitStr}";
    }
    public void JoinDeposit(int month) {
        long amount = (long)(amountSlider.value / 10000) * 10000;

        if (amount < 100_000) {
            StopCoroutine("ShowWarningRoutine");
            StartCoroutine(ShowWarningRoutine(LocalizationManager.GetText("MSG_WARN_MIN_DEPOSIT")));
            return;
        }

        if (PlayerManager.Instance.satoshiBankCash < amount) {
            StopCoroutine("ShowWarningRoutine");
            StartCoroutine(ShowWarningRoutine(LocalizationManager.GetText("MSG_WARN_LACK_BALANCE")));
            return;
        }

        double marketRate = GlobalEconomyManager.BaseInterestRate;
        double currentRate = 0;

        if (month == 1) currentRate = marketRate + 0.5;
        else if (month == 3) currentRate = marketRate * 1.5;
        else if (month == 6) currentRate = marketRate * 2.2;
        else if (month == 12) currentRate = marketRate * 3.5;

        currentRate = Math.Round(currentRate, 1);

        DepositData newDeposit = new DepositData {
            principal = amount,
            durationMonth = month,
            interestRate = currentRate,
            joinDate = CoinManager.Instance.CurrentDateTime,
            expireDate = CoinManager.Instance.CurrentDateTime.AddDays(month * 30)
        };

        myDeposits.Add(newDeposit);
        PlayerManager.Instance.satoshiBankCash -= amount;

        // [핵심 수정] 기록은 무조건 한글 원본 데이터 고정! (출금)
        // "{0}개월 예금 가입" 처럼 포맷팅된 문자열을 보내면 TransactionItem에서 번역을 못 찾습니다!
        // 따라서 "예금가입" 이라는 고정된 단어로 보내주세요.
        TransactionManager.Instance.AddRecord("예금가입", amount, "출금", "사토시 현금");

        if (SatoshiBankPanel.Instance != null) SatoshiBankPanel.Instance.RefreshDepositStatus();

        CoinManager.Instance.UpdateCashText();
        RefreshProductUI();
        ClosePanel();
    }

    private IEnumerator ShowWarningRoutine(string message) {
        isWarningActive = true;
        warningPopupText.text = message;
        warningPopupText.canvasRenderer.SetAlpha(1f);

        yield return new WaitForSecondsRealtime(1.5f); // 1.5초간 유지

        warningPopupText.CrossFadeAlpha(0f, 0.5f, true);
        yield return new WaitForSecondsRealtime(0.5f);
        isWarningActive = false;
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

        decimal rate = (decimal)deposit.interestRate;
        decimal principal = (decimal)deposit.principal;
        decimal annualProfit = principal * rate / 100m;
        long interestProfit = (long)Math.Round((double)annualProfit * (deposit.durationMonth / 12.0));

        long totalPayout = deposit.principal + interestProfit;

        PlayerManager.Instance.satoshiBankCash += (double)totalPayout;

        // [핵심 수정] 기록은 무조건 한글 원본 데이터 고정! (입금)
        // 여기도 "예금만기" 라는 고정된 단어로 보내주세요.
        TransactionManager.Instance.AddRecord("예금만기", totalPayout, "입금", "사토시 현금");

        string unit = LocalizationManager.GetText("UNIT_CURRENCY");
        string fullBody = string.Format(
            LocalizationManager.GetText("SMS_DEPOSIT_EXPIRE_BODY"),
            deposit.durationMonth,
            deposit.principal.ToString("N0"),
            unit,
            interestProfit.ToString("N0"),
            totalPayout.ToString("N0")
        );

        string smsSender = string.Format(LocalizationManager.GetText("SMS_DEPOSIT_EXPIRE_SENDER"), fullBody);
        string notiTitle = LocalizationManager.GetText("SMS_NOTI_TITLE");
        string notiMsg = string.Format(LocalizationManager.GetText("SMS_NOTI_MSG"), deposit.durationMonth, totalPayout.ToString("N0"), unit);

        if (GlobalNotificationManager.Instance != null) {
            GlobalNotificationManager.Instance.ShowNotification(
                "Bank", notiTitle, notiMsg,
                () => {
                    if (UIManager.Instance != null) {
                        UIManager.Instance.ShowSMSResult(smsSender);
                    }
                }
            );
        }

        myDeposits.RemoveAt(index);
        RefreshProductUI();

        if (SatoshiBankPanel.Instance != null) SatoshiBankPanel.Instance.RefreshDepositStatus();
        CoinManager.Instance.UpdateCashText();
    }
}