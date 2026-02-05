using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TradeOptionPanelController : MonoBehaviour {
    [Header("연결 패널들")]
    public GameObject tradeOptionPanel;
    public BuyPanelController buyPanel;
    public SellPanelController sellPanel;

    [Header("UI 요소")]
    public TextMeshProUGUI symbolText;
    public TextMeshProUGUI nameText;
    public Image iconImage;

    public Button buyButton;
    public Button sellButton;
    public Button closeButton;

    private CoinData currentCoin;

    private void Awake() {
        // 시작할 때는 꺼두기
        if (tradeOptionPanel != null) tradeOptionPanel.SetActive(false);

        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HidePanel);
        }
    }

    public void ShowPanel(string symbol) {
        // 코인 정보 찾기
        currentCoin = CoinManager.Instance.coins.Find(c => c.Symbol == symbol);
        if (currentCoin == null) return;

        // UI 갱신
        if (symbolText) symbolText.text = currentCoin.Symbol;
        if (nameText) nameText.text = currentCoin.Name;

        Sprite iconSprite = Resources.Load<Sprite>($"Coins/{currentCoin.Symbol}");
        if (iconImage) iconImage.sprite = iconSprite != null ? iconSprite : null;

        // 버튼 리스너 (매수/매도 창으로 넘어가기)
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => {
            HidePanel(); // 옵션창 닫고
            buyPanel.OpenPanel(currentCoin); // 매수창 열기
        });

        sellButton.onClick.RemoveAllListeners();
        sellButton.onClick.AddListener(() => {
            HidePanel();
            sellPanel.OpenPanel(currentCoin);
        });

        // [중요] 패널을 켜고, 화면 맨 앞으로 가져오기
        tradeOptionPanel.SetActive(true);
        tradeOptionPanel.transform.SetAsLastSibling();
    }

    public void HidePanel() {
        if (tradeOptionPanel) tradeOptionPanel.SetActive(false);
    }
}