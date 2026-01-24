using UnityEngine;
using UnityEngine.UI;
using System; // Action 사용을 위해 필요

public class GlobalNotificationManager : MonoBehaviour {
    public static GlobalNotificationManager Instance;

    [Header("설정")]
    public Transform notificationContainer;
    public GameObject notificationPrefab;

    // [신규] 메시지 최대 길이 설정 (이 길이를 넘으면 ... 처리)
    public int maxMessageLength = 20;

    [Header("아이콘 리소스")]
    public Sprite iconBullbit;
    public Sprite iconXbird;
    public Sprite iconBank;
    public Sprite iconRealEstate;
    public Sprite iconFournance;

    [Header("배경 색상 설정")]
    public Color colBullbit = new Color32(40, 40, 40, 240);
    public Color colXbird = new Color32(0, 0, 0, 240);
    public Color colBank = new Color32(0, 50, 120, 240);
    public Color colRealEstate = new Color32(0, 100, 50, 240);
    public Color colFournance = new Color32(0, 243, 219, 1);

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowNotification(string type, string sender, string message, Action onClickAction = null) {
        if (notificationPrefab == null || notificationContainer == null) return;

        // [핵심 수정] 메시지 길이 자르기 로직
        string displayMessage = message;
        if (message.Length > maxMessageLength) {
            // 설정한 길이만큼 자르고 뒤에 "..." 붙이기
            displayMessage = message.Substring(0, maxMessageLength) + "...";
        }

        GameObject go = Instantiate(notificationPrefab, notificationContainer);
        NotificationItem item = go.GetComponent<NotificationItem>();

        Sprite icon = null;
        Color bgCol = Color.black;

        switch (type) {
            case "Bullbit":
                icon = iconBullbit;
                bgCol = colBullbit;
                break;
            case "Xbird":
                icon = iconXbird;
                bgCol = colXbird;
                break;
            case "Bank":
                icon = iconBank;
                bgCol = colBank;
                break;
            case "RealEstate":
                icon = iconRealEstate;
                bgCol = colRealEstate;
                break;
            case "Fournance":
                icon = iconFournance;
                bgCol = colRealEstate;
                break;
        }

        // 가공된 displayMessage를 전달
        item.Setup(sender, displayMessage, icon, bgCol, onClickAction);

        go.transform.SetAsLastSibling();
    }
}