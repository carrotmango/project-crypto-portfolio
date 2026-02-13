using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MajorChangeGroupController : MonoBehaviour {
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI[] coinNameTexts;
    public TextMeshProUGUI[] rateTexts;
    public CanvasGroup canvasGroup;

    private Color positiveColor = new Color32(50, 214, 149, 255);   // green
    private Color negativeColor = new Color32(230, 60, 60, 255); // red
    private Color neutralColor = new Color32(180, 180, 180, 255); // gray

    public void SetData(string label, List<CoinManager.CoinChangeInfo> infos) {
        labelText.text = label;

        bool isPositiveGroup = label.Contains("상승");

        // 라벨 색상
        labelText.color = isPositiveGroup ? positiveColor : negativeColor;

        int count = Mathf.Min(2, infos.Count);

        for (int i = 0; i < count; i++) {
            var info = infos[i];
            double changeRate = info.changeRate;

            coinNameTexts[i].text = info.coin.Symbol;
            rateTexts[i].text = $"{changeRate:+0.0;-0.0}%";

            // 변동폭 색상
            if (changeRate > 0) {
                rateTexts[i].color = positiveColor;
            } else if (changeRate < 0) {
                rateTexts[i].color = negativeColor;
            } else {
                rateTexts[i].color = neutralColor;
            }
        }

        // 안쓰는 슬롯 초기화
        for (int i = count; i < 2; i++) {
            coinNameTexts[i].text = "-";
            rateTexts[i].text = "";
            rateTexts[i].color = neutralColor;
        }
    }


}
