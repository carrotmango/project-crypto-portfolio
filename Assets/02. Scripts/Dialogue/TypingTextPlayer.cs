using System.Collections;
using TMPro;
using UnityEngine;

public static class TypingTextPlayer {

    public static IEnumerator Play(
        TextMeshProUGUI target,
        string message,
        float speed,
        AudioClip typingClip = null
    ) {
        target.text = "";

        foreach (char c in message) {
            target.text += c;

            if (!char.IsWhiteSpace(c) && typingClip != null) {
                SfxPlayer.Instance?.Play(typingClip);
            }

            yield return new WaitForSecondsRealtime(speed);
        }
    }
}
