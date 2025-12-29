using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MainUIManager : MonoBehaviour {
    public GameObject coinRowPrefab;
    public Transform coinListParent;
    public BuyPanelController buyPanelController; // Inspector에서 연결
    public SellPanelController sellPanelController;
    public ChartPanelController chartPanelController; // Inspector에서 연결

    private List<GameObject> coinRows = new();

    void Start() {
        foreach (var coin in CoinManager.Instance.coins) {
            if (coin.IsDelisted)
                continue;

            GameObject row = Instantiate(coinRowPrefab, coinListParent);
            coinRows.Add(row);

            // 기본 UI 세팅
            row.transform.Find("SymbolText").GetComponent<TextMeshProUGUI>().text = coin.Symbol;
            row.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = coin.Name;

            // 코인 로고
            Image icon = row.transform.Find("IconImage")?.GetComponent<Image>();
            if (icon != null) {
                Sprite sprite = Resources.Load<Sprite>($"Coins/{coin.Symbol}");
                if (sprite != null) icon.sprite = sprite;
                else Debug.LogWarning($"[로고 없음] {coin.Symbol} 의 이미지 파일이 Resources/Coins 폴더에 없음");
            }

            // 차트 버튼
            row.transform.Find("ChartBtn").GetComponent<Button>().onClick.AddListener(() => {
                if (chartPanelController != null)
                    chartPanelController.ShowChartPanel(coin);
            });

            // 매수 버튼
            var buyBtn = row.transform.Find("BuyBtn").GetComponent<Button>();
            buyBtn.onClick.AddListener(() => {
                if (buyPanelController != null)
                    buyPanelController.OpenPanel(coin);
            });

            // 튜토리얼용 버튼 전달
            if (TutorialManager.Instance != null) {
                if (coin.Symbol == "BTC") {
                    TutorialManager.Instance.btcBuyButton = buyBtn;
                }else if(coin.Symbol == "ETH") {
                    TutorialManager.Instance.ethBuyButton = buyBtn;
                }
                    

            }

            // 매도 버튼
            row.transform.Find("SellBtn").GetComponent<Button>().onClick.AddListener(() => {
                if (sellPanelController != null)
                    sellPanelController.OpenPanel(coin);
            });
        }
    }
    void Update() {
        foreach (var row in coinRows) {
            var symbol = row.transform
                .Find("SymbolText")
                .GetComponent<TextMeshProUGUI>()
                .text;

            var coin = CoinManager.Instance.coins
                .Find(c => c.Symbol == symbol);

            if (coin == null || coin.IsDelisted)
                continue;

            row.transform.Find("PriceText")
                .GetComponent<TextMeshProUGUI>()
                .text = coin.GetFormattedPriceKRW();

            var changeText = row.transform.Find("ChangeText")
                .GetComponent<TextMeshProUGUI>();

            if (coin.InitialPrice <= 0) {
                changeText.text = "-";
                changeText.color = Color.white;
                continue;
            }

            double change =
                ((coin.CurrentPrice - coin.InitialPrice) / coin.InitialPrice) * 100.0;

            changeText.text = $"{change:+0.##;-0.##}%";

            if (change > 0)
                changeText.color = new Color32(37, 167, 80, 255);
            else if (change < 0)
                changeText.color = new Color32(255, 77, 77, 255);
            else
                changeText.color = Color.white;
        }
    }


    public void AddCoinRow(CoinData coin)
    {
        if (coin.IsDelisted)
            return;

        GameObject row = Instantiate(coinRowPrefab, coinListParent);
        coinRows.Add(row);

        row.transform.Find("SymbolText").GetComponent<TextMeshProUGUI>().text = coin.Symbol;
        row.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = coin.Name;

        Image icon = row.transform.Find("IconImage")?.GetComponent<Image>();
        if (icon != null)
        {
            Sprite sprite = Resources.Load<Sprite>($"Coins/{coin.Symbol}");
            if (sprite != null)
                icon.sprite = sprite;
            else
                Debug.LogWarning($"[로고 없음] {coin.Symbol} 의 이미지 파일이 Resources/Coins 폴더에 없음");
        }

        // 버튼 이벤트 연결
        row.transform.Find("ChartBtn").GetComponent<Button>().onClick.AddListener(() => {
            chartPanelController?.ShowChartPanel(coin);
        });
        row.transform.Find("BuyBtn").GetComponent<Button>().onClick.AddListener(() => {
            buyPanelController?.OpenPanel(coin);
        });
        row.transform.Find("SellBtn").GetComponent<Button>().onClick.AddListener(() => {
            sellPanelController?.OpenPanel(coin);
        });
    }

    public void RefreshCoinRows() {
        // 기존 UI 삭제
        foreach (Transform child in coinListParent)
            Destroy(child.gameObject);

        coinRows.Clear();

        // CoinManager.coins 기준으로 UI 재생성
        foreach (var coin in CoinManager.Instance.coins) {
            AddCoinRow(coin);
        }
    }

}
