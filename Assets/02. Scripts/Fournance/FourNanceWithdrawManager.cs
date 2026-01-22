using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class FourNanceWithdrawManager : MonoBehaviour {
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

    private double currentFee = 0; // 내부 이체라 수수료 0원 (필요시 변경)
    private double minWithdraw = 10; // 최소 출금 $10

    void Start() {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (withdrawButton != null) withdrawButton.onClick.AddListener(OnClickWithdraw);
        if (maxAmountButton != null) maxAmountButton.onClick.AddListener(OnClickAll);

        if (amountInput != null) amountInput.onValueChanged.AddListener(OnAmountChanged);
        if (coinDropdown != null) coinDropdown.onValueChanged.AddListener(delegate { RefreshOptions(); });

        SetupDropdowns();
    }

    public void OpenPanel() {
        withdrawPanel.SetActive(true);
        SetupDropdowns(); // 초기화
        RefreshUI();
    }

    public void ClosePanel() {
        withdrawPanel.SetActive(false);
    }

    // 1. 드롭다운 초기화 (USDT, USDC / 불비트 고정)
    void SetupDropdowns() {
        // 코인 설정
        coinDropdown.ClearOptions();
        coinDropdown.AddOptions(new List<string> { "USDT", "USDC" });

        // 플랫폼 설정 (불비트 고정)
        platformDropdown.ClearOptions();
        platformDropdown.AddOptions(new List<string> { "불비트 거래소 (현물지갑)" });
        platformDropdown.interactable = false; // 변경 불가

        RefreshOptions();
    }

    // 2. 코인 선택에 따른 네트워크/주소 자동 설정
    void RefreshOptions() {
        string selectedCoin = coinDropdown.options[coinDropdown.value].text;
        networkDropdown.ClearOptions();

        if (selectedCoin == "USDT") {
            networkDropdown.AddOptions(new List<string> { "TRON (TRC-20)" });
            addressInput.text = "T-Bullbit-HotWallet-Deposit"; // 불비트 수신 주소
        } else if (selectedCoin == "USDC") {
            networkDropdown.AddOptions(new List<string> { "Arbitrum One" });
            addressInput.text = "0x-Bullbit-HotWallet-Deposit"; // 불비트 수신 주소
        }

        addressInput.interactable = false; // 주소 수정 금지
        RefreshUI();
    }

    // 3. UI 갱신 (잔고 표시 등)
    void RefreshUI() {
        if (PlayerManager.Instance == null) return;

        double myDollar = PlayerManager.Instance.fournanceCash;
        if (availableBalanceText != null) {
            availableBalanceText.text = $"출금 가능: ${myDollar:N2}";
        }

        // 수수료 안내
        if (noticeText != null) {
            noticeText.text = $"불비트 내부 이체 수수료: ${currentFee} (무료)";
        }
    }

    // 4. 전액 버튼 로직
    void OnClickAll() {
        double myDollar = PlayerManager.Instance.fournanceCash;
        double maxAmt = Math.Max(0, myDollar - currentFee);
        amountInput.text = maxAmt.ToString("F2"); // 소수점 2자리
    }

    // 5. 금액 입력 감지 (유효성 검사)
    void OnAmountChanged(string val) {
        if (double.TryParse(val, out double amount)) {
            double myDollar = PlayerManager.Instance.fournanceCash;
            // 최소 금액 이상 && 잔고 충분
            bool isValid = (amount >= minWithdraw) && (amount + currentFee <= myDollar);
            withdrawButton.interactable = isValid;
        } else {
            withdrawButton.interactable = false;
        }
    }

    // 6. [핵심] 출금 실행 로직
    void OnClickWithdraw() {
        if (!double.TryParse(amountInput.text, out double amount)) return;

        // A. 선물 지갑($)에서 차감
        PlayerManager.Instance.fournanceCash -= (amount + currentFee);

        // B. 현물 지갑(Coin)에 추가 (1:1 비율)
        string coinSymbol = coinDropdown.options[coinDropdown.value].text; // USDT or USDC
        PlayerManager.Instance.ChangeCoin(coinSymbol, amount);

        Debug.Log($"[Transfer] FourNance($) -> Bullbit({coinSymbol}): {amount}");

        // C. UI 갱신
        if (FutureChartRenderer.Instance != null) {
            // 차트 쪽 잔고 UI 등 갱신 유도 (필요시)
        }

        ClosePanel();
    }
}