using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    
    public string playerName;
    public string birthday;
    public int characterIndex;

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

    public bool RegisterSell(string symbol, double price, ref double quantity)
    {
        const double Epsilon = 0.0000001;

        if (!holdings.ContainsKey(symbol))
        {
            Debug.LogWarning($"[매도 실패] {symbol} 미보유");
            return false;
        }

        double ownedQuantity = holdings[symbol];

        // 판매수량이 보유수량보다 아주 약간만 큰 경우 (부동소수점 오차)
        if (quantity > ownedQuantity && quantity < ownedQuantity + Epsilon)
        {
            quantity = ownedQuantity; // 판매수량을 보유수량으로 조정
        }

        if (ownedQuantity < quantity)
        {
            Debug.LogWarning($"[매도 실패] {symbol} 보유 수량 부족. 보유: {ownedQuantity}, 시도: {quantity}");
            return false;
        }

        if (!totalBuyQuantity.ContainsKey(symbol) || totalBuyQuantity[symbol] <= Epsilon)
        {
            Debug.LogWarning($"[매도 실패] {symbol} 매수 기록 없음");
            return false;
        }

        holdings[symbol] -= quantity;

        double avgPrice = GetAvgPrice(symbol);
        double reduceAmount = avgPrice * quantity;

        totalBuyAmount[symbol] -= reduceAmount;
        totalBuyQuantity[symbol] -= quantity;

        if (holdings[symbol] < Epsilon)
        {
            holdings[symbol] = 0;
            totalBuyAmount[symbol] = 0;
            totalBuyQuantity[symbol] = 0;
        }
        
        return true;
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

    public void ChangeSatoshiMoney(double amount) {
        satoshiBankCash += amount;
    }

    public void ChangeBullbitCash(double amount) {
        bullbitCash += amount;
    }

    public void ChangeCoin(string symbol, double amount) {
        if (!holdings.ContainsKey(symbol)) {
            holdings[symbol] = 0;
            totalBuyAmount[symbol] = 0;
            totalBuyQuantity[symbol] = 0;
        }

        holdings[symbol] += amount;

        // 에어드랍 / 이벤트 지급은
        // 평균단가 0원으로 "매수된 것"처럼 처리
        if (amount > 0) {
            totalBuyQuantity[symbol] += amount;
            // totalBuyAmount는 증가시키지 않음 >> avgPrice = 0
        }

        const double Epsilon = 0.0000001;
        if (Mathf.Abs((float)holdings[symbol]) < Epsilon) {
            holdings[symbol] = 0;
            totalBuyAmount[symbol] = 0;
            totalBuyQuantity[symbol] = 0;
        }
    }

}
