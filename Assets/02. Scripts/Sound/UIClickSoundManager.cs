using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;


public class UIClickSoundManager : MonoBehaviour {
    public AudioClip defaultClickSound;
    private AudioSource audioSource;

    void Start() {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) // ÁÂÅ¬¸¯
        {
            var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);

            foreach (var result in results) {
                GameObject go = result.gameObject;

                if (go.GetComponent<Button>() != null || go.GetComponent<UIClickable>() != null) {
                    if (defaultClickSound != null) SfxPlayer.Instance.Play(defaultClickSound);
                    break;
                }
            }
        }
    }
}
