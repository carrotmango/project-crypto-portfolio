using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NoticePopupController : MonoBehaviour {
    [Header("UI References")]
    public GameObject noticePanel;       // 공지사항 전체 패널
    public TextMeshProUGUI versionText;  // 버전 텍스트
    public TextMeshProUGUI contentText;  // 내용 텍스트
    public Button closeButton;           // 닫기 버튼

    [Header("Settings")]
    public string versionTag = "Project Crypto 0.9.5 Version";

    void Start() {
        // 버튼 리스너 연결
        if (closeButton != null) closeButton.onClick.AddListener(ClosePopup);

        // 게임 시작 시 팝업 띄우기
        ShowNotice();
    }

    public void ShowNotice() {
        if (noticePanel == null) return;

        // 1. 버전 텍스트 설정
        if (versionText != null) versionText.text = versionTag;

        // 2. 내용 작성 (Rich Text 활용으로 가독성 업)
        if (contentText != null) {
            contentText.text =
                "<size=120%><color=#14FF08>플레이해주셔서 진심으로 감사합니다!</color></size>\n\n" +
                "<b>[알려진 버그 안내]</b>\n" +
                "• 불비트에서 에어드랍을 받았거나,\n" +
                "• 보유 코인 종목이 1개인 경우 <color=red>전액 매도가 되지 않는 현상</color>이 있습니다!\n" +
                "해당 부분은 현재 수정 중이오니 양해 부탁드립니다.\n\n" +
                "<b>[커뮤니티 및 제보]</b>\n" +
                "버그 및 건의사항은 <color=#7289DA>Discord</color> 또는 메일로 제보해 주세요!\n" +
                "이메일: <color=#FFD700>projectcrypto11@gmail.com</color>";
        }

        noticePanel.SetActive(true);

        // 팝업이 뜰 때 게임 시간을 멈추고 싶다면 아래 주석 해제 (CoinManager 연동)
        // if(CoinManager.Instance != null) CoinManager.Instance.SetTimeSpeed(TimeSpeed.Paused);
    }

    public void ClosePopup() {
        noticePanel.SetActive(false);
        Debug.Log("공지사항을 확인했습니다.");

        // 닫을 때 다시 시간을 흐르게 하고 싶다면
        // if(CoinManager.Instance != null) CoinManager.Instance.SetTimeSpeed(TimeSpeed.Normal);
    }
}