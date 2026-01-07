using UnityEngine;
using UnityEngine.UI;
using System; // Action 사용을 위해 필요

public class GlobalNotificationManager : MonoBehaviour {
    public static GlobalNotificationManager Instance;

    [Header("설정")]
    public Transform notificationContainer;
    public GameObject notificationPrefab;

    [Header("아이콘 리소스")]
    public Sprite iconBullbit;
    public Sprite iconXbird;
    public Sprite iconBank;
    public Sprite iconRealEstate;

    [Header("배경 색상 설정")] // [신규]
    public Color colBullbit = new Color32(40, 40, 40, 240);    // 검회색
    public Color colXbird = new Color32(0, 0, 0, 240);         // 검정 (X 느낌)
    public Color colBank = new Color32(0, 50, 120, 240);       // 파랑 (은행 느낌)
    public Color colRealEstate = new Color32(0, 100, 50, 240); // 초록 (부동산 느낌)

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // [수정] onClickAction 매개변수 추가 (기본값 null)
    public void ShowNotification(string type, string sender, string message, Action onClickAction = null) {
        if (notificationPrefab == null || notificationContainer == null) return;

        GameObject go = Instantiate(notificationPrefab, notificationContainer);
        NotificationItem item = go.GetComponent<NotificationItem>();

        Sprite icon = null;
        Color bgCol = Color.black; // 기본값

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
        }

        // 아이콘, 색상, 클릭 이벤트를 모두 넘겨줌
        item.Setup(sender, message, icon, bgCol, onClickAction);

        go.transform.SetAsLastSibling();
    }
}