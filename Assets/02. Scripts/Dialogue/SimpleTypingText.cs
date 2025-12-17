using System.Collections;
using TMPro;
using UnityEngine;

public class SimpleTypingText : MonoBehaviour {
    public TextMeshProUGUI dialogueText;
    public float typingSpeed = 0.03f;

    [Header("Random Messages")]
    [TextArea(2, 3)]
    public string[] messages;

    private Coroutine typingCoroutine;

    void OnEnable() {
        ShowRandom();
    }


    public void ShowRandom() {
        if (messages == null || messages.Length == 0) return;

        string randomMessage = messages[Random.Range(0, messages.Length)];
        Show(randomMessage);
    }

    public void Show(string message) {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(message));
    }

    private IEnumerator TypeText(string message) {
        dialogueText.text = "";

        foreach (char c in message) {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }
}
