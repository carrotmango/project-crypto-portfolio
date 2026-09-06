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

    private readonly Color32 colorBank = new Color32(40, 50, 150, 255);
    private readonly Color32 colorBullbit = new Color32(50, 200, 100, 255);
    private readonly Color32 colorFournance = new Color32(240, 200, 50, 255);
    private readonly Color32 colorRealEstate = new Color32(200, 80, 50, 255);

    private float timer = 0f;
    private float updateInterval = 0.2f;

    private void Awake() {
        ApplyDefaultColors();
    }

    private void OnEnable() {
        // 패널이 켜질 때 즉시 갱신
        UpdatePieChart();
    }

    // [핵심 수정] 매 프레임(Tick)마다 차트 비중을 실시간으로 계산해서 다시 그립니다.
    private void Update() {
        timer += Time.unscaledDeltaTime; // Time.timeScale이 0이어도 작동하도록 unscaled 사용
        if (timer >= updateInterval) {
            UpdatePieChart();
            timer = 0f;
        }
    }

    public void UpdatePieChart() {
        ApplyDefaultColors();

        if (CoinManager.Instance == null || PlayerManager.Instance == null) return;

        double bank = CoinManager.Instance.GetSatoshiBankAsset();
        double bullbit = CoinManager.Instance.GetBullbitAsset();

        // [수정] 포넨스 총 자산 (현금 + 증거금 + PnL) 가져오기
        double fournance = GetFournanceTotalAssetKRW();

        double estate = GetEstateAssetValue();

        double total = bank + bullbit + fournance + estate;

        // 자산이 아예 없거나 마이너스인 경우 예외 처리
        if (total <= 0) {
            bankSlice.fillAmount = 1f;
            bankSlice.color = Color.gray;
            if (bankLegendIcon != null) bankLegendIcon.color = Color.gray;

            bullbitSlice.fillAmount = 0;
            fournanceSlice.fillAmount = 0;
            realEstateSlice.fillAmount = 0;
            return;
        }

        // 누적 FillAmount 계산 (Pie Chart 로직: 뒤에서부터 채워짐 1.0 -> 0.0)
        // 순서: Bank(1.0) -> Bullbit -> Fournance -> RealEstate

        // 1. 은행 (가장 뒤, 항상 100% 채워둠, 앞에게 덮어씀)
        bankSlice.fillAmount = 1.0f;

        // 2. 불비트 + 포넨스 + 부동산
        bullbitSlice.fillAmount = (float)((bullbit + fournance + estate) / total);

        // 3. 포넨스 + 부동산
        fournanceSlice.fillAmount = (float)((fournance + estate) / total);

        // 4. 부동산 (가장 앞)
        realEstateSlice.fillAmount = (float)(estate / total);
    }

    // [포넨스 자산 가져오기]
    private double GetFournanceTotalAssetKRW() {
        // 1. 포넨스 매니저가 있으면 거기서 계산된 총액(USD Equity)을 받아옵니다.
        if (FourNanceManager.Instance != null) {
            double equityUsd = FourNanceManager.Instance.GetTotalEquity();
            return equityUsd * GlobalEconomyManager.UsdToKrw;
        }

        // 2. 매니저가 로드되지 않았다면 최소한 현금이라도
        if (PlayerManager.Instance != null) {
            return PlayerManager.Instance.fournanceCash * GlobalEconomyManager.UsdToKrw;
        }
        return 0;
    }

    private void ApplyDefaultColors() {
        if (bankSlice != null) bankSlice.color = colorBank;
        if (bullbitSlice != null) bullbitSlice.color = colorBullbit;
        if (fournanceSlice != null) fournanceSlice.color = colorFournance;
        if (realEstateSlice != null) realEstateSlice.color = colorRealEstate;

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