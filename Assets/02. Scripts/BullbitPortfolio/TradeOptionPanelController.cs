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
        tradeOptionPanel.SetActive(false);

        // 닫기 버튼 리스너 연결
        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HidePanel);
        } else {
            Debug.LogWarning("[TradeOption] CloseButton이 연결되지 않았습니다.");
        }
    }

    public void ShowPanel(string symbol) {
        Debug.Log($"[TradeOption] 패널 열기 시도: {symbol}");
        currentCoin = FindCoin(symbol);
        if (currentCoin == null) {
            Debug.LogWarning($"코인 '{symbol}'을 찾을 수 없습니다.");
            return;
        }

        // 텍스트 설정
        symbolText.text = currentCoin.Symbol;
        nameText.text = currentCoin.Name;

        // 아이콘 설정
        Sprite iconSprite = Resources.Load<Sprite>($"Coins/{currentCoin.Symbol}");
        iconImage.sprite = iconSprite != null ? iconSprite : null;

        // 버튼 리스너 설정
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => {
            HidePanel();
            buyPanel.OpenPanel(currentCoin);
        });

        sellButton.onClick.RemoveAllListeners();
        sellButton.onClick.AddListener(() => {
            HidePanel();
            sellPanel.OpenPanel(currentCoin);
        });

        tradeOptionPanel.SetActive(true);
    }

    public void HidePanel() {
        tradeOptionPanel.SetActive(false);
    }

    private CoinData FindCoin(string symbol) {
        return CoinManager.Instance.coins.Find(c => c.Symbol == symbol);
    }
}
