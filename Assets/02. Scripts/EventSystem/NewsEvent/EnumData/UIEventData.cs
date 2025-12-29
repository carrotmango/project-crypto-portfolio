using System;

[Serializable]
public class UIEventData {
    public string key;
    public UIEventType type;

    public string authorName;
    public string title;
    public string message;

    public string profileImage;
    public string contentImage;
    public float height;
    public float width;
}

public enum UIEventType {
    News = 0,
    RumorSimple = 1,
    RumorDM = 2
}

