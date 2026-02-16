using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour {
    public static PlayerManager Instance { get; private set; }

    public string playerName;
    public string birthday;
    public int characterIndex;

    public double bullbitCash;
    public double satoshiBankCash;
    public double mangoCasinoCash;
    public double fournanceCash;
    public float userStress, userSleep, userHungry;

    [Header("누적 통계")]
    public double totalTradeVolume = 0;
    public double realizedProfit = 0;
    public double totalBullbitDeposit = 0;
    public double totalFeePaid = 0;
    public long totalSalaryReceived = 0;      // 누적 수령 급여
    public int totalPartTimeJobCount = 0;     // 누적 알바 횟수
    public long totalPartTimeJobIncome = 0;   // 누적 알바 수입
    public int totalLotteryTicketCount = 0;   // 누적 복권 구매 수
    public long totalLotterySpentAmount = 0;  // 누적 복권 구매 금액
    public long totalLotteryWonAmount = 0;    // 누적 복권 당첨 금액
    public long totalRealEstateIncome = 0;

    [Header("Fournance (선물) 전용 누적 통계")]
    public double fournanceTotalVolume = 0;   // 누적 거래량 (레버리지 포함 금액)
    public double fournanceTotalFee = 0;      // 누적 수수료 (진입+종료)
    public double fournanceRealizedPnL = 0;   // 누적 실현 손익 (확정된 수익/손실)
    [Header("오락실(Gamble) 누적 통계")]
    public long totalGambleSpent = 0;  // 총 베팅 금액 (지출)
    public long totalGambleEarned = 0; // 총 당첨 금액 (수입)

    public event Action OnStatsChanged;
 

    public Dictionary<string, double> holdings = new();
    public Dictionary<string, double> totalBuyAmount = new();
    public Dictionary<string, double> totalBuyQuantity = new();

    public event Action OnPlayerNameChanged;

    void Awake() {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }

    void Start() {
        if (bullbitCash > 0) {
            totalBullbitDeposit += bullbitCash;
        }
    }

    // 알바
    public void AddPartTimeJob(long income) {
        totalPartTimeJobCount++;
        totalPartTimeJobIncome += income;
        OnStatsChanged?.Invoke();
    }

    // 급여
    public void AddSalary(long amount) {
        totalSalaryReceived += amount;
        OnStatsChanged?.Invoke();
    }

    // 복권 구매
    public void AddLotteryBuy(long price) {
        totalLotteryTicketCount++;
        totalLotterySpentAmount += price;
        OnStatsChanged?.Invoke();
    }

    // 복권 당첨
    public void AddLotteryWin(long amount) {
        totalLotteryWonAmount += amount;
        OnStatsChanged?.Invoke();
    }
    public void AddGambleSpend(long amount) {
        totalGambleSpent += amount;
        OnStatsChanged?.Invoke();
    }

    public void AddGambleEarn(long amount) {
        totalGambleEarned += amount;
        OnStatsChanged?.Invoke();
    }

    public void SetPlayerName(string name) {
        playerName = name;
        OnPlayerNameChanged?.Invoke();
    }
    public void AddFee(double feeAmount) {
        totalFeePaid += feeAmount;
    }

    // --- [중요] 입출금 로직 완전 단순화 ---
    public void ChangeBullbitCash(double amount) {
        bullbitCash += amount;

        // 수익률(%) 계산을 위해 '입금된 돈'만 단순 기록
        // 출금할 때 원금을 까거나 하는 복잡한 로직은 다 버렸습니다.
        if (amount > 0) {
            totalBullbitDeposit += amount;
        }
    }
    // ------------------------------------

    public void ChangeSatoshiMoney(double amount) {
        satoshiBankCash += amount;
    }

    // [수정] fee 파라미터 추가!
    public void RegisterBuy(string symbol, double price, double quantity, double fee) {
        if (!holdings.ContainsKey(symbol)) holdings[symbol] = 0;
        if (!totalBuyAmount.ContainsKey(symbol)) totalBuyAmount[symbol] = 0;
        if (!totalBuyQuantity.ContainsKey(symbol)) totalBuyQuantity[symbol] = 0;

        double tradeValue = price * quantity;

        holdings[symbol] += quantity;
        totalFeePaid += fee;
        // [핵심] 매수 수수료를 '매수 원금'에 더해버립니다.
        // 이러면 평단가가 살짝 높아져서, 수수료만큼 수익이 깎인 상태로 시작합니다.
        totalBuyAmount[symbol] += (tradeValue + fee);

        totalBuyQuantity[symbol] += quantity;

        if (CoinManager.Instance != null) {
            CoinManager.Instance.CheckAndSkipAlertForNewBuy(symbol);
            SyncCoinOwnedAmount(symbol);
        }

        AddTradeVolume(tradeValue);
    }

    // [수정] fee 파라미터 추가!
    public bool RegisterSell(string symbol, double price, ref double quantity, double fee) {
        const double Epsilon = 0.0000001;

        if (!holdings.ContainsKey(symbol)) {
            Debug.LogWarning($"[매도 실패] {symbol} 미보유");
            return false;
        }

        double ownedQuantity = holdings[symbol];

        if (quantity > ownedQuantity && quantity < ownedQuantity + Epsilon) {
            quantity = ownedQuantity; // 판매수량을 보유수량으로 조정
        }

        if (ownedQuantity < quantity) {
            Debug.LogWarning($"[매도 실패] {symbol} 보유 수량 부족. 보유: {ownedQuantity}, 시도: {quantity}");
            return false;
        }

        if (!totalBuyQuantity.ContainsKey(symbol) || totalBuyQuantity[symbol] <= Epsilon) {
            Debug.LogWarning($"[매도 실패] {symbol} 매수 기록 없음");
            return false;
        }

        holdings[symbol] -= quantity;

        double avgPrice = GetAvgPrice(symbol);

        // [핵심] 매도 차익에서 수수료를 뺍니다.
        // (판매가 - 평단가) * 수량 - 수수료 = 진짜 내 주머니에 들어온 순수익
        double profitFromThisTrade = ((price - avgPrice) * quantity) - fee;

        realizedProfit += profitFromThisTrade; // 누적 수익에 합산

        double reduceAmount = avgPrice * quantity;

        totalBuyAmount[symbol] -= reduceAmount;
        totalBuyQuantity[symbol] -= quantity;


        if (holdings[symbol] < Epsilon) {
            holdings[symbol] = 0;
            totalBuyAmount[symbol] = 0;
            totalBuyQuantity[symbol] = 0;
            totalFeePaid += fee;
        }
        SyncCoinOwnedAmount(symbol);
        double tradeValue = price * quantity; // 실제 매도되는 금액


        AddTradeVolume(tradeValue);

        return true;
    }

    public double GetAvgPrice(string symbol) {
        if (totalBuyAmount.ContainsKey(symbol) && totalBuyQuantity.ContainsKey(symbol)) {
            double quantity = totalBuyQuantity[symbol];
            if (quantity > 0) {
                return totalBuyAmount[symbol] / quantity;
            }
        }
        return 0;
    }

    public double GetBuyTotal(string symbol) {
        return totalBuyAmount.TryGetValue(symbol, out double total) ? total : 0;
    }

    public double GetHoldingAmount(string symbol) {
        return holdings.TryGetValue(symbol, out double amount) ? amount : 0;
    }

    //총자산 계산: 불비트 + 사토시 은행
    public string GetTotalUserAssetText() {
        double bullbit = bullbitCash;
        double bank = satoshiBankCash;
        double total = bullbit + bank;

        return $"총자산 ₩{total:N0} (불비트 ₩{bullbit:N0} / 사토시 ₩{bank:N0})";
    }

    public void ChangeCoin(string symbol, double amount) {
        if (!holdings.ContainsKey(symbol)) {
            holdings[symbol] = 0;
            totalBuyAmount[symbol] = 0;
            totalBuyQuantity[symbol] = 0;
        }

        holdings[symbol] += amount;

        if (amount > 0) {
            totalBuyQuantity[symbol] += amount;
        }

        const double Epsilon = 0.0000001;
        if (Mathf.Abs((float)holdings[symbol]) < Epsilon) {
            holdings[symbol] = 0;
            totalBuyAmount[symbol] = 0;
            totalBuyQuantity[symbol] = 0;
        }
        SyncCoinOwnedAmount(symbol);
    }
    public void UpdateHolding(string symbol, double amount) {
        holdings[symbol] = amount;

        CoinData coin = CoinManager.Instance.coins
            .Find(c => c.Symbol == symbol);

        if (coin != null) {
            coin.SetOwnedAmount(amount);
        }
    }
    private void SyncCoinOwnedAmount(string symbol) {
        if (CoinManager.Instance == null) return;

        CoinData coin = CoinManager.Instance.coins
            .Find(c => c.Symbol == symbol);

        if (coin != null)
            coin.SetOwnedAmount(GetHoldingAmount(symbol));
    }
    public double GetTotalBullbitBuyPrice() {
        double totalBuy = 0;
        // holdings를 순회하며 현재 가지고 있는 코인들의 (평단가 * 수량)을 합산
        foreach (var kv in holdings) {
            string symbol = kv.Key;
            double amount = kv.Value;

            if (amount > 0) {
                totalBuy += GetAvgPrice(symbol) * amount;
            }
        }
        return totalBuy;
    }

    public void AddTradeVolume(double amountInKRW) {
        totalTradeVolume += amountInKRW;
    }
    // [추가] 외부 입금(이체) 전용 함수 (거래대금 X, 수수료 X)
    public void RegisterTransferIn(string symbol, double price, double quantity) {
        if (!holdings.ContainsKey(symbol)) holdings[symbol] = 0;
        if (!totalBuyAmount.ContainsKey(symbol)) totalBuyAmount[symbol] = 0;
        if (!totalBuyQuantity.ContainsKey(symbol)) totalBuyQuantity[symbol] = 0;

        // 1. 수량 증가
        holdings[symbol] += quantity;

        // 2. 평단가 유지를 위해 '매수 원금'에는 시세를 반영 (거래대금 아님)
        totalBuyAmount[symbol] += (price * quantity);
        totalBuyQuantity[symbol] += quantity;

        // 3. UI 동기화
        if (CoinManager.Instance != null) {
            // 입금은 급등락 알림 스킵 체크 불필요
            // CoinManager.Instance.CheckAndSkipAlertForNewBuy(symbol); 
            SyncCoinOwnedAmount(symbol);
        }

        // ddTradeVolume(거래대금 누적)을 호출하지 않음
    }

    public string PlayerTitle {
        get {
            if (OfficeManager.Instance != null) {
                return OfficeManager.Instance.GetCurrentTitle();
            }
            return "무직";
        }
    }

    // 직급 인덱스도 OfficeManager의 새 변수를 참조
    public int PlayerTitleIndex {
        get {
            if (OfficeManager.Instance == null) return 0;
            return OfficeManager.Instance.currentRankIndex;
        }
    }


}