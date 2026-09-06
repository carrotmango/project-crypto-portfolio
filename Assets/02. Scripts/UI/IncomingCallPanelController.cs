using UnityEngine;
using UnityEngine.UI;

public class IncomingCallPanelController : MonoBehaviour {

    public Button yesButton;
    public Button noButton;

    [Header("Sound")]
    public AudioClip callClip;

    private void OnEnable() {
        if (SfxPlayer.Instance != null && callClip != null) {
            SfxPlayer.Instance.Play(callClip); // 한 번만 재생
        }
    }

    public void Init(System.Action onYes, System.Action onNo) {

        yesButton.onClick.AddListener(() => {
            onYes?.Invoke();
            Destroy(gameObject);
        });

        noButton.onClick.AddListener(() => {
            onNo?.Invoke();
            Destroy(gameObject);
        });
    }
}
