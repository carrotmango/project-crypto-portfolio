using UnityEngine;

public class WithdrawPanelController : MonoBehaviour {
    public GameObject withdrawPanel; // 전체 부모 (WithdrawParent)
    public BankTransferUIController bankTransferUIController;

    [SerializeField] private GameObject satoshiBankPanel; // 은행 입금(가져오기) 패널
    [SerializeField] private GameObject cryptoExchangePanel; // 불비트 출금 패널

    // [은행 앱 전용] 불비트에서 돈을 가져오는 패널 열기
    public void OpenPanelForBankDeposit() {
        if (withdrawPanel != null)
            withdrawPanel.SetActive(true);

        // 은행 패널은 켜고, 불비트 출금용 패널은 끕니다.
        if (satoshiBankPanel != null)
            satoshiBankPanel.SetActive(true);

        if (cryptoExchangePanel != null)
            cryptoExchangePanel.SetActive(false);

        CoinManager.Instance.RenderBankCashOnce();

        // 은행 UI 초기화 로직이 있다면 실행
        if (bankTransferUIController != null)
            bankTransferUIController.PrepareForBullbit();
    }

    public void ClosePanel() {
        if (withdrawPanel != null)
            withdrawPanel.SetActive(false);
    }

    public bool IsOpen() {
        return withdrawPanel != null && withdrawPanel.activeSelf;
    }
}