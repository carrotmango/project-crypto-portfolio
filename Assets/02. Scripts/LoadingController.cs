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

    string[] lowTips = new string[]
    {
    "도지코인은 2013년에 처음 만들어졌습니다.",
    "비트코인의 첫 블록은 2009년에 생성되었습니다.",
    "모든 코인은 각자 다른 네트워크 위에서 작동합니다.",
    "ERC-20은 이더리움 기반 토큰 표준입니다.",
    "네트워크 수수료는 블록체인마다 다릅니다.",
    "지갑 주소는 네트워크가 다르면 사용할 수 없습니다.",
    "코인 전송은 되돌릴 수 없습니다."
    };

    string[] highTips = new string[]
    {
    "도지코인은 작업증명 방식의 블록체인입니다.",
    "비트코인과 라이트코인은 같은 채굴 알고리즘 계열입니다.",
    "네트워크 혼동은 자산 손실로 이어질 수 있습니다.",
    "브릿지는 편리하지만 항상 위험을 동반합니다.",
    "토큰과 코인은 기술적으로 다른 개념입니다.",
    "유동성은 가격보다 먼저 확인해야 합니다.",
    "온체인 데이터는 시장 심리를 보여줍니다."
    };

    void Start() {
        loadingRoot.SetActive(false);
        enabled = false;
    }

    public void BeginLoading() {
        currentFill = 0f;
        fillBar.fillAmount = 0f;
        loadingText.text = "로딩중...";
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
        if (tipText == null || lowTips.Length == 0) return;

        int index = Random.Range(0, lowTips.Length);
        tipText.text = lowTips[index];
    }

    void ShowRandomHighTip() {
        if (tipText == null || highTips.Length == 0) return;

        int index = Random.Range(0, highTips.Length);
        tipText.text = highTips[index];
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
