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

    // --------------------------------------------------------------------------
    // [핵심 함수 1] 실제 출금 가능한 금액 계산 (Buying Power 기준)
    // --------------------------------------------------------------------------
    private double GetWithdrawableAmount() {
        double buyingPower = 0;

        if (FutureChartRenderer.Instance != null) {
            // 차트 씬이면: 손실분 등을 제외한 '진짜 뺄 수 있는 돈'을 가져옴
            buyingPower = FutureChartRenderer.Instance.GetBuyingPower();
        } else {
            // 로비 등 다른 곳이면 그냥 현금 잔고
            if (PlayerManager.Instance != null)
                buyingPower = PlayerManager.Instance.fournanceCash;
        }

        return buyingPower;
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

        // 수수료 제외
        double maxAmt = Math.Max(0, withdrawableAmount - currentFee);
        amountInput.text = maxAmt.ToString("F2");
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
    void OnClickWithdraw() {
        if (!double.TryParse(amountInput.text, out double amount)) return;

        // [최종 검증] 한 번 더 Buying Power 체크 (해킹 방지)
        double withdrawableAmount = GetWithdrawableAmount();
        double totalCost = amount + currentFee;

        if (totalCost > withdrawableAmount) {
            Debug.LogError("출금 가능 금액 부족!");
            // 여기에 알림 팝업 띄우기
            return;
        }

        // A. 선물 지갑($)에서 차감
        PlayerManager.Instance.fournanceCash -= totalCost;

        // B. 현물 지갑(Coin)에 추가 (1:1 비율)
        string coinSymbol = coinDropdown.options[coinDropdown.value].text;
        PlayerManager.Instance.ChangeCoin(coinSymbol, amount);

        Debug.Log($"[Transfer] FourNance($) -> Bullbit({coinSymbol}): {amount}");

        // C. UI 갱신
        if (FourNanceManager.Instance != null) {
            FourNanceManager.Instance.RefreshUI();
        }

        ClosePanel();
    }
}