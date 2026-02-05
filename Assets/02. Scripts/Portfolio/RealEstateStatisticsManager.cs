using UnityEngine;
using TMPro;
using System.Collections;

public class RealEstateStatisticsManager : MonoBehaviour {

    [Header("UI References")]
    public TextMeshProUGUI estateTotalValueText;
    public TextMeshProUGUI estateTotalIncomeText;
    public TextMeshProUGUI estateNetProfitText;
    public TextMeshProUGUI estateExpectedIncomeText;

    private void OnEnable() {
        // 코루틴 시작 (초기화 및 구독 연결)
        StartCoroutine(InitAndSubscribe());
    }

    private void OnDisable() {
        // [중요] 패널이 꺼질 때 구독 해제 (메모리 누수 방지)
        if (RealEstatePanelController.Instance != null) {
            RealEstatePanelController.Instance.OnRealEstateChanged -= UpdateStatisticsUI;
        }
    }

    IEnumerator InitAndSubscribe() {
        // 매니저들이 준비될 때까지 대기
        while (PlayerManager.Instance == null || RealEstatePanelController.Instance == null) {
            yield return null;
        }

        // [핵심] 이벤트 구독: "부동산 정보 바뀌면 UpdateStatisticsUI 실행해라"
        RealEstatePanelController.Instance.OnRealEstateChanged -= UpdateStatisticsUI; // 중복 방지용 제거
        RealEstatePanelController.Instance.OnRealEstateChanged += UpdateStatisticsUI;

        // 처음에 한 번 강제 실행 (초기값 표시용)
        UpdateStatisticsUI();
    }

    public void UpdateStatisticsUI() {
        var pm = PlayerManager.Instance;
        var estateCtrl = RealEstatePanelController.Instance;

        if (pm == null || estateCtrl == null) return;

        long currentTotalValue = 0;
        long totalPurchaseCost = 0;
        long totalExpectedIncome = 0;

        var myEstates = estateCtrl.GetAllEstates();

        // 데이터 리스트가 비어있을 수 있으므로 체크
        if (myEstates != null) {
            foreach (var estate in myEstates) {
                if (estate.owned) {
                    currentTotalValue += estate.price;
                    totalPurchaseCost += estate.purchasePrice;

                    // 예상 수익 계산
                    long income = (long)(estate.price * estate.monthlyYield);
                    totalExpectedIncome += income;
                }
            }
        }

        // UI 갱신 (기존 코드와 동일)
        estateTotalValueText.text = $"총 보유 자산: {currentTotalValue:N0}원";
        estateTotalIncomeText.text = $"총 월세 수익: {pm.totalRealEstateIncome:N0}원";

        long netProfit = currentTotalValue - totalPurchaseCost;
        if (netProfit > 0) estateNetProfitText.text = $"평가 손익: <color=red>▲{netProfit:N0}원</color>";
        else if (netProfit < 0) estateNetProfitText.text = $"평가 손익: <color=blue>▼{netProfit:N0}원</color>";
        else estateNetProfitText.text = "평가 손익: -";

        if (estateExpectedIncomeText != null) {
            estateExpectedIncomeText.text = $"예상 주기 수익: +{totalExpectedIncome:N0}원";
        }
    }
}