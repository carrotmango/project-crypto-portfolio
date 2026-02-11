using UnityEngine;

public class CommunityDataLoader : MonoBehaviour {
    public static CommunityDataLoader Instance;

    // 인스펙터에서 확인용 (디버깅용)
    public TextAsset communityJson;
    public TextAsset userJson;

    public CommunityPostDB PostDB { get; private set; }
    public UserDB UserDB { get; private set; }

    private void Awake() {
        if (Instance == null) Instance = this;
        LoadData();
    }

    public void LoadData() {
        // =========================================================
        // [핵심] Assets/Resources/json/ 폴더 안의 파일을 자동으로 로드
        // 파일명: community_noise, community_users (확장자 제외)
        // =========================================================

        // 1. 똥글 템플릿 로드 (경로: json/community_noise)
        communityJson = Resources.Load<TextAsset>("json/Community_Noise");

        // 2. 유저 DB 로드 (경로: json/community_users)
        userJson = Resources.Load<TextAsset>("json/Community_Users");

        // 3. 파싱 (데이터 변환)
        if (communityJson != null) {
            PostDB = JsonUtility.FromJson<CommunityPostDB>(communityJson.text);
            Debug.Log($"[Community] 똥글 템플릿 로드 성공: {PostDB.templates.Count}개");
        } else {
            Debug.LogError("[Community] 'Resources/json/community_noise' 파일을 찾을 수 없습니다!");
        }

        if (userJson != null) {
            UserDB = JsonUtility.FromJson<UserDB>(userJson.text);
            Debug.Log($"[Community] 유저 DB 로드 성공: {UserDB.names.Length}명");
        } else {
            Debug.LogError("[Community] 'Resources/json/community_users' 파일을 찾을 수 없습니다!");
        }
    }
}