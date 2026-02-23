using UnityEngine;
using UnityEngine.UI;

public class DiscordLinkHandler : MonoBehaviour {
    [Header("디스코드 설정")]
    // 여기에 행님의 디스코드 서버 초대 링크를 넣으세요!
    public string discordInviteUrl = "https://discord.gg/temDDBAB";

    void Start() {
        // 만약 이 오브젝트에 Button 컴포넌트가 있다면 자동으로 연결해줍니다.
        Button btn = GetComponent<Button>();
        if (btn != null) {
            btn.onClick.AddListener(OpenDiscord);
        }
    }

    // 이미지를 클릭했을 때 실행될 함수
    public void OpenDiscord() {
        if (!string.IsNullOrEmpty(discordInviteUrl)) {
            // 외부 브라우저를 실행해 링크를 엽니다.
            Application.OpenURL(discordInviteUrl);
            Debug.Log($"[시스템] 디스코드 연결 시도: {discordInviteUrl}");
        } else {
            Debug.LogWarning("[시스템] 디스코드 링크가 비어있습니다!");
        }
    }
}