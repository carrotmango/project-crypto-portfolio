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
                "<size=125%><color=#14FF08>프로젝트 크립토 데모 버전에 오신 것을 환영합니다!</color></size>\n\n" +

                "<b>[데모 버전 안내]</b>\n" +
                "• 데모 버전은 <color=#FF6347>게임 시간 4월 2일</color>에 종료됩니다.\n" +
                "• 별도의 튜토리얼은 없으며, 여러분의 <color=#00BFFF>플레이 데이터</color>를 기반으로\n" +
                "  향후 정식 튜토리얼이 제작될 예정입니다!\n\n" +

                "<b>[개발 진행 중인 사항]</b>\n" +
                "• <color=#FFD700>비서 전화 기능</color> 및 <color=#FFD700>업무 패널 메시지 로그</color>는 현재 미구현 상태입니다.\n" +
                "• <color=#FFD700>캐릭터 선택 초상화</color> 및 <color=#FFD700>로딩 스크린 이미지</color>는 현재 임시 리소스이며 정식 데모 버전에서 변경될 예정입니다.\n" +
                "• <color=#FFD700>편의점 명칭</color> 및 <color=#FFD700>코인 이름</color>은 추후 업데이트 시 모두 변경됩니다.\n" +
                "• 편의성 및 피드 UI 피드백 일부 반영되었습니다.\n\n" +

                "<b>[목표]</b>\n" +
                "운명의 날이 오기 전까지 시장의 흐름을 읽고, 과감한 투자로 \n" +
                "세계 최고의 <color=#FFD700>자산가</color> 자리에 도전해 보세요!\n\n" +

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