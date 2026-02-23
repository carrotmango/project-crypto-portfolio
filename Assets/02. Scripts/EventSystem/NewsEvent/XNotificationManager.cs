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

    [Header("무료 체험 설정")] // [추가됨]
    public bool isFreeTrialUsed = false; // 무료 체험을 이미 사용했는지 체크

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

    // [추가됨] 게임 시작 시 무료 체험 체크
    private void Start() {
        // 아직 구독 안 했고, 무료 체험도 안 썼다면 -> 무료 체험 시작
        if (isSubscribed == false && isFreeTrialUsed == false) {
            ApplyFreeTrial();
        }
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

    // [추가됨] 무료 체험 적용 함수
    public void ApplyFreeTrial() {
        isSubscribed = true;
        isCancelRequested = true;
        isFreeTrialUsed = true; // 무료 체험 사용 처리

        // 돈은 깎지 않고, 다음 결제일만 30일 뒤로 설정
        // CoinManager가 초기화된 직후여야 하므로 안전하게 날짜를 가져옵니다.
        if (coinManager != null) {
            nextBillingDate = coinManager.CurrentDateTime.AddDays(30);
        } else {
            // 혹시 CoinManager가 아직 준비 안 됐을 경우 대비 (기본값)
            nextBillingDate = new DateTime(2020, 1, 1).AddDays(30);
        }

        Debug.Log($"[무료 체험 시작] 30일간 무료! 다음 청구일: {nextBillingDate}, 자동 해지예정");
    }

    // 알림 표시
    // Show 함수 수정
    // 알림 표시 함수
    public void Show(string authorName, string postId) {
        if (isSubscribed == false) return;

        latestPostId = postId;

        if (GlobalNotificationManager.Instance != null) {
            GlobalNotificationManager.Instance.ShowNotification(
                "Xbird",           // 타입: Xbird (검정 배경)
                "Xbird 알림",       // 보낸사람
                $"{authorName} 님이 새 글을 게시했습니다.", // 내용
                () => {            // [핵심] 클릭 시 실행할 람다 함수
                    if (appSelector != null) {
                        appSelector.OpenXbirdApp(); // 앱 열기
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

            // [수정] 이미 무료체험을 썼는데 구독이 안 되어 있다면 (재구독 상황)
            if (isFreeTrialUsed) {
                if (player.satoshiBankCash < monthlyFee) {
                    btnText.text = "잔액 부족";
                    btnText.color = Color.red;
                    subscribeActionBtn.interactable = false;
                } else {
                    btnText.text = "구독하기";
                    btnText.color = Color.white;
                    subscribeActionBtn.interactable = true;
                }
            }
            // [수정] 무료체험을 아직 안 썼다면 (혹시 Start에서 적용 안 됐을 경우 대비)
            else {
                btnText.text = "무료체험 시작";
                btnText.color = Color.green; // 초록색 등으로 강조
                subscribeActionBtn.interactable = true;
            }

            return;
        }

        // 구독 중 (취소 미요청)
        if (isCancelRequested == false) {

            // [수정] UI에 무료 체험 중인지 표시해주면 더 좋습니다.
            string status = "현재 구독 중입니다.";
            // 만약 돈을 안 냈는데 구독 중이라면 (간단한 체크 로직)
            // 여기서는 변수 하나를 더 쓰기보다 그냥 날짜와 상태로 보여줍니다.

            subcribeText.text =
                $"{status}\n" +
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
            // [수정] 버튼 눌렀을 때도 무료 체험 안 썼으면 무료로 시작
            if (isFreeTrialUsed == false) {
                ApplyFreeTrial();
            } else {
                StartSubscription(); // 유료 구독
            }
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
        isFreeTrialUsed = true; // 유료 결제하면 무료 체험권도 소진된 것으로 처리

        // 테스트용 30일 청구
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