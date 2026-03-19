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
        if (leftArrowBtn != null) leftArrowBtn.onClick.AddListener(() => ChangePanel(-1));
        if (rightArrowBtn != null) rightArrowBtn.onClick.AddListener(() => ChangePanel(1));

        UpdatePanelVisibility();
    }

    public void ChangePanel(int direction) {
        currentPanelIndex += direction;

        if (currentPanelIndex >= infoPanels.Length) currentPanelIndex = 0;
        if (currentPanelIndex < 0) currentPanelIndex = infoPanels.Length - 1;

        UpdatePanelVisibility();
        UpdateAssetFromStatus();
    }

    private void UpdatePanelVisibility() {
        if (infoPanels == null) return;

        for (int i = 0; i < infoPanels.Length; i++) {
            if (infoPanels[i] != null) {
                infoPanels[i].SetActive(i == currentPanelIndex);
            }
        }
    }

    public void SetPlayerInfo(string name, int characterIndex, string birthday = "") {
        PlayerManager.Instance.SetPlayerName(name);
        playerNameText.text = name;

        if (!string.IsNullOrEmpty(birthday)) {
            // [수정] 생일 텍스트 현지화
            playerBirthdayText.text = string.Format(LocalizationManager.GetText("LBL_BIRTHDAY"), birthday);
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
        double realizedProfit = PlayerManager.Instance.realizedProfit;

        double currentCryptoValue = CoinManager.Instance.GetBullbitAsset() - PlayerManager.Instance.bullbitCash;
        double currentCryptoCost = PlayerManager.Instance.GetTotalBullbitBuyPrice();
        double unrealizedProfit = currentCryptoValue - currentCryptoCost;

        double totalProfit = realizedProfit + unrealizedProfit;
        double principal = PlayerManager.Instance.totalBullbitDeposit;
        double totalReturnRate = (principal > 0) ? (totalProfit / principal) * 100 : 0;

        // --- 텍스트 표시 ---
        if (totalProfitText != null) {
            // [수정] 누적 순이익 현지화
            totalProfitText.text = string.Format(LocalizationManager.GetText("LBL_TOTAL_PROFIT"), totalProfit.ToString("N0"));
            totalProfitText.color = totalProfit >= 0 ? Color.green : Color.red;
        }

        if (totalFeeText != null) {
            double fee = PlayerManager.Instance.totalFeePaid;
            // [수정] 누적 수수료 현지화
            totalFeeText.text = string.Format(LocalizationManager.GetText("LBL_TOTAL_FEE"), fee.ToString("N0"));
        }

        if (totalTradeVolumeText != null) {
            double totalVolume = PlayerManager.Instance.totalTradeVolume;
            // [수정] 누적 거래대금 현지화
            totalTradeVolumeText.text = string.Format(LocalizationManager.GetText("LBL_TOTAL_VOLUME"), totalVolume.ToString("N0"));
        }

        RenderAssetRatios(GetTotalBalance(), GetTotalBalance() <= 0 ? 1 : GetTotalBalance());
    }

    private void RenderAssetRatios(double total, double safeTotal) {
        double crypto = CoinManager.Instance.GetBullbitAsset();
        double bank = CoinManager.Instance.GetSatoshiBankAsset();
        double estate = GetEstateAsset();

        double fTotalKrw = GetFournanceTotalAssetKRW();
        double fTotalUsd = fTotalKrw / GlobalEconomyManager.UsdToKrw;

        // [수정] 자산 비중 퍼센트 표시 현지화
        CryptoAsset.text = string.Format(LocalizationManager.GetText("LBL_ASSET_BULLBIT"), ((crypto / safeTotal) * 100).ToString("F0"), crypto.ToString("N0"));
        CashAsset.text = string.Format(LocalizationManager.GetText("LBL_ASSET_BANK"), ((bank / safeTotal) * 100).ToString("F0"), bank.ToString("N0"));
        estateAsset.text = string.Format(LocalizationManager.GetText("LBL_ASSET_ESTATE"), ((estate / safeTotal) * 100).ToString("F0"), estate.ToString("N0"));

        if (fournanceAsset != null) {
            fournanceAsset.text = string.Format(LocalizationManager.GetText("LBL_ASSET_FOURNANCE"), ((fTotalKrw / safeTotal) * 100).ToString("F0"), fTotalUsd.ToString("N2"));
        }

        totalAsset.text = string.Format(LocalizationManager.GetText("LBL_TOTAL_ASSET"), total.ToString("N0"));
    }

    public double GetTotalBalance() {
        double crypto = CoinManager.Instance.GetBullbitAsset();
        double cash = CoinManager.Instance.GetSatoshiBankAsset();
        double estate = GetEstateAsset();
        double fTotalKrw = GetFournanceTotalAssetKRW();

        return crypto + cash + estate + fTotalKrw;
    }

    private double GetFournanceTotalAssetKRW() {
        if (FourNanceManager.Instance != null) {
            double equityUsd = FourNanceManager.Instance.GetTotalEquity();
            return equityUsd * GlobalEconomyManager.UsdToKrw;
        }

        if (PlayerManager.Instance != null) {
            return PlayerManager.Instance.fournanceCash * GlobalEconomyManager.UsdToKrw;
        }
        return 0;
    }
}