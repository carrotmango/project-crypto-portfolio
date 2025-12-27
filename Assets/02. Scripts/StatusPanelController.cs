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
    public TextMeshProUGUI totalAsset;


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
        if (RealEstatePanelController.Instance == null)
            return 0;

        var estates = RealEstatePanelController.Instance.GetAllEstates();
        if (estates == null)
            return 0;

        long total = 0;

        foreach (var estate in estates) {
            if (!estate.owned)
                continue;

            total += estate.price;
        }

        return total;
    }



    public void UpdateAssetFromStatus() {
        double crypto = CoinManager.Instance.GetBullbitAsset();
        double cash = CoinManager.Instance.GetSatoshiBankAsset();
        double estate = GetEstateAsset();

        CryptoAsset.text = $"불비트: {crypto:N0} KRW";
        CashAsset.text = $"은행: {cash:N0} KRW";
        estateAsset.text = $"부동산: {estate:N0} KRW";
        totalAsset.text = $"총자산: {GetTotalBalance():N0} KRW";
    }


    double GetTotalBalance() {
        double crypto = CoinManager.Instance.GetBullbitAsset();
        double cash = CoinManager.Instance.GetSatoshiBankAsset();
        double estate = GetEstateAsset();

        return crypto + cash + estate;
    }

}