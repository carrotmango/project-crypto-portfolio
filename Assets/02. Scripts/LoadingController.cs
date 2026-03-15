using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingController : MonoBehaviour {
    public GameObject loadingRoot;
    public Image fillBar;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI tipText;

    float currentFill = 0f;

    bool lowTipShown = false;
    bool highTipShown = false;

    string[] lowTipsKeys = new string[]
        {
        "TIP_LOW_1", "TIP_LOW_2", "TIP_LOW_3", "TIP_LOW_4", "TIP_LOW_5", "TIP_LOW_6", "TIP_LOW_7"
        };

    string[] highTipsKeys = new string[]
    {
        "TIP_HIGH_1", "TIP_HIGH_2", "TIP_HIGH_3", "TIP_HIGH_4", "TIP_HIGH_5", "TIP_HIGH_6", "TIP_HIGH_7"
    };

    void Start() {
        loadingRoot.SetActive(false);
        enabled = false;
    }

    public void BeginLoading() {
        currentFill = 0f;
        fillBar.fillAmount = 0f;
        loadingText.text = LocalizationManager.GetText("UI_LOADING"); // 여기도 Key 적용
        tipText.text = "";

        lowTipShown = false;
        highTipShown = false;

        loadingRoot.SetActive(true);
        enabled = true;
    }

    void Update() {
        float target = CalculateProgress();

        currentFill = Mathf.MoveTowards(
            currentFill,
            target,
            Time.deltaTime * 0.6f
        );

        fillBar.fillAmount = currentFill;
        loadingText.text = Mathf.RoundToInt(currentFill * 100f) + "%";

        // 0% ~ 49%
        if (!lowTipShown && currentFill < 0.5f) {
            ShowRandomLowTip();
            lowTipShown = true;
        }

        // 50% 이상
        if (!highTipShown && currentFill >= 0.5f) {
            ShowRandomHighTip();
            highTipShown = true;
        }

        if (GameBootState.IsAllReady() && currentFill >= 0.999f) {
            FinishLoading();
        }
    }


    void ShowRandomLowTip() {
        if (tipText == null || lowTipsKeys.Length == 0) return;

        int index = Random.Range(0, lowTipsKeys.Length);
        tipText.text = LocalizationManager.GetText(lowTipsKeys[index]);
    }

    void ShowRandomHighTip() {
        if (tipText == null || highTipsKeys.Length == 0) return;

        int index = Random.Range(0, highTipsKeys.Length);
        tipText.text = LocalizationManager.GetText(highTipsKeys[index]);
    }

    void FinishLoading() {
        loadingRoot.SetActive(false);
        enabled = false;
    }

    float CalculateProgress() {
        float progress = 0f;
        if (GameBootState.saveLoaded) progress += 0.5f;
        if (GameBootState.playerReady) progress += 0.5f;
        return Mathf.Clamp01(progress);
    }
}
