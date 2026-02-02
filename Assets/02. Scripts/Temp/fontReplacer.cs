using UnityEngine;
using TMPro;

public class FontReplacer : MonoBehaviour {
    public TMP_FontAsset newFont;

    void Start() {
        TMP_Text[] allText = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (TMP_Text text in allText) {
            text.font = newFont;
        }
    }
}
