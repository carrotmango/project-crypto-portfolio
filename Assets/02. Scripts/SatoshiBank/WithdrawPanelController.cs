using UnityEngine;

public class WithdrawPanelController : MonoBehaviour {
    public GameObject withdrawPanel;

    // 🔹 열기 (이체 버튼)
    public void OpenPanel() {
        if (withdrawPanel != null)
            withdrawPanel.SetActive(true);
    }

    //  닫기 (X 버튼 또는 다른 메뉴)
    public void ClosePanel() {
        if (withdrawPanel != null)
            withdrawPanel.SetActive(false);
    }
    public bool IsOpen() {
        return withdrawPanel != null && withdrawPanel.activeSelf;
    }

}
