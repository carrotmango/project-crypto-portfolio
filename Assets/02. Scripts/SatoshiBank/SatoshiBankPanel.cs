using TMPro;
using UnityEngine;

public class SatoshiBankPanel : MonoBehaviour
{
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
        userNameLabel.text = $"안녕하세요, {PlayerManager.Instance.playerName}님, 오늘도 좋은 하루 되세요. ";
    }

}
