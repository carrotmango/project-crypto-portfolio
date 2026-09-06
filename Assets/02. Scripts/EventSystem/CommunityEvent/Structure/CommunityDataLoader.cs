using UnityEngine;

public class CommunityDataLoader : MonoBehaviour {
    public static CommunityDataLoader Instance;

    public CommunityPostDB PostDB { get; private set; }
    public UserDB UserDB { get; private set; }

    private void Awake() {
        // 로비/인게임 왔다갔다 할 때 인스턴스 꼬임 방지
        Instance = this;
        LoadData();
    }

    public void LoadData() {
        // [수정] GameMenuController랑 똑같은 키값("Saved_Language")으로 가져와야 합니다!!
        int currentLang = PlayerPrefs.GetInt("Saved_Language", 1);

        // 0이면 영어(_En), 1이면 한국어(접미사 없음)
        string suffix = (currentLang == 0) ? "_En" : "";

        // 1. 데이터 로드 (파일명: Community_Noise / Community_Noise_En)
        TextAsset noiseJson = Resources.Load<TextAsset>($"json/Community_Noise{suffix}");
        TextAsset userJson = Resources.Load<TextAsset>($"json/Community_Users{suffix}");

        if (noiseJson != null) {
            PostDB = JsonUtility.FromJson<CommunityPostDB>(noiseJson.text);
            Debug.Log($"[Community] 템플릿 로드 완료: {noiseJson.name}");
        } else {
            Debug.LogError($"[Community] 파일을 찾을 수 없음: json/Community_Noise{suffix}");
        }

        if (userJson != null) {
            UserDB = JsonUtility.FromJson<UserDB>(userJson.text);
            Debug.Log($"[Community] 유저 로드 완료: {userJson.name}");
        }
    }
}