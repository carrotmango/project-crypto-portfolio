using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CapitalDepositPanel : MonoBehaviour {

    [Header("UI")]
    [SerializeField] private Slider depositSlider;
    [SerializeField] private TextMeshProUGUI depositAmountLabel;
    [SerializeField] private Button depositButton;
    [SerializeField] private Button cancelButton;

    // 최소 납입 단위: 10만원
    private const long MIN_UNIT = 100_000;

    private void OnEnable() {
        if (depositSlider == null || depositAmountLabel == null) return;
        InitSlider();
        RefreshAmountText();

        depositSlider.onValueChanged.RemoveAllListeners();
        depositSlider.onValueChanged.AddListener(_ => RefreshAmountText());

        depositButton.onClick.RemoveAllListeners();
        depositButton.onClick.AddListener(OnClickDeposit);

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(Close);
    }

    void InitSlider() {
        if (PlayerManager.Instance == null) {
            depositSlider.interactable = false;
            return;
        }

        long bankCash = (long)PlayerManager.Instance.satoshiBankCash;

        if (bankCash < MIN_UNIT) {
            depositSlider.interactable = false;
            depositSlider.minValue = 0;
            depositSlider.maxValue = 0;
            depositSlider.value = 0;
            return;
        }

        long maxUnits = bankCash / MIN_UNIT;

        depositSlider.interactable = true;
        depositSlider.wholeNumbers = true;
        depositSlider.minValue = 0;
        depositSlider.maxValue = maxUnits;
        depositSlider.value = maxUnits;
        RefreshAmountText();
    }

    void RefreshAmountText() {
        long amount = GetDepositAmount();
        depositAmountLabel.text = $"{amount:N0}원";
    }

    long GetDepositAmount() {
        return (long)depositSlider.value * MIN_UNIT;
    }

    void OnClickDeposit() {
        long amount = GetDepositAmount();
        if (amount <= 0) return;

        if (PlayerManager.Instance == null || OfficeManager.Instance == null) return;

        PlayerManager.Instance.satoshiBankCash -= amount;
        OfficeManager.Instance.AddCapital(amount);

        OfficePanelController officeUI =
            FindFirstObjectByType<OfficePanelController>(FindObjectsInactive.Include);
        if (officeUI != null) {
            officeUI.RefreshAll();
        }

        InitSlider(); // 다음에 다시 열릴 때 값 보장
        Close();
    }


    void Close() {
        gameObject.SetActive(false);
    }
}
