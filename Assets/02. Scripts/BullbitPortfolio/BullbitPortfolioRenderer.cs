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

        foreach (var coin in CoinManager.Instance.coins) {
            if (!PlayerManager.Instance.holdings.TryGetValue(coin.Symbol, out double amount) || amount <= 0)
                continue;

            GameObject row = Instantiate(portfolioCoinRowPrefab, contentParent);
            string coinSymbol = coin.Symbol;

            // CoinSymbol 클릭 시 패널 열기
            var symbolBtn = row.transform.Find("CoinSymbol").GetComponent<Button>();
            if (symbolBtn != null && tradeOptionPanelController != null) {
                symbolBtn.onClick.RemoveAllListeners();
                symbolBtn.onClick.AddListener(() => {
                    Debug.Log($"[디버그] CoinSymbol 버튼 클릭됨: {coinSymbol}");
                    tradeOptionPanelController.ShowPanel(coinSymbol);
                });
            }

            // CoinNameText 클릭 시 패널 열기
            var nameBtn = row.transform.Find("CoinNameText").GetComponent<Button>();
            if (nameBtn != null && tradeOptionPanelController != null) {
                nameBtn.onClick.RemoveAllListeners();
                nameBtn.onClick.AddListener(() => {
                    Debug.Log($"[디버그] CoinNameText 버튼 클릭됨: {coinSymbol}");
                    tradeOptionPanelController.ShowPanel(coinSymbol);
                });
            }

            // 아이콘 설정
            Image icon = row.transform.Find("CoinIcon").GetComponent<Image>();
            Sprite loadedSprite = Resources.Load<Sprite>($"Coins/{coin.Symbol}");
            if (loadedSprite != null) icon.sprite = loadedSprite;

            // 텍스트 설정
            row.transform.Find("CoinSymbol").GetComponent<TextMeshProUGUI>().text = coin.Symbol;
            row.transform.Find("CoinNameText").GetComponent<TextMeshProUGUI>().text = coin.Name;
            row.transform.Find("HoldingAmountText").GetComponent<TextMeshProUGUI>().text = amount.ToString("N4");

            double avgPrice = PlayerManager.Instance.GetAvgPrice(coin.Symbol);
            double buyTotal = avgPrice * amount;
            double evalTotal = coin.CurrentPrice * amount;
            double profitLoss = evalTotal - buyTotal;
            double returnRate = (buyTotal > 0) ? (profitLoss / buyTotal) * 100 : 0;

            row.transform.Find("BuyAvgPriceText").GetComponent<TextMeshProUGUI>().text = FormatPriceKRW(avgPrice);
            row.transform.Find("BuyTotalText").GetComponent<TextMeshProUGUI>().text = buyTotal.ToString("N0");
            row.transform.Find("EvalTotalText").GetComponent<TextMeshProUGUI>().text = evalTotal.ToString("N0");

            var profitText = row.transform.Find("ProfitLossText").GetComponent<TextMeshProUGUI>();
            profitText.text = profitLoss >= 0 ? $"+{profitLoss:N0}" : profitLoss.ToString("N0");
            profitText.color = profitLoss >= 0 ? Color.green : Color.red;

            var rateText = row.transform.Find("ProfitLossPercentText").GetComponent<TextMeshProUGUI>();
            rateText.text = returnRate >= 0 ? $"+{returnRate:F2}%" : $"{returnRate:F2}%";
            rateText.color = returnRate >= 0 ? Color.green : Color.red;

            totalBuy += buyTotal;
            totalEval += evalTotal;

            if (TutorialManager.Instance != null) {
                var symbolText = row.transform.Find("CoinSymbol")?.GetComponent<TextMeshProUGUI>();
                var nameText = row.transform.Find("CoinNameText")?.GetComponent<TextMeshProUGUI>();

                if (symbolText != null)
                    TutorialManager.Instance.portfolioCoinSymbolTexts.Add(symbolText);
                if (nameText != null)
                    TutorialManager.Instance.portfolioCoinLabelTexts.Add(nameText);
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
