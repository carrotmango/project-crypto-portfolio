using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class BullbitPortfolioRenderer : MonoBehaviour {
    [Header("Prefab & Content")]
    public GameObject portfolioCoinRowPrefab;
    public Transform contentParent;

    [Header("Summary UI")]
    public TextMeshProUGUI cashKRWText;
    public TextMeshProUGUI totalAssetText;
    public TextMeshProUGUI totalBuyText;
    public TextMeshProUGUI totalEvalText;
    public TextMeshProUGUI totalProfitText;
    public TextMeshProUGUI totalProfitRateText;

    [Header("Detail Panel Manager")]
    public BullBitDetailTradeManager detailTradeManager;

    [Header("Sell All")]
    public Button sellAllButton;

    private float timer = 0f;
    private Dictionary<string, GameObject> rowMap = new Dictionary<string, GameObject>();

    void OnEnable() {
        RenderPortfolioRows();
    }

    void Start() {
        if (sellAllButton != null) {
            sellAllButton.onClick.AddListener(OnSellAllButtonClicked);
        }
    }

    public void OnSellAllButtonClicked() {
        PlayerManager.Instance.SellAllListedCoins();
        RenderPortfolioRows();
    }

    void Update() {
        if (!gameObject.activeSelf) return;

        timer += Time.deltaTime;
        float interval = CoinManager.Instance.GetUpdateInterval();
        if (timer >= interval) {
            timer = 0f;
            RenderPortfolioRows();
        }
    }

    public void RenderPortfolioRows() {
        double totalBuy = 0;
        double totalEval = 0;

        if (TutorialManager.Instance != null) {
            TutorialManager.Instance.portfolioCoinSymbolTexts.Clear();
            TutorialManager.Instance.portfolioCoinLabelTexts.Clear();
        }

        HashSet<string> currentHoldings = new HashSet<string>();
        foreach (var kv in PlayerManager.Instance.holdings) {
            if (kv.Value > 0) currentHoldings.Add(kv.Key);
        }

        List<string> keysToRemove = new List<string>();
        foreach (var kvp in rowMap) {
            if (!currentHoldings.Contains(kvp.Key)) {
                Destroy(kvp.Value);
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove) {
            rowMap.Remove(key);
        }

        foreach (var kv in PlayerManager.Instance.holdings) {
            string symbol = kv.Key;
            double amount = kv.Value;

            if (amount <= 0) continue;

            CoinData coin = CoinManager.Instance.coins.Find(c => c.Symbol == symbol);
            bool listed = (coin != null && coin.IsListed && !coin.IsDelisted);

            GameObject row;

            if (!rowMap.TryGetValue(symbol, out row)) {
                row = Instantiate(portfolioCoinRowPrefab, contentParent);
                rowMap.Add(symbol, row);

                Image icon = row.transform.Find("CoinIcon").GetComponent<Image>();
                Sprite loadedSprite = Resources.Load<Sprite>($"Coins/{symbol}");
                if (loadedSprite != null) icon.sprite = loadedSprite;

                var meta = Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);

                row.transform.Find("CoinSymbol").GetComponent<TextMeshProUGUI>().text = symbol;
                row.transform.Find("CoinNameText").GetComponent<TextMeshProUGUI>().text = meta != null ? meta.Name : symbol;

                CoinData targetCoin = coin;
                Transform hitBox = row.transform.Find("Image");
                if (hitBox != null) {
                    Button hitBtn = hitBox.GetComponent<Button>();
                    if (hitBtn != null) {
                        hitBtn.onClick.RemoveAllListeners();
                        hitBtn.onClick.AddListener(() => {
                            Debug.Log($"[디버그] {targetCoin.Symbol} 코인 클릭! 디테일 패널 열고 포트폴리오 패널은 닫습니다.");

                            // 1. 디테일 패널 열기
                            if (detailTradeManager != null) {
                                detailTradeManager.OpenDetailPanel(targetCoin);
                            }

                            // 2. 포트폴리오 패널 & 자산 패널 모두 닫기 (새로 추가한 로직)
                            TotalAssetPanelController assetCtrl = FindFirstObjectByType<TotalAssetPanelController>();
                            if (assetCtrl != null) {
                                assetCtrl.CloseAllAssetPanels();
                            }
                        });
                    }
                }
            }

            Transform updateHitBox = row.transform.Find("Image");
            if (updateHitBox != null) {
                Button b = updateHitBox.GetComponent<Button>();
                if (b != null) b.interactable = listed;
            }

            row.transform.Find("HoldingAmountText").GetComponent<TextMeshProUGUI>().text = amount.ToString("N4");

            double avgPrice = PlayerManager.Instance.GetAvgPrice(symbol);
            double buyTotal = avgPrice * amount;

            double evalTotal = 0;
            if (listed && coin != null) {
                evalTotal = coin.CurrentPrice * amount;
            }

            double profitLoss = evalTotal - buyTotal;
            double returnRate = buyTotal > 0 ? (profitLoss / buyTotal) * 100 : 0;

            row.transform.Find("BuyAvgPriceText").GetComponent<TextMeshProUGUI>().text = listed ? FormatPriceKRW(avgPrice) : "-";
            row.transform.Find("BuyTotalText").GetComponent<TextMeshProUGUI>().text = listed ? buyTotal.ToString("N0") : "-";
            row.transform.Find("EvalTotalText").GetComponent<TextMeshProUGUI>().text = listed ? evalTotal.ToString("N0") : "-";

            var profitText = row.transform.Find("ProfitLossText").GetComponent<TextMeshProUGUI>();
            var rateText = row.transform.Find("ProfitLossPercentText").GetComponent<TextMeshProUGUI>();

            if (listed) {
                profitText.text = profitLoss >= 0 ? $"+{profitLoss:N0}" : profitLoss.ToString("N0");
                profitText.color = profitLoss >= 0 ? Color.green : Color.red;

                rateText.text = returnRate >= 0 ? $"+{returnRate:F2}%" : $"{returnRate:F2}%";
                rateText.color = returnRate >= 0 ? Color.green : Color.red;
            } else {
                string unlistedStr = LocalizationManager.GetText("LBL_UNLISTED");

                profitText.text = unlistedStr;
                profitText.color = Color.gray;

                rateText.text = "";
                rateText.color = Color.gray;
            }

            if (listed) {
                totalBuy += buyTotal;
                totalEval += evalTotal;
            }
        }

        double cash = PlayerManager.Instance.bullbitCash;
        double totalAsset = cash + totalEval;
        double totalProfit = totalEval - totalBuy;
        double totalReturnRate = (totalBuy > 0) ? (totalProfit / totalBuy) * 100 : 0;

        cashKRWText.text = $"{cash:N0}";
        totalAssetText.text = $"{totalAsset:N0}";
        totalBuyText.text = $"{totalBuy:N0}";
        totalEvalText.text = $"{totalEval + cash:N0}";

        if (totalProfit > 0) {
            totalProfitText.text = $"+{totalProfit:N0}";
            totalProfitText.color = Color.green;
        } else if (totalProfit < 0) {
            totalProfitText.text = totalProfit.ToString("N0");
            totalProfitText.color = Color.red;
        } else {
            totalProfitText.text = "0";
            totalProfitText.color = Color.white;
        }

        if (totalBuy <= 0) {
            totalProfitRateText.text = "0.00%";
            totalProfitRateText.color = Color.white;
        } else {
            string sign = totalReturnRate > 0 ? "+" : "";
            totalProfitRateText.text = $"{sign}{totalReturnRate:F2}%";
            totalProfitRateText.color = totalReturnRate > 0 ? Color.green :
                                         totalReturnRate < 0 ? Color.red : Color.white;
        }
    }

    private string FormatPriceKRW(double price) {
        if (price >= 1000)
            return price.ToString("N0");
        else if (price >= 100)
            return price.ToString("N2");
        else if (price >= 10)
            return price.ToString("N3");
        else
            return price.ToString("N4");
    }
}