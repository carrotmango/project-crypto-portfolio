using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TooltipSimpleToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public GameObject targetPanel;         // Tooltip Panel
    public GameObject targetPanel2;        // Tooltip Panel
    public TextMeshProUGUI tooltipText;    // Tooltip text element

    [Header("여기에 한글 대신 Key 값을 적으세요!")]
    public string hoverText = "";

    public void OnPointerEnter(PointerEventData eventData) {
        if (tooltipText != null) {
            // ★ [핵심] 인스펙터에 적힌 hoverText(키값)를 번역해서 넣어줍니다!
            tooltipText.text = LocalizationManager.GetText(hoverText);
        }

        if (targetPanel != null)
            targetPanel.SetActive(true);

        if (targetPanel2 != null)
            targetPanel2.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (targetPanel != null)
            targetPanel.SetActive(false);

        if (targetPanel2 != null)
            targetPanel2.SetActive(false);
    }
}