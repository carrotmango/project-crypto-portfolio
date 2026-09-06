using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class RealEstateStatisticsManager : MonoBehaviour {

    [Header("UI References")]
    public TextMeshProUGUI estateTotalValueText;
    public TextMeshProUGUI estateTotalIncomeText;
    public TextMeshProUGUI estateNetProfitText;
    public TextMeshProUGUI estateExpectedIncomeText;

    private void OnEnable() {
        // 여기서 바로 실행하지 않고, 코루틴에게 넘깁니다.
        StartCoroutine(InitAndSubscribe());
    }

    private void OnDisable() {
        if (RealEstatePanelController.Instance != null) {
            RealEstatePanelController.Instance.OnRealEstateChanged -= UpdateStatisticsUI;
        }
    }

    IEnumerator InitAndSubscribe() {
        // [핵심] 언어 매니저(LocalizationManager)가 JSON을 다 읽을 수 있도록 딱 1프레임만 기다려줍니다!
        yield return null;

        // 1프레임 쉰 다음, 안전하게 첫 번역본을 화면에 쏴줍니다. (해금 전에도 무조건 0원으로 영문 출력)
        UpdateStatisticsUI();

        // 그 이후에 부동산 매니저가 켜질 때까지 무한 대기
        while (PlayerManager.Instance == null || RealEstatePanelController.Instance == null) {
            yield return null;
        }

        // 해금 완료 후 이벤트 연결
        RealEstatePanelController.Instance.OnRealEstateChanged -= UpdateStatisticsUI;
        RealEstatePanelController.Instance.OnRealEstateChanged += UpdateStatisticsUI;

        // 해금되었으니 다시 한번 갱신
        UpdateStatisticsUI();
    }

    public void UpdateStatisticsUI() {
        long currentTotalValue = 0;
        long totalPurchaseCost = 0;
        long totalExpectedIncome = 0;
        long totalRentIncome = 0;

        // [수정] PlayerManager나 RealEstatePanelController가 없어도 
        // 튕겨나가지(return) 않고 기본값(0)으로 아래 번역 로직을 실행하게 바꿨습니다.

        if (PlayerManager.Instance != null) {
            totalRentIncome = PlayerManager.Instance.totalRealEstateIncome;
        }

        if (RealEstatePanelController.Instance != null) {
            var myEstates = RealEstatePanelController.Instance.GetAllEstates();
            if (myEstates != null) {
                foreach (var estate in myEstates) {
                    if (estate.owned) {
                        currentTotalValue += estate.price;
                        totalPurchaseCost += estate.purchasePrice;

                        long income = (long)(estate.price * estate.monthlyYield);
                        totalExpectedIncome += income;
                    }
                }
            }
        }

        string unit = LocalizationManager.GetText("UNIT_CURRENCY");

        if (estateTotalValueText != null)
            estateTotalValueText.text = string.Format(LocalizationManager.GetText("LBL_ESTATE_TOTAL_VALUE"), currentTotalValue.ToString("N0"), unit);

        if (estateTotalIncomeText != null)
            estateTotalIncomeText.text = string.Format(LocalizationManager.GetText("LBL_ESTATE_TOTAL_INCOME"), totalRentIncome.ToString("N0"), unit);

        long netProfit = currentTotalValue - totalPurchaseCost;
        if (estateNetProfitText != null) {
            if (netProfit > 0) {
                estateNetProfitText.text = string.Format(LocalizationManager.GetText("LBL_ESTATE_NET_PROFIT_UP"), netProfit.ToString("N0"), unit);
            } else if (netProfit < 0) {
                estateNetProfitText.text = string.Format(LocalizationManager.GetText("LBL_ESTATE_NET_PROFIT_DOWN"), Math.Abs(netProfit).ToString("N0"), unit);
            } else {
                estateNetProfitText.text = LocalizationManager.GetText("LBL_ESTATE_NET_PROFIT_NONE");
            }
        }

        if (estateExpectedIncomeText != null) {
            estateExpectedIncomeText.text = string.Format(LocalizationManager.GetText("LBL_ESTATE_EXPECTED_INCOME"), totalExpectedIncome.ToString("N0"), unit);
        }
    }
}