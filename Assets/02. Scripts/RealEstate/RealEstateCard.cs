using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RealEstateCard : MonoBehaviour {
    [Header("UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI yieldText;
    public Button buyButton;
    public Image estateImage;
    public TextMeshProUGUI ownedText;

    private RealEstateData data;
    public System.Action<RealEstateData> onClickBuy;

    void Start() {
        if (buyButton != null) {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnClickButton);
        }
    }

    void OnClickButton() {
        if (data == null)
            return;

        onClickBuy?.Invoke(data);
    }

    public void SetData(RealEstateData newData) {
        data = newData;
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");

        // [수정] 텍스트 현지화
        nameText.text = data.name; // Controller에서 이미 번역된 값을 줌
        priceText.text = $"{data.price:N0} {unit}";
        yieldText.text = string.Format(LocalizationManager.GetText("LBL_YIELD"), (data.monthlyYield * 100f).ToString("F1"));

        if (estateImage != null) {
            estateImage.sprite = data.image;
            estateImage.enabled = data.image != null;
        }

        UpdateButtonAndText();
    }

    void UpdateButtonAndText() {
        if (buyButton != null) {
            buyButton.interactable = true;

            var btnText = buyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) {
                // [수정] 매각/매입 버튼 텍스트 현지화
                btnText.text = data.owned ? LocalizationManager.GetText("BTN_SELL_ESTATE") : LocalizationManager.GetText("BTN_BUY_ESTATE");
            }
        }

        if (ownedText != null) {
            ownedText.gameObject.SetActive(data.owned);
            if (data.owned) {
                // [수정] 보유중 텍스트 현지화
                ownedText.text = LocalizationManager.GetText("LBL_ESTATE_OWNED");
            }
        }
    }
}