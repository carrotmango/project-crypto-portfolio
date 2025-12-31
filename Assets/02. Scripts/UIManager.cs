using UnityEngine;

public class UIManager : MonoBehaviour {

    public static UIManager Instance;

    [Header("Confirm Panel")]
    public GameObject ConfrimPanel;
    public Transform uiParent;
    public GameObject InstantEventPanel;
    public GameObject IncomingCallPanel;
    public GameObject SMSResult;


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

    public void ShowSMSResult(string message) {
        if (SMSResult == null) return;

        GameObject instance = Instantiate(SMSResult, uiParent);
        ConfirmPanelController panel = instance.GetComponent<ConfirmPanelController>();

        if (panel != null) {
            panel.SetMessage(message);
        }
    }
    public void ShowConfirmTyping(string title, string message) {
        if (InstantEventPanel == null) return;

        GameObject instance = Instantiate(InstantEventPanel, uiParent);
        ConfirmPanelController panel = instance.GetComponent<ConfirmPanelController>();

        if (panel != null) {
            panel.SetTitle(title);
            StartCoroutine(
                TypingTextPlayer.Play(
                    panel.messageText, 
                    message,
                    0.03f,
                    null               
                )
            );
        }
    }
    public void ShowIncomingCall(
    string title,
    string message
) {
        if (IncomingCallPanel == null) return;

        GameObject instance = Instantiate(IncomingCallPanel, uiParent);
        var panel = instance.GetComponent<IncomingCallPanelController>();

        if (panel != null) {
            panel.Init(
                onYes: () => {
                    // 전화 받음 → 실제 내용 표시
                    ShowConfirmTyping(title, message);
                },
                onNo: () => {
                    // 전화 안 받음 → 아무것도 안 함
                }
            );
        }
    }
}
