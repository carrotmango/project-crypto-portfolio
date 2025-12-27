using UnityEngine;

public class UIManager : MonoBehaviour {

    public static UIManager Instance;

    [Header("Confirm Panel")]
    public GameObject ConfrimPanel;
    public Transform uiParent;
    public GameObject InstantEventPanel;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    // 새로 추가 (title 있는 컨펌)
    public void ShowConfirm(string title, string message) {
        if (InstantEventPanel == null) return;

        GameObject instance = Instantiate(InstantEventPanel, uiParent);
        ConfirmPanelController panel = instance.GetComponent<ConfirmPanelController>();

        if (panel != null) {
            // title UI가 없는 패널도 안전
            panel.SetTitle(title);
            panel.SetMessage(message);
        }
    }

    public void ShowConfirm(string message) {
        if (ConfrimPanel == null) return;

        GameObject instance = Instantiate(ConfrimPanel, uiParent);
        ConfirmPanelController panel = instance.GetComponent<ConfirmPanelController>();

        if (panel != null) {
            panel.SetMessage(message);
        }
    }
}
