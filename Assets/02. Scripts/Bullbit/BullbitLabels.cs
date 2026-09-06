using TMPro;
using UnityEngine;

public class BullbitLabels : MonoBehaviour {
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI cashLabel;
    [SerializeField] private TextMeshProUGUI userNameLabel;

    void Start() {
        PlayerManager.Instance.OnPlayerNameChanged += RefreshName;
        RefreshName();
    }

    void OnDestroy() {
        if (PlayerManager.Instance != null) {
            PlayerManager.Instance.OnPlayerNameChanged -= RefreshName;
        }
    }

    // 이름 갱신 (안녕하세요, OOO님 / Hello, OOO)
    void RefreshName() {
        string format = LocalizationManager.GetText("LBL_BULLBIT_WELCOME");
        userNameLabel.text = string.Format(format, PlayerManager.Instance.playerName);
    }

    void Update() {
        double cash = PlayerManager.Instance.bullbitCash;

        // "원" 또는 " Won"을 가져와서 숫자 뒤에 붙임
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");
        cashLabel.text = $"{cash:N0}{unit}";
    }
}