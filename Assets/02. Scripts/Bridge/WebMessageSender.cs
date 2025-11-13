using UnityEngine;

public class WebMessageSender : MonoBehaviour
{
    [Header("플레이어 기본 정보")]
    [Tooltip("React에서 전달받은 지갑 주소 (WebGL 전용)")]
    public string connectedWallet = "local";

    [Tooltip("현재 플레이어 닉네임")]
    public string playerName = "Player";

    [Tooltip("현재 총 자산 (불비트 + 은행 등 포함)")]
    public double totalAsset = 0;

    [Header("연동 매니저")]
    public CoinManager coinManager;

    private int lastSyncedDay = -1;

    void Start()
    {
        // 게임 처음 켤 때 자동으로 전송되지 않도록 현재 날짜로 동기화
        if (coinManager != null)
        {
            lastSyncedDay = coinManager.survivalDays;
        }

        // Debug.Log("[WebMessageSender] 초기화 완료 - React로부터 데이터 수신 대기 중");
    }

    void Update()
    {
        if (coinManager == null) return;

        int currentDay = coinManager.survivalDays;
        totalAsset = coinManager.GetTotalUserAsset();

        // Debug.Log("현재 survivalDays: " + coinManager.survivalDays);

        // 하루가 바뀌었을 때만 전송
        if (currentDay != lastSyncedDay)
        {
            lastSyncedDay = currentDay;
            SendPlayerDataToWeb();
            Debug.Log("[WebMessageSender] 플레이어 데이터 전송 완료");
        }
    }

    // React -> Unity : React에서 지갑 주소를 전달받을 때 호출됨
    public void SetWalletAddress(string wallet)
    {
        if (string.IsNullOrEmpty(wallet))
        {
            connectedWallet = "local";
            // Debug.Log("[WebMessageSender] React에서 전달받은 지갑 주소 없음 - Local 모드로 설정");
        }
        else
        {
            connectedWallet = wallet;
            // Debug.Log("[WebMessageSender] React에서 전달받은 지갑 주소: " + connectedWallet);
        }
    }

    // Unity -> React : 플레이어 데이터를 웹으로 전송
    [ContextMenu("Send Player Data To Web")]
    public void SendPlayerDataToWeb()
    {
        PlayerSyncData data = new PlayerSyncData(connectedWallet, playerName, totalAsset);
        string json = JsonUtility.ToJson(data);

        // Debug.Log("[Unity → Web] 전송 JSON: " + json);
        SendMessageToWeb(json);
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void SendMessageToWeb(string msg);
#else
    private static void SendMessageToWeb(string msg)
    {
        Debug.Log("(에디터 전용) Web 메시지 시뮬레이션: " + msg);
    }
#endif
}
