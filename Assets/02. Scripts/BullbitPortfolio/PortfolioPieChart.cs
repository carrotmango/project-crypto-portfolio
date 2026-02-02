using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PortfolioPieChart : MonoBehaviour {
    [Header("차트 조각들 (뒤에서부터 순서대로)")]
    public Image bankSlice;
    public Image bullbitSlice;
    public Image fournanceSlice;
    public Image realEstateSlice;

    [Header("범례 아이콘 (색상 매칭용 사각형)")]
    public Image bankLegendIcon;
    public Image bullbitLegendIcon;
    public Image fournanceLegendIcon;
    public Image realEstateLegendIcon;

    private readonly Color32 colorBank = new Color32(40, 50, 150, 255);       // 남색
    private readonly Color32 colorBullbit = new Color32(50, 200, 100, 255);   // 초록
    private readonly Color32 colorFournance = new Color32(240, 200, 50, 255); // 노랑
    private readonly Color32 colorRealEstate = new Color32(200, 80, 50, 255); // 주황

    private void Awake() {
        ApplyDefaultColors();
    }

    private void OnEnable() {
        StopAllCoroutines();
        StartCoroutine(InitChartRealtime());
    }

    IEnumerator InitChartRealtime() {
        // 시간 정지 상태에서도 작동하도록 Realtime 대기
        yield return new WaitForSecondsRealtime(0.1f);
        UpdatePieChart();
    }

    public void UpdatePieChart() {
        ApplyDefaultColors();

        if (CoinManager.Instance == null || PlayerManager.Instance == null) return;

        double bank = CoinManager.Instance.GetSatoshiBankAsset();
        double bullbit = CoinManager.Instance.GetBullbitAsset();
        double fournance = PlayerManager.Instance.fournanceCash * GlobalEconomyManager.UsdToKrw;
        double estate = GetEstateAssetValue();

        double total = bank + bullbit + fournance + estate;

        if (total <= 0) {
            bankSlice.fillAmount = 1f;
            bankSlice.color = Color.gray;
            if (bankLegendIcon != null) bankLegendIcon.color = Color.gray;

            bullbitSlice.fillAmount = 0;
            fournanceSlice.fillAmount = 0;
            realEstateSlice.fillAmount = 0;
            return;
        }

        // 누적 FillAmount 계산
        bankSlice.fillAmount = 1.0f;
        bullbitSlice.fillAmount = (float)((bullbit + fournance + estate) / total);
        fournanceSlice.fillAmount = (float)((fournance + estate) / total);
        realEstateSlice.fillAmount = (float)(estate / total);
    }

    private void ApplyDefaultColors() {
        // 차트 조각 색상 적용
        if (bankSlice != null) bankSlice.color = colorBank;
        if (bullbitSlice != null) bullbitSlice.color = colorBullbit;
        if (fournanceSlice != null) fournanceSlice.color = colorFournance;
        if (realEstateSlice != null) realEstateSlice.color = colorRealEstate;

        // [추가] 범례 사각형 아이콘 색상 동기화
        if (bankLegendIcon != null) bankLegendIcon.color = colorBank;
        if (bullbitLegendIcon != null) bullbitLegendIcon.color = colorBullbit;
        if (fournanceLegendIcon != null) fournanceLegendIcon.color = colorFournance;
        if (realEstateLegendIcon != null) realEstateLegendIcon.color = colorRealEstate;
    }

    private double GetEstateAssetValue() {
        try {
            if (RealEstatePanelController.Instance == null) return 0;
            var estates = RealEstatePanelController.Instance.GetAllEstates();
            if (estates == null) return 0;

            double total = 0;
            foreach (var estate in estates) {
                if (estate != null && estate.owned) total += estate.price;
            }
            return total;
        } catch { return 0; }
    }
}