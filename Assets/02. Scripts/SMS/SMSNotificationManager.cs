using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SMSNotificationManager : MonoBehaviour {
    public static SMSNotificationManager Instance;

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI senderText;
    public TextMeshProUGUI previewText;
    public Button panelButton;

    private CanvasGroup cg;
    private RectTransform rt;
    private Coroutine routine;

    private string fullMessage;

    private float hiddenY = -80f;
    private float shownY = 50f;

    private void Awake() {
        Instance = this;

        cg = panel.GetComponent<CanvasGroup>();
        rt = panel.GetComponent<RectTransform>();

        if (cg == null)
            cg = panel.AddComponent<CanvasGroup>();

        panelButton.onClick.AddListener(OnClickSMS);

        panel.SetActive(false);
        cg.alpha = 0f;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, hiddenY);
    }

    public void ReceiveSMS(string sender, string preview, string message) {
        senderText.text = sender;
        previewText.text = preview;
        fullMessage = message;

        panel.SetActive(true);

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(NotificationRoutine());
    }

    private void OnClickSMS() {
        panel.SetActive(false);

        UIManager.Instance.ShowConfirm(
            $"πﬂΩ≈¿Œ: {senderText.text}\n\n{fullMessage}"
        );
    }

    private IEnumerator NotificationRoutine() {
        float t = 0f;
        float duration = 0.25f;

        cg.alpha = 0f;

        while (t < duration) {
            t += Time.deltaTime;
            float n = t / duration;

            cg.alpha = Mathf.Lerp(0f, 1f, n);
            float y = Mathf.Lerp(hiddenY, shownY, n);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);

            yield return null;
        }

        yield return new WaitForSeconds(3f);

        t = 0f;
        while (t < duration) {
            t += Time.deltaTime;
            float n = t / duration;

            cg.alpha = Mathf.Lerp(1f, 0f, n);
            float y = Mathf.Lerp(shownY, hiddenY, n);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);

            yield return null;
        }

        panel.SetActive(false);
    }

}
