using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

#region Data Structure

[System.Serializable]
public class XTargetGroup {
    public string[] symbols;                      // 타겟 코인들
    public float priceChangeMin = 0f;
    public float priceChangeMax = 0f;
    public bool priceChangeFixed = false;         // true: 고정, false: 랜덤
    public string marketPhaseToSet = "";          // 개별 코인에 부여될 페이즈
    public int durationHours = 6;                 // 개별 코인에 적용되는 지속 시간

}

[System.Serializable]
public class XPostData {
    [Header("기본 정보")]
    public string key;
    public string profileImage;
    public string name;
    public string date;  // "MM/dd/yyyy"
    public string time;  // "HH:mm"
    [TextArea] public string content;
    public string contentImage;
    public float width = 550f;
    public float height = 700f;
    public string eventType;

    [Header("이벤트 시간 설정")]
    public string startDate;  // "MM/dd/yyyy HH:mm"
    public string endDate;
    public bool eventMustRequired = false;

    [Header("시장 상태 영향")]
    public bool overrideMarketPhase = false;
    public string marketPhaseToSet;
    public string marketPhaseAfter;

    [Header("이벤트 지속 시간")]
    public int durationHours = 6;

    [Header("전체 시장 영향 설정")]
    public bool affectAllCoins = false;  

    [Header("타겟 코인 그룹 영향 설정")]
    public XTargetGroup[] targetGroups;

    public bool postYn = true;
}

#endregion

public class XPostLoader : MonoBehaviour {
    [Header("Refs")]
    public Image profileImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI contentText;

    public GameObject contentImageContainer;
    public Image contentImage;

    public void Load(XPostData data) {
        nameText.text = data.name ?? "Unknown";
        dateText.text = data.date ?? "--/--/----";
        timeText.text = data.time ?? "--:--";
        contentText.text = data.content ?? "(No content)";

        TrySetSpriteOrHide(profileImage, "image/profile", data.profileImage, null);
        TrySetSpriteOrHide(contentImage, "image/content", data.contentImage, contentImageContainer ?? contentImage?.gameObject);
    }

    private void TrySetSpriteOrHide(Image target, string folder, string fileName, GameObject containerToToggle) {
        if (string.IsNullOrEmpty(fileName)) {
            if (containerToToggle != null) containerToToggle.SetActive(false);
            else if (target != null) target.enabled = false;
            return;
        }

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string path = $"{folder}/{baseName}";
        var sprite = Resources.Load<Sprite>(path);

        if (sprite == null) {
            if (containerToToggle != null) containerToToggle.SetActive(false);
            else if (target != null) target.enabled = false;
            return;
        }

        if (containerToToggle != null) containerToToggle.SetActive(true);
        if (target != null) {
            target.enabled = true;
            target.sprite = sprite;
            // target.SetNativeSize(); // 필요 시
        }
    }
}
