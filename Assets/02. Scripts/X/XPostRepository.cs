using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class XPostDB {
    public XAuthor[] authors;
}

[System.Serializable]
public class XAuthor {
    public string id;
    public string defaultProfileImage;
    public XPostData[] events;
}

public class XPostRepository : MonoBehaviour {
    [Header("json/News")]
    public TextAsset allPostsJson; // 비워두면 Resources에서 로드

    private Dictionary<string, Dictionary<string, XPostData>> map;

    // 외부에서 접근 가능한 전체 authors 정보
    public List<XAuthor> authors { get; private set; } = new();

    private void Awake() => Load();

    public void Load() {
        if (allPostsJson == null)
            allPostsJson = Resources.Load<TextAsset>("json/News");

        if (allPostsJson == null) {
            Debug.LogError("News.json 없어용: Resources/json");
            map = new();
            return;
        }

        var db = JsonUtility.FromJson<XPostDB>(allPostsJson.text);
        map = new();
        authors = new List<XAuthor>();

        if (db?.authors == null) return;

        foreach (var author in db.authors) {
            var inner = new Dictionary<string, XPostData>();
            if (author.events != null) {
                foreach (var ev in author.events) {
                    // 프로필 기본값 보정
                    if (string.IsNullOrEmpty(ev.profileImage) && !string.IsNullOrEmpty(author.defaultProfileImage))
                        ev.profileImage = author.defaultProfileImage;

                    if (!string.IsNullOrEmpty(ev.key))
                        inner[ev.key] = ev;
                }
            }

            map[author.id] = inner;
            authors.Add(author);
        }
    }

    public bool TryGet(string authorId, string eventKey, out XPostData data) {
        data = null;
        return map != null
            && map.TryGetValue(authorId, out var inner)
            && inner.TryGetValue(eventKey, out data);
    }
}
