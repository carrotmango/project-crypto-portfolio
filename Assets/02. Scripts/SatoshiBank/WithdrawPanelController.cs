using UnityEngine;

public class WithdrawPanelController : MonoBehaviour {
    public GameObject withdrawPanel;
    public BankTransferUIController bankTransferUIController;
    [SerializeField] private GameObject satoshiBankPanel;

    // 열기 (이체 버튼)
    public void OpenPanel() {
        if (withdrawPanel != null)
            withdrawPanel.SetActive(true);
    }

    void OpenSatoshiPanel() {
        satoshiBankPanel.SetActive(true);
        CoinManager.Instance.RenderBankCashOnce();
    }


    public void ClosePanel() {
        if (withdrawPanel != null)
            withdrawPanel.SetActive(false);
    }
    public bool IsOpen() {
        return withdrawPanel != null && withdrawPanel.activeSelf;
    }
    public void OpenPanelForBullbit() {
        if (withdrawPanel != null)
            withdrawPanel.SetActive(true);

        if (satoshiBankPanel != null)
            satoshiBankPanel.SetActive(true);

        CoinManager.Instance.RenderBankCashOnce();

        if (bankTransferUIController != null)
            bankTransferUIController.PrepareForBullbit();
    }


}
