using System.Collections;
using TMPro;
using UnityEngine;

public class SimpleTypingText : MonoBehaviour {
    public TextMeshProUGUI dialogueText;
    public float typingSpeed = 0.03f;

    [Header("Key값")]
    [TextArea(2, 3)]
    public string[] messages;

    private Coroutine typingCoroutine;

    void OnEnable() {
        ShowRandom();
    }

    public void ShowRandom() {
        if (messages == null || messages.Length == 0) return;

        // 인스펙터에서 랜덤으로 '키 값'을 하나 뽑습니다.
        string randomKey = messages[Random.Range(0, messages.Length)];
        Show(randomKey);
    }

    public void Show(string messageKey) {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // ★ [핵심] 타이핑 시작 전에, 키 값을 실제 국가별 언어로 번역합니다!
        string translatedText = LocalizationManager.GetText(messageKey);

        // 번역된 텍스트를 코루틴에 넘겨서 한 글자씩 타이핑합니다.
        typingCoroutine = StartCoroutine(TypeText(translatedText));
    }

    private IEnumerator TypeText(string message) {
        dialogueText.text = "";

        foreach (char c in message) {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }
}