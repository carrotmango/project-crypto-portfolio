using UnityEngine;
using TMPro;
using System;

public class FourNanceManager : MonoBehaviour {
    [Header("포넨스 핵심 UI")]
    public TextMeshProUGUI totalBalanceText;      // 총 자산 (Equity: 원금 + PnL)
    public TextMeshProUGUI availableMarginText;   // 주문 가능 금액 (Free Margin)
    public TextMeshProUGUI unrealizedPNLText;     // 미실현 손익

    public static FourNanceManager Instance;

    private void Awake() {
        Instance = this;
    }

    private void Update() {
        RefreshUI();
    }

    public void RefreshUI() {
        if (PlayerManager.Instance == null) return;

        // 1. 현재 지갑 잔고 (단순 현금)
        double currentCash = PlayerManager.Instance.fournanceCash;

        // 2. 증거금 합계
        double usedMargin = GetUsedMargin();

        // 3. 미실현 손익
        double pnlUsd = GetCurrentPnL();

        // ---------------------------------------------------------------
        // [수정 1] 총 자산 (Equity) = 현금 + 증거금 + PnL
        // ---------------------------------------------------------------
        double totalWalletBalance = currentCash + usedMargin + pnlUsd;

        if (totalBalanceText != null)
            totalBalanceText.text = $"${Math.Max(0, totalWalletBalance):N2}";

        // ---------------------------------------------------------------
        // [수정 2] 주문 가능 금액 (Available Margin)
        // 변경: FutureChartRenderer의 GetBuyingPower() 호출 (쓴 돈 뺀 금액)
        // ---------------------------------------------------------------
        double availableMargin = currentCash; // 기본값

        if (FutureChartRenderer.Instance != null) {
            // [핵심 수정] Equity가 아니라 BuyingPower를 가져와야 함!
            availableMargin = FutureChartRenderer.Instance.GetBuyingPower();
        }

        if (availableMarginText != null)
            availableMarginText.text = $"${Math.Max(0, availableMargin):N2}";

        // ---------------------------------------------------------------

        // 미실현 손익 UI
        if (unrealizedPNLText != null) {
            unrealizedPNLText.text = (pnlUsd >= 0) ? $"+${pnlUsd:N2}" : $"-${Math.Abs(pnlUsd):N2}";
            unrealizedPNLText.color = (pnlUsd > 0) ? new Color32(50, 214, 149, 255) : (pnlUsd < 0 ? new Color32(230, 60, 60, 255) : Color.white);
        }
    }

    private double GetCurrentPnL() {
        if (FutureChartRenderer.Instance != null) {
            return FutureChartRenderer.Instance.CalculateTotalUnrealizedPnL();
        }
        return 0;
    }

    private double GetUsedMargin() {
        if (FutureChartRenderer.Instance != null) {
            return FutureChartRenderer.Instance.CalculateTotalUsedMargin();
        }
        return 0;
    }
}