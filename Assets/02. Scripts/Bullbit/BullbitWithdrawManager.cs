using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BullbitWithdrawManager : MonoBehaviour {
    [Header("패널 관련")]
    public GameObject bullbitWithdrawPanel;
    public Button toggleButton;           // 열기/닫기 토글 버튼
    public Button closeButtonInPanel;     // X 닫기 버튼

    [Header("입력 UI")]
    public TMP_InputField withdrawInput;
    public Button withdrawButton;
    public Button allButton;

    private const double minimumWithdraw = 5000;
    private const double fee = 1000;
    private const double epsilon = 0.0001; // 실수 오차 방지용

    void Start() {
        toggleButton.onClick.AddListener(ToggleBullbitWithdrawPanel);
        closeButtonInPanel.onClick.AddListener(CloseBullbitWithdrawPanel);

        withdrawInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        withdrawInput.onValueChanged.AddListener(OnValueChanged);
        withdrawButton.onClick.AddListener(OnClickWithdraw);
        allButton.onClick.AddListener(OnClickAll);
        withdrawButton.interactable = false;
    }

    public void ToggleBullbitWithdrawPanel() {
        bool isActive = bullbitWithdrawPanel.activeSelf;
        bullbitWithdrawPanel.SetActive(!isActive);

        if (!isActive) {
            withdrawInput.text = "";
            withdrawButton.interactable = false;
        }
    }

    public void CloseBullbitWithdrawPanel() {
        bullbitWithdrawPanel.SetActive(false);
    }

    void OnValueChanged(string input) {
        string raw = input.Replace(",", "");
        if (double.TryParse(raw, out double value)) {
            double available = PlayerManager.Instance.bullbitCash;

            // 실수 오차 허용 및 조건 보정
            withdrawButton.interactable = value >= minimumWithdraw && (value + fee) <= available + epsilon;
        } else {
            withdrawButton.interactable = false;
        }
    }

    public void OnClickWithdraw() {
        string raw = withdrawInput.text.Replace(",", "");
        if (!double.TryParse(raw, out double amount)) return;

        double available = PlayerManager.Instance.bullbitCash;

        // 강제로 정수화 (원 단위)
        amount = Math.Floor(amount);

        // 실수 오차 보정: 조건이 살짝 안 맞을 때 자동으로 1원 덜 출금
        if ((amount + fee) > available && (amount + fee - available) < 2) {
            amount = Math.Floor(available - fee - 1);
        }

        if (amount < minimumWithdraw) {
            Debug.LogWarning("출금 금액이 최소 출금액 미만입니다.");
            return;
        }

        PlayerManager.Instance.bullbitCash -= (amount + fee);
        PlayerManager.Instance.satoshiBankCash += amount;

        CoinManager.Instance.UpdateCashText();
        withdrawInput.text = "";
        withdrawButton.interactable = false;

        Debug.Log($"불비트 현금을 사토시뱅크로 {amount:N0}원 이체 완료 (수수료 {fee:N0}원 차감)");
        CloseBullbitWithdrawPanel();
    }

    public void OnClickAll() {
        double available = PlayerManager.Instance.bullbitCash;
        double max = available - fee;

        // 소수점 오차나 조건 미세 불일치 시 1원 자동 남기기
        if (max < minimumWithdraw) {
            withdrawInput.text = "";
            withdrawButton.interactable = false;
            return;
        }

        if ((max + fee) > available && (max + fee - available) < 2) {
            max -= 1;
        }

        withdrawInput.text = Math.Floor(max).ToString("N0");
        withdrawButton.interactable = true;
    }
}
