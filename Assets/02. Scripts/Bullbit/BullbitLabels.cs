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

    void RefreshName() {
        userNameLabel.text = $"¾È³çÇÏ¼¼¿ä, {PlayerManager.Instance.playerName}´Ô";
    }

    void Update() {
        double cash = PlayerManager.Instance.bullbitCash;
        cashLabel.text = $"{cash:N0}¿ø";
    }
}
