using TMPro;
using UnityEngine;

public class BullbitLabels : MonoBehaviour {
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI cashLabel;
    [SerializeField] private TextMeshProUGUI userNameLabel;

    void Start() {
        userNameLabel.text = $"æ»≥Á«œººø‰, {PlayerManager.Instance.playerName}¥‘";
    }

    void Update() {
        double cash = PlayerManager.Instance.bullbitCash;
        cashLabel.text = $"{cash:N0}";
    }
}
