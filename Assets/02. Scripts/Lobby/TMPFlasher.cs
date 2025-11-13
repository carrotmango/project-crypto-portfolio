using UnityEngine;
using TMPro;
using System.Collections;

public class TMPFlasher : MonoBehaviour {
    public TextMeshProUGUI targetText;
    public float flashSpeed = 0.5f;

    private bool isFlashing = true;

    void Start() {
        if (targetText != null) {
            StartCoroutine(FlashText());
        }
    }

    IEnumerator FlashText() {
        Color bright = new Color(1f, 1f, 1f, 1f);      // 밝은 흰색
        Color dimmed = new Color(0.4f, 0.4f, 0.4f, 1f); // 어두운 회색

        while (isFlashing) {
            targetText.color = dimmed;
            yield return new WaitForSecondsRealtime(flashSpeed);
            targetText.color = bright;
            yield return new WaitForSecondsRealtime(flashSpeed);
        }
    }

    public void StopFlashing() {
        isFlashing = false;
        if (targetText != null) {
            targetText.color = new Color(1f, 1f, 1f, 1f); // 최종 고정색
        }
    }
}
