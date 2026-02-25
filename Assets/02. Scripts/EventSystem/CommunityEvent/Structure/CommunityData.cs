using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PostTemplate {
    [Header("Content")]
    public string content;        // 내용 ("{0} 가즈아!")
    public string contentImage;   // 짤방 (옵션)
    public float width = 550f;
    public float height = 350f;   // 텍스트만 있으면 350, 이미지 있으면 700 추천

    [Header("Fixed NPC (Optional)")]
    public string fixedName;      // 고정 닉네임 (비워두면 랜덤)
    public string fixedProfile;   // 고정 프사 (비워두면 랜덤)

    [Header("Conditions")]
    public string[] targetSymbols; // 특정 코인 전용
    public double minPrice = 0;    // 최소 가격
    public double maxPrice = 0;    // 최대 가격
    public double minChange = 0;   // 최소 변동률 
    public double maxChange = 0;   // 최대 변동률 
    public int minCoins = 1; // 기본값 1
    public int maxCoins = 1; // 기본값 1

    public string startDate;       // "MM-dd"
    public string endDate;         // "MM-dd"
    public int minHour = -1;       // 0~23
    public int maxHour = -1;       // 0~23

    public bool isOneTime = false;
    public float customCooltime = 0f;
}

[System.Serializable]
public class UserDB {
    public string[] profiles; // 랜덤 프사 풀
    public string[] names;    // 랜덤 닉네임 풀
}

[System.Serializable]
public class CommunityPostDB {
    public List<PostTemplate> templates;
}