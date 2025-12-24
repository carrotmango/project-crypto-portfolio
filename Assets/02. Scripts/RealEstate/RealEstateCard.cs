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

        nameText.text = data.name;
        priceText.text = $"{data.price:N0}원";
        yieldText.text = $"월세 수익: {data.monthlyYield * 100f:F1}%";

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
                btnText.text = data.owned ? "매각" : "매입";
            }
        }

        if (ownedText != null) {
            ownedText.gameObject.SetActive(data.owned);
            if (data.owned) {
                ownedText.text = "보유중";
            }
        }
    }
}
