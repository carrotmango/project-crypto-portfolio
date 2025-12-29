using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

#region Data Structure

[System.Serializable]
public class XTargetGroup {
    public string[] symbols;
    public float priceChangeMin;
    public float priceChangeMax;
    public bool priceChangeFixed;
    public string marketPhaseToSet;
    public int durationHours;

    public int coinEventType = 0;      // Delist / Relist 대응용 더미
    public int targetMarketPhase = 0;  // enum MarketPhase 대응 더미
    public double relistBasePrice = 0; // 재상장 베이스가 대응용
    public string eventType;
}


[System.Serializable]
public class XPostData {
    public string key;
    public string profileImage;
    public string name;
    public string date;
    public string time;
    [TextArea] public string content;
    public string contentImage;
    public float width = 550f;
    public float height = 700f;

    public string startDate;
    public string endDate;
    public bool eventMustRequired;

    public bool overrideMarketPhase;
    public string marketPhaseToSet;       // 전체 시장용
    public string marketPhaseAfter;

    public int durationHours;             // 전체 시장 이벤트 지속시간
    public bool affectAllCoins;

    public XTargetGroup[] targetGroups;

    public bool postYn = true;
    public string eventType;
    public string authorId;
    public string uiKey;

}

#endregion

public class XPostLoader : MonoBehaviour {
    // UI
    public Image profileImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI contentText;

    public GameObject contentImageContainer;
    public Image contentImage;

    // Saved Data (UI + 이벤트 시스템)
    public string key;
    public string authorName;
    public string profileImgName;
    public string contentImgName;
    public string date;
    public string time;
    public string content;
    public float width;
    public float height;

    public bool eventMustRequired;
    public bool overrideMarketPhase;
    public string marketPhaseToSet;
    public string marketPhaseAfter;
    public int durationHours;
    public bool affectAllCoins;

    public string startDate;
    public string endDate;

    public XTargetGroup[] targetGroups;

    public bool postYn;
    public XPostData originalData;


    public void Load(XPostData data) {
        // UI 반영
        nameText.text = data.name ?? "Unknown";
        dateText.text = data.date ?? "--/--/----";
        timeText.text = data.time ?? "--:--";
        contentText.text = data.content ?? "(No content)";

        TrySetSpriteOrHide(profileImage, "image/profile", data.profileImage, null);
        TrySetSpriteOrHide(contentImage, "image/content", data.contentImage,
            contentImageContainer ?? contentImage?.gameObject);

        // 저장될 값들
        key = data.key;
        authorName = data.name;
        profileImgName = data.profileImage;
        contentImgName = data.contentImage;

        date = data.date;
        time = data.time;
        content = data.content;

        width = data.width;
        height = data.height;

        postYn = data.postYn;

        // 이벤트 정보 저장
        eventMustRequired = data.eventMustRequired;

        overrideMarketPhase = data.overrideMarketPhase;
        marketPhaseToSet = data.marketPhaseToSet;         // 전체 시장
        marketPhaseAfter = data.marketPhaseAfter;

        durationHours = data.durationHours;               // 전체 지속시간
        affectAllCoins = data.affectAllCoins;

        startDate = data.startDate;
        endDate = data.endDate;

        // 타겟 그룹
        targetGroups = data.targetGroups;

        originalData = data;
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
        }
    }
}
