using UnityEngine;
using TMPro;

public class WebBridgeManager : MonoBehaviour
{
    public static string walletAddress = "";
    [SerializeField] private TextMeshProUGUI walletText;

    void Start()
    {
#if UNITY_EDITOR
        walletAddress = "Local";
        Debug.Log("Editor 모드 — Local 모드로 설정됨");
        UpdateWalletUI();
#else
    Debug.Log("게임 시작 — React에서 지갑 주소 대기 중...");
#endif
    }


    public void SetWalletAddress(string wallet)
    {
        if (string.IsNullOrEmpty(wallet))
        {
            walletAddress = "Local"; // 
            Debug.Log("React 지갑주소 없음 >> Local 모드로 설정");
        }
        else
        {
            walletAddress = wallet;
            Debug.Log("React로부터 받은 지갑 주소: " + wallet);
        }

        UpdateWalletUI();
    }

    public void UpdateWalletUI()
    {
        if (walletText != null)
        {
            walletText.text = "지갑 주소: " + walletAddress;
        }
    }
}
