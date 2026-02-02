using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class FourNanceWithdrawManager : MonoBehaviour {
    // ... (기존 UI 변수들 동일) ...

    [Header("패널 제어")]
    public GameObject withdrawPanel; // 출금 팝업 본체
    public Button closeButton;

    [Header("입력 및 표시 UI")]
    public TMP_InputField amountInput;
    public TextMeshProUGUI availableBalanceText; // 출금 가능 금액 ($)
    public TextMeshProUGUI noticeText;           // 안내 문구 (수수료 등)
    public Button withdrawButton;
    public Button maxAmountButton; // '전액' 버튼

    [Header("옵션 선택 UI")]
    public TMP_Dropdown coinDropdown;     // USDT, USDC
    public TMP_Dropdown platformDropdown; // 불비트 (고정)
    public TMP_Dropdown networkDropdown;  // 자동 설정
    public TMP_InputField addressInput;   // 자동 입력 (수정 불가)

    private double currentFee = 1; // 내부 이체라 수수료 0원 (필요시 변경)
    private double minWithdraw = 10; // 최소 출금 $10

    void Start() {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (withdrawButton != null) withdrawButton.onClick.AddListener(OnClickWithdraw);
        if (maxAmountButton != null) maxAmountButton.onClick.AddListener(OnClickAll);

        if (amountInput != null) amountInput.onValueChanged.AddListener(OnAmountChanged);
        if (coinDropdown != null) coinDropdown.onValueChanged.AddListener(delegate { RefreshOptions(); });

        SetupDropdowns();
    }

    // ... (OpenPanel, ClosePanel, SetupDropdowns 등 기존 코드 동일) ...
    public void OpenPanel() {
        withdrawPanel.SetActive(true);
        SetupDropdowns();
        RefreshUI();
    }

    public void ClosePanel() {
        withdrawPanel.SetActive(false);
    }

    void SetupDropdowns() {
        coinDropdown.ClearOptions();
        coinDropdown.AddOptions(new List<string> { "USDT", "USDC" });

        platformDropdown.ClearOptions();
        platformDropdown.AddOptions(new List<string> { "불비트 거래소 (현물지갑)" });
        platformDropdown.interactable = false;

        RefreshOptions();
    }

    void RefreshOptions() {
        string selectedCoin = coinDropdown.options[coinDropdown.value].text;
        networkDropdown.ClearOptions();

        if (selectedCoin == "USDT") {
            networkDropdown.AddOptions(new List<string> { "TRON (TRC-20)" });
            addressInput.text = "T-Bullbit-HotWallet-Deposit";
        } else if (selectedCoin == "USDC") {
            networkDropdown.AddOptions(new List<string> { "Arbitrum One" });
            addressInput.text = "0x-Bullbit-HotWallet-Deposit";
        }

        addressInput.interactable = false;
        RefreshUI();
    }
    private double GetWithdrawableAmount() {
        // 1. 현재 보유한 순수 현금
        double safeCash = PlayerManager.Instance.fournanceCash;

        // 2. 현재 포지션들의 미실현 손익(PnL) 합계 가져오기
        // (주의: FutureChartRenderer가 켜져 있어야 계산 가능)
        if (FutureChartRenderer.Instance != null) {

            // 수수료까지 감안한 순수 PnL 합계 (Net PnL)
            double totalPnL = FutureChartRenderer.Instance.CalculateTotalUnrealizedPnL();

            // [핵심] PnL이 '마이너스(손실)'일 때만 현금을 깎아먹음
            // 수익(양수)일 때는 담보가 늘어난 것일 뿐, 현금화 전까진 출금 불가하므로 무시(0으로 처리)
            double floatingLoss = Math.Min(0, totalPnL);

            // 최종 출금 가능액 = 현금 - 손실분
            return Math.Max(0, safeCash + floatingLoss);
        }

        // 차트가 안 켜져있다면(로비 등) 일단 현금만 리턴 (혹은 0 리턴하여 안전하게 처리)
        return safeCash;
    }

    // 3. UI 갱신 (잔고 표시 등)
    void RefreshUI() {
        if (PlayerManager.Instance == null) return;

        // [수정] 현금 잔고가 아니라 '출금 가능액(Buying Power)'을 가져옵니다.
        double withdrawableAmount = GetWithdrawableAmount();

        if (availableBalanceText != null) {
            // 수수료를 뺀 실질적 출금 가능액
            double finalWithdrawable = Math.Max(0, withdrawableAmount - currentFee);
            availableBalanceText.text = $"출금 가능: ${finalWithdrawable:N2}";
        }

        if (noticeText != null) {
            noticeText.text = $"최소 출금 ${minWithdraw} 이상, 출금 수수료: ${currentFee} 차감됩니다";
        }
    }

    // 4. 전액 버튼 로직
    void OnClickAll() {
        double withdrawableAmount = GetWithdrawableAmount();

        // 1. 수수료 먼저 뺌
        double rawMax = withdrawableAmount - currentFee;

        // 2. [핵심] 소수점 2자리 밑으로 무조건 내림 (버림) 처리
        // 예: 계산 결과가 99.999달러여도 99.99달러만 입력시킴 (안전빵)
        double safeMax = FloorToTwoDecimal(rawMax);

        // 0보다 작으면 0
        safeMax = Math.Max(0, safeMax);

        amountInput.text = safeMax.ToString("F2");
    }

    // 5. 금액 입력 감지 (유효성 검사)
    void OnAmountChanged(string val) {
        if (!double.TryParse(val, out double inputAmount)) {
            withdrawButton.interactable = false;
            return;
        }

        // [수정] Buying Power 기준으로 검사
        double withdrawableAmount = GetWithdrawableAmount();

        inputAmount = Math.Round(inputAmount, 2);
        double totalCost = Math.Round(inputAmount + currentFee, 2);
        withdrawableAmount = Math.Round(withdrawableAmount, 2);

        bool isValid =
            inputAmount >= minWithdraw &&
            totalCost <= withdrawableAmount; // 내 Buying Power보다 작아야 출금 가능

        withdrawButton.interactable = isValid;
    }

    // 6. [핵심] 출금 실행 로직
    // 6. [핵심] 출금 실행 로직
    void OnClickWithdraw() {
        if (!double.TryParse(amountInput.text, out double amount)) return;

        double withdrawableAmount = GetWithdrawableAmount();
        double totalCost = amount + currentFee;

        // [핵심 수정] 부동소수점 오차 무시 (Epsilon 비교)
        if (totalCost > withdrawableAmount + 0.00001) {
            Debug.LogError($"잔액 부족! 보유: {withdrawableAmount}, 필요: {totalCost}");
            return;
        }

        // --- 실제 차감 로직 ---
        if (totalCost > withdrawableAmount) {
            totalCost = withdrawableAmount;
        }

        PlayerManager.Instance.fournanceCash -= totalCost;

        if (PlayerManager.Instance.fournanceCash < 0) PlayerManager.Instance.fournanceCash = 0;

        // ------------------------------------------------------------------
        // [수정] 단순 ChangeCoin 대신, 현재가 기준으로 '매수' 처리하여 평단가 유지
        // ------------------------------------------------------------------
        string coinSymbol = coinDropdown.options[coinDropdown.value].text;

        // 1. 현재 코인의 KRW 가격 가져오기 (불비트 시세 기준)
        CoinData coinData = CoinManager.Instance.coins.Find(c => c.Symbol == coinSymbol);
        double currentPriceKrw = 0;

        if (coinData != null) {
            currentPriceKrw = coinData.CurrentPrice;
        } else {
            currentPriceKrw = GlobalEconomyManager.UsdToKrw;
        }

        // 2. 입금 전용 함수 호출 (거래량 증가 안 함)
        PlayerManager.Instance.RegisterTransferIn(coinSymbol, currentPriceKrw, amount);

        // ------------------------------------------------------------------

        ClosePanel();
    }
    private double FloorToTwoDecimal(double value) {
        return Math.Floor(value * 100.0) / 100.0;
    }
}