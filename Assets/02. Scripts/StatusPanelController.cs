using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusPanelController : MonoBehaviour {
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerBirthdayText;
    public Image characterImage;
    public Sprite[] characterSprites;

    [Header("자산 정보")]
    public TextMeshProUGUI CryptoAsset;
    public TextMeshProUGUI CashAsset;
    public TextMeshProUGUI estateAsset;
    // [추가] 포넨스(달러) 자산 표시용 UI
    public TextMeshProUGUI fournanceAsset;
    public TextMeshProUGUI totalAsset;

    [Header("불비트 누적 요약 정보")]
    public TextMeshProUGUI totalProfitText;     // 누적 순이익 (KRW)
    public TextMeshProUGUI totalTradeVolumeText; // 누적 거래대금 (KRW)
    //public TextMeshProUGUI totalReturnRateText;  // 누적 수익률 (%)
    public TextMeshProUGUI totalFeeText;

    public GameObject[] infoPanels;
    public Button leftArrowBtn;
    public Button rightArrowBtn;

    private int currentPanelIndex = 0; // 0: 불비트, 1: 기타, 2: ...

    void Start() {
        // 화살표 버튼 연결
        if (leftArrowBtn != null) leftArrowBtn.onClick.AddListener(() => ChangePanel(-1));
        if (rightArrowBtn != null) rightArrowBtn.onClick.AddListener(() => ChangePanel(1));

        // 초기 상태 설정 (첫 번째 패널만 켜기)
        UpdatePanelVisibility();
    }

    public void ChangePanel(int direction) {
        currentPanelIndex += direction;

        // 인덱스 순환 (끝에서 누르면 처음으로, 처음에서 누르면 끝으로)
        if (currentPanelIndex >= infoPanels.Length) currentPanelIndex = 0;
        if (currentPanelIndex < 0) currentPanelIndex = infoPanels.Length - 1;

        UpdatePanelVisibility();

        // 패널을 바꿨을 때 데이터도 즉시 갱신해주면 좋습니다.
        UpdateAssetFromStatus();
    }

    private void UpdatePanelVisibility() {
        if (infoPanels == null) return;

        for (int i = 0; i < infoPanels.Length; i++) {
            if (infoPanels[i] != null) {
                // 현재 인덱스와 같으면 켜고, 다르면 끕니다.
                infoPanels[i].SetActive(i == currentPanelIndex);
            }
        }
    }

    public void SetPlayerInfo(string name, int characterIndex, string birthday = "") {
        PlayerManager.Instance.SetPlayerName(name);
        playerNameText.text = name;

        if (!string.IsNullOrEmpty(birthday)) {
            playerBirthdayText.text = $"생일: {birthday}";
        }

        if (characterIndex >= 0 && characterIndex < characterSprites.Length) {
            characterImage.sprite = characterSprites[characterIndex];
        }
    }

    double GetEstateAsset() {
        if (RealEstatePanelController.Instance == null) return 0;
        var estates = RealEstatePanelController.Instance.GetAllEstates();
        if (estates == null) return 0;
        double total = 0;
        foreach (var estate in estates) {
            if (estate.owned) total += estate.price;
        }
        return total;
    }

    public void UpdateAssetFromStatus() {
        // 1. [실현 손익] 매도 확정 수익
        double realizedProfit = PlayerManager.Instance.realizedProfit;

        // 2. [미실현 손익] 현재 보유 코인의 평가 수익
        // (총 자산 - 현금 = 순수 코인 가치) - (코인 매수 원금)
        double currentCryptoValue = CoinManager.Instance.GetBullbitAsset() - PlayerManager.Instance.bullbitCash;
        double currentCryptoCost = PlayerManager.Instance.GetTotalBullbitBuyPrice();
        double unrealizedProfit = currentCryptoValue - currentCryptoCost;

        // 3. [최종 누적 순이익] = 실현 + 미실현
        // ★ 수수료, 입출금 다 무시하고 오직 "매매 결과"만 보여줍니다.
        double totalProfit = realizedProfit + unrealizedProfit;

        // 4. [수익률] (누적 순이익 / 총 입금액)
        double principal = PlayerManager.Instance.totalBullbitDeposit;
        double totalReturnRate = (principal > 0) ? (totalProfit / principal) * 100 : 0;

        // --- 텍스트 표시 ---
        if (totalProfitText != null) {
            totalProfitText.text = $"누적 순이익: {totalProfit:N0} KRW";
            totalProfitText.color = totalProfit >= 0 ? Color.green : Color.red;
        }

        if (totalFeeText != null) {
            double fee = PlayerManager.Instance.totalFeePaid;
            totalFeeText.text = $"누적 수수료: {fee:N0} KRW";
        }

        //if (totalReturnRateText != null) {
        //    string sign = totalReturnRate >= 0 ? "+" : "";
        //    totalReturnRateText.text = $"누적 수익률: {sign}{totalReturnRate:F2}%";
        //    totalReturnRateText.color = totalReturnRate >= 0 ? Color.green : Color.red;
        //}

        if (totalTradeVolumeText != null) {
            double totalVolume = PlayerManager.Instance.totalTradeVolume;
            totalTradeVolumeText.text = $"누적 거래대금: {totalVolume:N0} KRW";
        }

        RenderAssetRatios(GetTotalBalance(), GetTotalBalance() <= 0 ? 1 : GetTotalBalance());
    }

    private void RenderAssetRatios(double total, double safeTotal) {
        double crypto = CoinManager.Instance.GetBullbitAsset();
        double bank = CoinManager.Instance.GetSatoshiBankAsset();
        double estate = GetEstateAsset();
        double fCashUsd = PlayerManager.Instance.fournanceCash;
        double fCashKrw = fCashUsd * GlobalEconomyManager.UsdToKrw;

        CryptoAsset.text = $"불비트 ({(crypto / safeTotal) * 100:F0}%): {crypto:N0} KRW";
        CashAsset.text = $"은행 ({(bank / safeTotal) * 100:F0}%): {bank:N0} KRW";
        estateAsset.text = $"부동산 ({(estate / safeTotal) * 100:F0}%): {estate:N0} KRW";

        if (fournanceAsset != null) {
            fournanceAsset.text = $"포넨스 ({(fCashKrw / safeTotal) * 100:F0}%): ${fCashUsd:N2} USD";
        }

        totalAsset.text = $"총자산: \n {total:N0} KRW";
    }

    public double GetTotalBalance() {
        double crypto = CoinManager.Instance.GetBullbitAsset();
        double cash = CoinManager.Instance.GetSatoshiBankAsset();
        double estate = GetEstateAsset();
        double fCashKrw = PlayerManager.Instance.fournanceCash * GlobalEconomyManager.UsdToKrw;

        return crypto + cash + estate + fCashKrw;
    }


}