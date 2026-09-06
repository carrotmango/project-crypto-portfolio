using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayClickSound : MonoBehaviour, IPointerClickHandler {
    public AudioClip clickSound;
    private AudioSource audioSource;

    void Start() {
        audioSource = Camera.main.GetComponent<AudioSource>();
        if (audioSource == null) {
            audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
        }
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (clickSound != null) {
            audioSource.PlayOneShot(clickSound);
        }
    }
}
