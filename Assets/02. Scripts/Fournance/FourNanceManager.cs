using UnityEngine;
using TMPro;
using System;

public class FourNanceManager : MonoBehaviour {
    [Header("포넨스 핵심 UI")]
    public TextMeshProUGUI totalBalanceText;      // 총 자산 (Equity: 원금 + PnL)
    public TextMeshProUGUI availableMarginText;   // 주문 가능 금액 (Free Margin)
    public TextMeshProUGUI unrealizedPNLText;     // 미실현 손익

    private void Update() {
        RefreshUI();
    }

    public void RefreshUI() {
        if (PlayerManager.Instance == null) return;

        // 1. 현재 지갑에 남은 현금 (주문 가능 금액)
        double currentCash = PlayerManager.Instance.fournanceCash;

        // 2. 포지션들에 묶여있는 증거금 합계
        double usedMargin = GetUsedMargin();

        // 3. 미실현 손익
        double pnlUsd = GetCurrentPnL();

        // [수정된 로직] 
        // 보유 잔액(Equity) = (남은 현금 + 묶인 증거금) + 현재 손익
        // 수익이 나면 잔액이 늘어나 보이고, 손실이 나면 줄어들어 보입니다.
        double totalWalletBalance = currentCash + usedMargin + pnlUsd;

        // [UI 적용]
        // 보유 잔액: PnL에 따라 실시간 변동
        if (totalBalanceText != null)
            totalBalanceText.text = $"${Math.Max(0, totalWalletBalance):N2}";

        // 주문 가능 금액: 아직 실현 안 했으니 PnL 제외한 '찐 현금'만 표시
        if (availableMarginText != null)
            availableMarginText.text = $"${Math.Max(0, currentCash):N2}";

        // 미실현 손익: 색상 처리
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