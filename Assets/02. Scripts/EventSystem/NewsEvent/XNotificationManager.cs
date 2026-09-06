using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class XNotificationManager : MonoBehaviour {
    public static XNotificationManager Instance;

    [Header("구독 설정")]
    public bool isSubscribed = false;
    public bool isCancelRequested = false;
    public int monthlyFee = 30000;
    public DateTime nextBillingDate;

    [Header("무료 체험 설정")]
    public bool isFreeTrialUsed = false;

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI messageText;
    public Button panelButton;

    [Header("구독UI")]
    public GameObject subcribePanel;
    public TextMeshProUGUI subcribeText;
    public Button subscribeActionBtn;

    [Header("외부 연결")]
    public AppSelectorController appSelector;
    public PlayerManager player;
    public CoinManager coinManager;

    private CanvasGroup cg;
    private RectTransform rt;
    private Coroutine routine;
    private string latestPostId;

    private float hiddenY = -80f;
    private float shownY = 50f;

    private void Awake() {
        Instance = this;

        cg = panel.GetComponent<CanvasGroup>();
        rt = panel.GetComponent<RectTransform>();
        if (cg == null) {
            cg = panel.AddComponent<CanvasGroup>();
        }

        panelButton.onClick.AddListener(OnClickNotification);

        panel.SetActive(false);
        cg.alpha = 0f;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, hiddenY);
    }

    private void Start() {
        if (isSubscribed == false && isFreeTrialUsed == false) {
            ApplyFreeTrial();
        }
    }

    private void Update() {
        if (isSubscribed == false) {
            return;
        }

        DateTime now = coinManager.CurrentDateTime;

        if (isCancelRequested == true) {
            if (now >= nextBillingDate) {
                isSubscribed = false;
                isCancelRequested = false;
                Debug.Log("[구독 종료] 취소 신청 → 결제일 도달, 자동 해지");
            }
            return;
        }

        if (now >= nextBillingDate) {
            ChargeSubscription();
        }
    }

    public void ApplyFreeTrial() {
        isSubscribed = true;
        isCancelRequested = true;
        isFreeTrialUsed = true;

        if (coinManager != null) {
            nextBillingDate = coinManager.CurrentDateTime.AddDays(30);
        } else {
            nextBillingDate = new DateTime(2020, 1, 1).AddDays(30);
        }

        Debug.Log($"[무료 체험 시작] 30일간 무료! 다음 청구일: {nextBillingDate}, 자동 해지예정");
    }

    public void Show(string authorName, string postId) {
        if (isSubscribed == false) return;

        latestPostId = postId;

        if (GlobalNotificationManager.Instance != null) {
            // [수정] 푸시 알림 발신자 및 내용 현지화
            string sender = LocalizationManager.GetText("LBL_XBIRD_NOTI_SENDER");
            string msg = string.Format(LocalizationManager.GetText("MSG_XBIRD_NEW_POST"), authorName);

            GlobalNotificationManager.Instance.ShowNotification(
                "Xbird",
                sender,
                msg,
                () => {
                    if (appSelector != null) {
                        appSelector.OpenXbirdApp();
                    }
                    Debug.Log("Xbird 알림 클릭됨! 앱 실행");
                }
            );
        }
    }

    private void OnClickNotification() {
        panel.SetActive(false);

        if (appSelector != null) {
            appSelector.OpenXbirdApp();
        }
        Debug.Log("새 글 이동: " + latestPostId);
    }

    public void OnClickSubcribeBtn() {
        subcribePanel.SetActive(true);
        UpdateSubscribePanelUI();
    }

    private void UpdateSubscribePanelUI() {
        var btnText = subscribeActionBtn.GetComponentInChildren<TextMeshProUGUI>();
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");

        // 구독 전
        if (isSubscribed == false) {
            // [수정] 구독 제안 문구 현지화
            subcribeText.text = string.Format(LocalizationManager.GetText("MSG_SUB_PROMPT"), monthlyFee.ToString("N0"), unit);

            if (isFreeTrialUsed) {
                if (player.satoshiBankCash < monthlyFee) {
                    btnText.text = LocalizationManager.GetText("BTN_INSUFFICIENT");
                    btnText.color = Color.red;
                    subscribeActionBtn.interactable = false;
                } else {
                    btnText.text = LocalizationManager.GetText("BTN_SUBSCRIBE");
                    btnText.color = Color.white;
                    subscribeActionBtn.interactable = true;
                }
            } else {
                btnText.text = LocalizationManager.GetText("BTN_FREE_TRIAL");
                btnText.color = Color.green;
                subscribeActionBtn.interactable = true;
            }
            return;
        }

        // 구독 중 (취소 미요청)
        if (isCancelRequested == false) {
            // [수정] 구독 중 안내 문구 현지화
            string dateStr = nextBillingDate.ToString("yyyy-MM-dd");
            subcribeText.text = string.Format(LocalizationManager.GetText("MSG_SUB_ACTIVE"), dateStr);

            btnText.text = LocalizationManager.GetText("BTN_CANCEL_SUB");
            btnText.color = Color.white;
            subscribeActionBtn.interactable = true;
            return;
        }

        // 취소 신청 후
        // [수정] 취소 완료 안내 문구 현지화
        string cancelDateStr = nextBillingDate.ToString("yyyy-MM-dd");
        subcribeText.text = string.Format(LocalizationManager.GetText("MSG_SUB_CANCELED"), cancelDateStr);

        btnText.text = LocalizationManager.GetText("BTN_CANCEL_DONE");
        btnText.color = Color.gray;
        subscribeActionBtn.interactable = false;
    }

    public void OnClickSubcribeCancelBtn() {
        subcribePanel.SetActive(false);
    }

    public void OnClickSubscribeAction() {
        if (isSubscribed == false) {
            if (isFreeTrialUsed == false) {
                ApplyFreeTrial();
            } else {
                StartSubscription();
            }
        } else {
            RequestCancelSubscription();
        }

        UpdateSubscribePanelUI();
    }

    private void StartSubscription() {
        if (player.satoshiBankCash < monthlyFee) {
            // [수정] 잔액 부족 경고 현지화
            subcribeText.text = LocalizationManager.GetText("MSG_SUB_LACK_BALANCE");
            return;
        }

        player.satoshiBankCash -= monthlyFee;
        coinManager.UpdateCashText();

        isSubscribed = true;
        isCancelRequested = false;
        isFreeTrialUsed = true;

        nextBillingDate = coinManager.CurrentDateTime.AddDays(30);

        // [수정] 구독 성공 문구 현지화
        string dateStr = nextBillingDate.ToString("yyyy-MM-dd");
        subcribeText.text = string.Format(LocalizationManager.GetText("MSG_SUB_SUCCESS"), dateStr);

        Debug.Log("[구독 시작] 다음 청구일: " + nextBillingDate);
    }

    private void RequestCancelSubscription() {
        isCancelRequested = true;

        // [수정] 취소 신청 문구 현지화
        string dateStr = nextBillingDate.ToString("yyyy-MM-dd");
        subcribeText.text = string.Format(LocalizationManager.GetText("MSG_SUB_CANCEL_REQ"), dateStr);

        Debug.Log("[구독 취소 신청]");
    }

    private void ChargeSubscription() {
        if (player.satoshiBankCash < monthlyFee) {
            isSubscribed = false;
            isCancelRequested = false;
            Debug.Log("[구독 해지] 잔액 부족으로 자동 해지됨");
            return;
        }

        player.satoshiBankCash -= monthlyFee;
        coinManager.UpdateCashText();
        nextBillingDate = nextBillingDate.AddDays(30);

        Debug.Log("[자동 결제 완료] 다음 청구일: " + nextBillingDate);
    }
}