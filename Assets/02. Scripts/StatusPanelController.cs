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


    public void SetPlayerInfo(string name, int characterIndex, string birthday = "") {
        playerNameText.text = name;

        if (!string.IsNullOrEmpty(birthday)) {
            playerBirthdayText.text = $"생일: {birthday}";
        }

        if (characterIndex >= 0 && characterIndex < characterSprites.Length) {
            characterImage.sprite = characterSprites[characterIndex];
        }
    }


    public void UpdateAssetFromStatus() {
        double crypto = CoinManager.Instance.GetBullbitAsset(); // 추후 다른 거래소 추가 시 확장 필요
        double cash = CoinManager.Instance.GetSatoshiBankAsset();
        double estate = 0; // 현재 부동산 자산은 없으므로 0으로 처리

        CryptoAsset.text = $"불비트: {crypto:N0} KRW";
        CashAsset.text = $"은행: {cash:N0} KRW";
        estateAsset.text = $"부동산: {estate:N0} KRW"; // 항상 0으로 표시
    }
}