using UnityEngine;

public class XFeedSpawner : MonoBehaviour {
    public GameObject xPrefab;      // 1에서 만든 XPrefab
    public Transform contentParent; // ScrollView/Viewport/Content

    public GameObject SpawnFromData(XPostData data) {
        var go = Instantiate(xPrefab, contentParent);

        go.transform.SetAsFirstSibling();

        // JSON width/height가 있으면 프리팹 크기를 즉시 덮어씀
        var rt = go.GetComponent<RectTransform>();
        if (rt != null) {
            if (data.width > 0f)
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, data.width);
            if (data.height > 0f)
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, data.height);
        }

        // 콘텐츠 로딩
        go.GetComponent<XPostLoader>()?.Load(data);
        return go;
    }
}
