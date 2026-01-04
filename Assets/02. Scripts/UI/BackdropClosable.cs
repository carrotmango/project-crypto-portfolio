using UnityEngine;
using UnityEngine.EventSystems;

public class BackdropClosable : MonoBehaviour, IPointerClickHandler {
    public GameObject closeTarget;

    public void OnPointerClick(PointerEventData eventData) {
        if (closeTarget != null) {
            closeTarget.SetActive(false);
        } else {
            gameObject.SetActive(false);
        }
    }
}
