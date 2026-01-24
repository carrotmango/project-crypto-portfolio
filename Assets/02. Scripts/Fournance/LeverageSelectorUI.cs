using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeverageSelectorUI : MonoBehaviour {

    [Header("UI Components")]
    public Slider slider;
    public TextMeshProUGUI valueText; // "5x" 처럼 표시될 텍스트
    public Button confirmButton;      // 완료 버튼
    public Button closeButton;        // (선택) 닫기/배경 버튼

    private FutureChartRenderer manager;

    // 패널 열 때 초기화
    public void Show(FutureChartRenderer renderer, float currentLev) {
        manager = renderer;

        // 슬라이더 설정
        slider.minValue = 1;
        slider.maxValue = 100; // 최대 100배
        slider.wholeNumbers = true;
        slider.value = currentLev;

        UpdateText(currentLev);

        // 이벤트 연결
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(OnSliderChanged);

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirmClicked);

        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        gameObject.SetActive(true);
    }

    private void OnSliderChanged(float val) {
        UpdateText(val);
    }

    private void UpdateText(float val) {
        if (valueText != null) {
            valueText.text = $"{val:F0}x";
        }
    }

    private void OnConfirmClicked() {
        if (manager != null) {
            // 매니저에게 변경된 레버리지 전달
            manager.SetLeverage(slider.value);
        }
        gameObject.SetActive(false);
    }
}