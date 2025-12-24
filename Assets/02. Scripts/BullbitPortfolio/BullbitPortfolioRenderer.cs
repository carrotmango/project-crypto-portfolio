using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

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

    [Header("Trade Option Panel")]
    public TradeOptionPanelController tradeOptionPanelController;

    private float timer = 0f;

    void OnEnable() {
        RenderPortfolioRows(); // 패널이 켜질 때 즉시 반영
    }

    void Update() {
        if (!gameObject.activeSelf) return;

        timer += Time.deltaTime;
        float interval = CoinManager.Instance.GetUpdateInterval(); // 배속 반영된 시간
        if (timer >= interval) {
            timer = 0f;
            RenderPortfolioRows();
        }
    }

    public void RenderPortfolioRows() {
        double totalBuy = 0;
        double totalEval = 0;

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (TutorialManager.Instance != null) {
            TutorialManager.Instance.portfolioCoinSymbolTexts.Clear();
            TutorialManager.Instance.portfolioCoinLabelTexts.Clear();
        }

        foreach (var kv in PlayerManager.Instance.holdings) {
            string symbol = kv.Key;
            double amount = kv.Value;

            if (amount <= 0) continue;

            bool listed = CoinManager.Instance.IsCoinListedOnBullbit(symbol);

            CoinData coin = null;
            if (listed) {
                coin = CoinManager.Instance.coins
                    .Find(c => c.Symbol == symbol);
            }

            GameObject row = Instantiate(portfolioCoinRowPrefab, contentParent);

            // >> 아이콘
            Image icon = row.transform.Find("CoinIcon").GetComponent<Image>();
            Sprite loadedSprite = Resources.Load<Sprite>($"Coins/{symbol}");
            if (loadedSprite != null) icon.sprite = loadedSprite;

            var meta = Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);

            // >> 기본 텍스트
            row.transform.Find("CoinSymbol").GetComponent<TextMeshProUGUI>().text = symbol;
            row.transform.Find("CoinNameText").GetComponent<TextMeshProUGUI>().text =
                meta != null ? meta.Name : symbol;

            row.transform.Find("HoldingAmountText")
                .GetComponent<TextMeshProUGUI>().text = amount.ToString("N4");

            // 거래 패널 클릭
            var symbolBtn = row.transform.Find("CoinSymbol").GetComponent<Button>();
            var nameBtn = row.transform.Find("CoinNameText").GetComponent<Button>();

            //  기본값은 항상 비활성
            symbolBtn.interactable = false;
            nameBtn.interactable = false;

            // >> 상장된 경우에만 활성
            if (listed && tradeOptionPanelController != null) {
                symbolBtn.onClick.RemoveAllListeners();
                symbolBtn.onClick.AddListener(() => tradeOptionPanelController.ShowPanel(symbol));
                symbolBtn.interactable = true;

                nameBtn.onClick.RemoveAllListeners();
                nameBtn.onClick.AddListener(() => tradeOptionPanelController.ShowPanel(symbol));
                nameBtn.interactable = true;
            }


            // >> 가격 / 평가 계산
            double avgPrice = PlayerManager.Instance.GetAvgPrice(symbol);
            double buyTotal = avgPrice * amount;

            double evalTotal = listed && coin != null
                ? coin.CurrentPrice * amount
                : 0;

            double profitLoss = evalTotal - buyTotal;
            double returnRate = buyTotal > 0 ? (profitLoss / buyTotal) * 100 : 0;

            row.transform.Find("BuyAvgPriceText")
                .GetComponent<TextMeshProUGUI>().text =
                listed ? FormatPriceKRW(avgPrice) : "-";

            row.transform.Find("BuyTotalText")
                .GetComponent<TextMeshProUGUI>().text =
                listed ? buyTotal.ToString("N0") : "-";

            row.transform.Find("EvalTotalText")
                .GetComponent<TextMeshProUGUI>().text =
                listed ? evalTotal.ToString("N0") : "-";

            var profitText = row.transform.Find("ProfitLossText").GetComponent<TextMeshProUGUI>();
            var rateText = row.transform.Find("ProfitLossPercentText").GetComponent<TextMeshProUGUI>();

            if (listed) {
                profitText.text = profitLoss >= 0 ? $"+{profitLoss:N0}" : profitLoss.ToString("N0");
                profitText.color = profitLoss >= 0 ? Color.green : Color.red;

                rateText.text = returnRate >= 0 ? $"+{returnRate:F2}%" : $"{returnRate:F2}%";
                rateText.color = returnRate >= 0 ? Color.green : Color.red;
            } else {
                // << 상장 전 표시
                profitText.text = "-";
                profitText.color = Color.gray;

                rateText.text = "상장 예정";
                rateText.color = Color.gray;
            }

            // >> 합계는 상장 코인만 반영
            if (listed) {
                totalBuy += buyTotal;
                totalEval += evalTotal;
            }
        }

        double cash = PlayerManager.Instance.bullbitCash;
        double totalAsset = cash + totalEval;
        double totalProfit = totalEval - totalBuy;
        double totalReturnRate = (totalBuy > 0) ? (totalProfit / totalBuy) * 100 : 0;

        // 요약 UI 반영
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
