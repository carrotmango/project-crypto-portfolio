using System;

[Serializable]
public class SavedXPost {
    public string key;
    public string name;
    public string content;
    public string profileImage;
    public string contentImage;
    public string date;
    public string time;
    public float width;
    public float height;

    public bool eventMustRequired;
    public bool postYn;
    public string eventType;

    public string startDate;
    public string endDate;

    public bool overrideMarketPhase;
    public string marketPhaseToSet;
    public string marketPhaseAfter;

    public int durationHours;

    public bool affectAllCoins;

    public XTargetGroup[] targetGroups;
    public string authorId;

    // Saved ¡æ XPostData º¯È¯
    public XPostData ToXPostData() {
        return new XPostData {
            key = key,
            name = name,
            content = content,
            profileImage = profileImage,
            contentImage = contentImage,
            date = date,
            time = time,
            width = width,
            height = height,

            eventMustRequired = eventMustRequired,
            postYn = postYn,
            eventType = eventType,

            startDate = startDate,
            endDate = endDate,

            overrideMarketPhase = overrideMarketPhase,
            marketPhaseToSet = marketPhaseToSet,
            marketPhaseAfter = marketPhaseAfter,

            durationHours = durationHours,
            affectAllCoins = affectAllCoins,

            targetGroups = targetGroups
        };
    }
}
