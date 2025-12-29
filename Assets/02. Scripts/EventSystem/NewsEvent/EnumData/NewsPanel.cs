using System;
using UnityEngine;

public class NewsPanel : MonoBehaviour {

    public Transform contentParent;
    public GameObject newsItemPrefab;

    public void Show(UIEventData data, DateTime gameTime) {
        var go = Instantiate(newsItemPrefab, contentParent);

        // 최신 뉴스 위로
        go.transform.SetAsFirstSibling();

        ApplySize(go, data);

        var loader = go.GetComponent<NewsLoader>();
        loader.Load(data, gameTime);
    }

    private void ApplySize(GameObject go, UIEventData data) {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;

        if (data.width > 0f) {
            rt.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                data.width
            );
        }

        if (data.height > 0f) {
            rt.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                data.height
            );
        }
    }
}
