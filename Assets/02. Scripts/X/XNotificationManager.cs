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
    public int monthlyFee = 1000000;
    public DateTime nextBillingDate;

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

    private void Update() {
        if (isSubscribed == false) {
            return;
        }

        DateTime now = coinManager.CurrentDateTime;

        // 취소 신청된 상태 > 결제일 도달 시 완전 해지
        if (isCancelRequested == true) {
            if (now >= nextBillingDate) {
                isSubscribed = false;
                isCancelRequested = false;
                Debug.Log("[구독 종료] 취소 신청 → 결제일 도달, 자동 해지");
            }
            return;
        }

        // 일반 구독 상태 >> 자동 결제
        if (now >= nextBillingDate) {
            ChargeSubscription();
        }
    }

    // 알림 표시
    public void Show(string authorName, string postId) {
        if (isSubscribed == false) {
            return;
        }

        latestPostId = postId;
        messageText.text = $"{authorName} 님이 게시글을 올렸습니다.";

        panel.SetActive(true);

        if (routine != null) {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(NotificationRoutine());
    }

    private IEnumerator NotificationRoutine() {
        float t = 0f;
        float duration = 0.25f;
        cg.alpha = 0f;

        // slide in
        while (t < duration) {
            t += Time.deltaTime;
            float n = t / duration;

            cg.alpha = Mathf.Lerp(0f, 1f, n);
            float y = Mathf.Lerp(hiddenY, shownY, n);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);

            yield return null;
        }

        cg.alpha = 1f;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, shownY);

        yield return new WaitForSeconds(3f);

        // slide out
        t = 0f;
        while (t < duration) {
            t += Time.deltaTime;
            float n = t / duration;

            cg.alpha = Mathf.Lerp(1f, 0f, n);
            float y = Mathf.Lerp(shownY, hiddenY, n);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);

            yield return null;
        }

        cg.alpha = 0f;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, hiddenY);
        panel.SetActive(false);
    }

    private void OnClickNotification() {
        panel.SetActive(false);

        if (appSelector != null) {
            appSelector.OpenXbirdApp();
        }

        Debug.Log("새 글 이동: " + latestPostId);
    }

    // 구독 패널 열기
    public void OnClickSubcribeBtn() {
        subcribePanel.SetActive(true);
        UpdateSubscribePanelUI();
    }

    // UI 갱신
    private void UpdateSubscribePanelUI() {

        var btnText = subscribeActionBtn.GetComponentInChildren<TextMeshProUGUI>();

        // 구독 전
        if (isSubscribed == false) {

            subcribeText.text =
                "Xbird를 구독하시겠습니까?\n" +
                "구독 시 상시 알림 혜택을 받습니다.\n\n" +
                $"월 구독료: {monthlyFee:N0}원\n" +
                "30일마다 사토시 은행에서 자동 결제됩니다.";

            if (player.satoshiBankCash < monthlyFee) {
                btnText.text = "잔액 부족";
                btnText.color = Color.red;
                subscribeActionBtn.interactable = false;
            } else {
                btnText.text = "구독하기";
                btnText.color = Color.white;
                subscribeActionBtn.interactable = true;
            }

            return;
        }

        // 구독 중 (취소 미요청)
        if (isCancelRequested == false) {

            subcribeText.text =
                $"현재 구독 중입니다.\n" +
                $"다음 청구일: {nextBillingDate:yyyy-MM-dd}\n" +
                "구독 취소 시 다음 결제일까지 서비스 이용 가능\n" +
                "다음 결제는 진행되지 않습니다.";

            btnText.text = "구독취소";
            btnText.color = Color.white;
            subscribeActionBtn.interactable = true;

            return;
        }

        // 취소 신청 후
        subcribeText.text =
            $"이미 구독 취소 신청을 하셨습니다.\n" +
            $"이용 가능 기간: {nextBillingDate:yyyy-MM-dd} 까지\n" +
            "이후 자동 해지됩니다.";

        btnText.text = "취소완료";
        btnText.color = Color.gray;
        subscribeActionBtn.interactable = false;
    }

    public void OnClickSubcribeCancelBtn() {
        subcribePanel.SetActive(false);
    }

    // 버튼 동작
    public void OnClickSubscribeAction() {
        if (isSubscribed == false) {
            StartSubscription();
        } else {
            RequestCancelSubscription();
        }

        UpdateSubscribePanelUI();
    }

    private void StartSubscription() {
        if (player.satoshiBankCash < monthlyFee) {
            subcribeText.text = "은행 잔고가 부족합니다.";
            return;
        }

        // 첫 결제
        player.satoshiBankCash -= monthlyFee;
        coinManager.UpdateCashText();

        isSubscribed = true;
        isCancelRequested = false;

        // 테스트용 3일 청구
        nextBillingDate = coinManager.CurrentDateTime.AddDays(30);

        subcribeText.text =
            $"구독이 완료되었습니다.\n" +
            $"다음 청구일: {nextBillingDate:yyyy-MM-dd}";

        Debug.Log("[구독 시작] 다음 청구일: " + nextBillingDate);
    }

    private void RequestCancelSubscription() {
        isCancelRequested = true;

        subcribeText.text =
            $"구독 취소 신청 완료.\n" +
            $"이용 가능 기간: {nextBillingDate:yyyy-MM-dd} 까지";

        Debug.Log("[구독 취소 신청]");
    }

    // 자동 결제
    private void ChargeSubscription() {
        if (player.satoshiBankCash < monthlyFee) {

            isSubscribed = false;
            isCancelRequested = false;

            Debug.Log("[구독 해지] 잔액 부족으로 자동 해지됨");
            return;
        }

        // 정상 결제
        player.satoshiBankCash -= monthlyFee;
        coinManager.UpdateCashText();

        nextBillingDate = nextBillingDate.AddDays(30);

        Debug.Log("[자동 결제 완료] 다음 청구일: " + nextBillingDate);
    }
}
