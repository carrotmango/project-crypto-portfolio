using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GlobalUIClickSound : MonoBehaviour {
    public AudioSource audioSource;
    public AudioClip clickSound;

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) {
                // 클릭된 오브젝트가 버튼인지 체크
                GameObject clicked = EventSystem.current.currentSelectedGameObject;
                if (clicked != null && clicked.GetComponent<Button>() != null) {
                    audioSource.PlayOneShot(clickSound);
                }
            }
        }
    }
}
