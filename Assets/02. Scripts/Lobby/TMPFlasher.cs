using UnityEngine;
using TMPro;
using System.Collections;

public class TMPFlasher : MonoBehaviour {
    public TextMeshProUGUI targetText;
    public float flashSpeed = 0.5f;

    private Coroutine flashRoutine;

    void OnEnable() {
        if (targetText == null) return;

        flashRoutine = StartCoroutine(FlashText());
    }

    void OnDisable() {
        if (flashRoutine != null) {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
    }

    IEnumerator FlashText() {
        Color bright = new Color(1f, 1f, 1f, 1f);
        Color dimmed = new Color(0.4f, 0.4f, 0.4f, 1f);

        while (true) {
            targetText.color = dimmed;
            yield return new WaitForSecondsRealtime(flashSpeed);
            targetText.color = bright;
            yield return new WaitForSecondsRealtime(flashSpeed);
        }
    }
}
