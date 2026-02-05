using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeverageSelectorUI : MonoBehaviour {

    [Header("UI Components")]
    public Slider slider;
    public TextMeshProUGUI valueText; // "5x" 표시

    // 설명 텍스트(limitInfoText) 제거함

    public Button confirmButton;
    public Button closeButton;

    private FutureChartRenderer manager;
    public TextMeshProUGUI maxRangeLabel;

    // 패널 열 때 초기화
    public void Show(FutureChartRenderer renderer, float currentLev) {
        manager = renderer;

        // 1. 내 직급에 맞는 최대 레버리지 가져오기
        int maxLeverage = 1;
        if (OfficeManager.Instance != null) {
            maxLeverage = OfficeManager.Instance.GetMaxLeverage();
        }

        // 2. 슬라이더 최대값을 직급 한계치로 설정
        // (예: 차장이면 5, 부장이면 20으로 슬라이더 길이가 바뀜)
        slider.minValue = 1;
        slider.maxValue = maxLeverage;
        slider.wholeNumbers = true;

        if (maxRangeLabel != null) {
            maxRangeLabel.text = $"x{maxLeverage}";
        }

        // 3. 현재 설정값이 최대치를 넘지 않도록 안전장치
        if (currentLev > maxLeverage) currentLev = maxLeverage;

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

            // (선택사항) 배율에 따른 색상 변화는 남겨두었습니다 (직관적임)
            if (val >= 50) valueText.color = Color.red;          // 초고배율
            else if (val >= 20) valueText.color = new Color(1f, 0.5f, 0f); // 고배율 (주황)
            else valueText.color = Color.white;                  // 일반
        }
    }

    private void OnConfirmClicked() {
        if (manager != null) {
            manager.SetLeverage(slider.value);
        }
        gameObject.SetActive(false);
    }
}