using UnityEngine;

public class UIManager : MonoBehaviour
{

    public static UIManager Instance;

    [Header("Confirm Panel")]
    public GameObject ConfrimPanel;
    public Transform uiParent;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public void ShowConfirm(string message) {
        if ((ConfrimPanel == null)) {
            return;
        }
        GameObject instance = Instantiate(ConfrimPanel, uiParent);
        ConfirmPanelController panel = instance.GetComponent<ConfirmPanelController>();
        if (panel != null) {
            panel.SetMessage(message);
        }
    }
}
