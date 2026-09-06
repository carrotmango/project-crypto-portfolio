using UnityEngine;
using TMPro;
using UnityEngine.UI; // [추가] 일반 Text를 사용하기 위해 필수!

public class UILocalizer : MonoBehaviour {
    public string key;

    // 두 종류의 텍스트 컴포넌트를 모두 대응합니다.
    private TMP_Text tmpText;
    private Text legacyText;

    private bool isQuitting = false;

    void Awake() {
        // 일단 둘 다 찾아봅니다. (있으면 들어가고 없으면 null)
        tmpText = GetComponent<TMP_Text>();
        legacyText = GetComponent<Text>();
    }

    void OnEnable() {
        if (isQuitting) return;
        Refresh();
    }

    void Start() {
        if (isQuitting) return;
        Refresh();
    }

    void OnApplicationQuit() {
        isQuitting = true;
    }

    public void Refresh() {
        if (isQuitting || this == null) return;

        // Missing 체크 및 재할당 (둘 다 시도)
        if (tmpText == null) tmpText = GetComponent<TMP_Text>();
        if (legacyText == null) legacyText = GetComponent<Text>();

        if (string.IsNullOrEmpty(key)) return;

        string translated = LocalizationManager.GetText(key);

        // 1. TMP가 있으면 TMP에 할당
        if (tmpText != null) {
            tmpText.text = translated;
        }
        // 2. 일반 Text가 있으면 일반 Text에 할당
        else if (legacyText != null) {
            legacyText.text = translated;
        }
    }
}