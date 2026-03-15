using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TooltipSimpleToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public GameObject targetPanel;          // Tooltip Panel
    public GameObject targetPanel2;          // Tooltip Panel
    public TextMeshProUGUI tooltipText;     // Tooltip text element
    public string hoverText = "";           

    public void OnPointerEnter(PointerEventData eventData) {
        if (tooltipText != null)
            tooltipText.text = hoverText;

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
