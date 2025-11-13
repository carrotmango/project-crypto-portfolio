using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public double bullbitCash;
    public double satoshiBankCash;
    public double mangoCasinoCash; // 망고카지노 현금
    public float userStress, userSleep, userHungry;

    public Dictionary<string, double> holdings = new();
    public Dictionary<string, double> totalBuyAmount = new();
    public Dictionary<string, double> totalBuyQuantity = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 테스트 데이터 예시
        // holdings["BTC"] = 1.0;
        // totalBuyAmount["BTC"] = 100000;
        // totalBuyQuantity["BTC"] = 1.0;
    }

    void Update()
    {
        // 추후 로직 삽입 가능
    }

    public void RegisterBuy(string symbol, double price, double quantity)
    {
        if (!holdings.ContainsKey(symbol)) holdings[symbol] = 0;
        if (!totalBuyAmount.ContainsKey(symbol)) totalBuyAmount[symbol] = 0;
        if (!totalBuyQuantity.ContainsKey(symbol)) totalBuyQuantity[symbol] = 0;

        holdings[symbol] += quantity;
        totalBuyAmount[symbol] += price * quantity;
        totalBuyQuantity[symbol] += quantity;
    }

    public void RegisterSell(string symbol, double price, double quantity)
    {
        if (!holdings.ContainsKey(symbol) || holdings[symbol] < quantity) return;

        holdings[symbol] -= quantity;

        if (!totalBuyQuantity.ContainsKey(symbol) || totalBuyQuantity[symbol] <= 0) return;

        double avgPrice = GetAvgPrice(symbol);
        double reduceAmount = avgPrice * quantity;

        totalBuyAmount[symbol] -= reduceAmount;
        totalBuyQuantity[symbol] -= quantity;

        if (holdings[symbol] <= 0)
        {
            holdings[symbol] = 0;
            totalBuyAmount[symbol] = 0;
            totalBuyQuantity[symbol] = 0;
        }
    }

    public double GetAvgPrice(string symbol)
    {
        if (totalBuyAmount.ContainsKey(symbol) && totalBuyQuantity.ContainsKey(symbol))
        {
            double quantity = totalBuyQuantity[symbol];
            if (quantity > 0)
            {
                return totalBuyAmount[symbol] / quantity;
            }
        }
        return 0;
    }

    public double GetBuyTotal(string symbol)
    {
        return totalBuyAmount.TryGetValue(symbol, out double total) ? total : 0;
    }

    public double GetHoldingAmount(string symbol)
    {
        return holdings.TryGetValue(symbol, out double amount) ? amount : 0;
    }

    //총자산 계산: 불비트 + 사토시 은행
    public string GetTotalUserAssetText()
    {
        double bullbit = bullbitCash;
        double bank = satoshiBankCash;
        double total = bullbit + bank;

        return $"총자산 ₩{total:N0} (불비트 ₩{bullbit:N0} / 사토시 ₩{bank:N0})";
    }
}
