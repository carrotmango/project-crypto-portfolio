using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipSimpleToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public GameObject targetPanel;

    public void OnPointerEnter(PointerEventData eventData) {
        if (targetPanel != null) {
            targetPanel.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (targetPanel != null) {
            targetPanel.SetActive(false);
        }
    }
}
