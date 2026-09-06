using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmPanelController : MonoBehaviour {
    [Header("UI References")]
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI titleText;
    public Button confirmButton;

    void Awake() {
        if (confirmButton != null) {
            confirmButton.onClick.AddListener(OnConfirm);
        }
    }

    public void SetTitle(string title) {
        if (titleText != null) {
            titleText.text = "-" + title + "-";
        }
    }

    public void SetMessage(string message) {
        if (messageText != null) {
            messageText.text = message;
        }
    }

    public void OnConfirm() {
        Destroy(gameObject);
    }

    void OnDestroy() {
        if (confirmButton != null) {
            confirmButton.onClick.RemoveListener(OnConfirm);
        }
    }
}
