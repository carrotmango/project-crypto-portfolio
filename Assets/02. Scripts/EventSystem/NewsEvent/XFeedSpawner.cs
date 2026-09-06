using System.Collections.Generic;
using UnityEngine;

public class XFeedSpawner : MonoBehaviour {
    public GameObject xPrefab;
    public Transform contentParent;
    public static XFeedSpawner Instance;
    public List<GameObject> spawnedPosts = new List<GameObject>();

    void Awake() {
        Instance = this;
    }


    // ------------------------------------------------
    // 1) 플레이 중 새로운 이벤트 생성
    // ------------------------------------------------
    public GameObject SpawnFromData(XPostData data) {

        if (!data.postYn) {
            return null;
        }

        if (xPrefab == null || contentParent == null) return null;

        var go = Instantiate(xPrefab, contentParent);
        spawnedPosts.Add(go);

        // 항상 최신 글이 위로
        go.transform.SetAsFirstSibling();

        // UI 크기 설정
        ApplySize(go, data);

        // 로드
        var loader = go.GetComponent<XPostLoader>();
        loader.Load(data);
        loader.originalData = data; // 이벤트 원본 유지

        return go;
    }


    // ------------------------------------------------
    // 2) 저장 데이터 전체 로딩 (불러오기)
    // ------------------------------------------------
    public void LoadFeed(List<SavedXPost> savedPosts) {
        if (xPrefab == null || contentParent == null) return;

        // 기존 UI 제거
        foreach (var go in spawnedPosts)
            if (go != null) Destroy(go);

        spawnedPosts.Clear();

        // 저장된 모든 포스트를 UI로 재생성
        // savedPosts의 순서가 곧 화면 표시 순서

        savedPosts.Reverse();

        foreach (var saved in savedPosts) {
            XPostData data = saved.ToXPostData();

            var go = Instantiate(xPrefab, contentParent);

            // UI 크기 적용
            ApplySize(go, data);

            var loader = go.GetComponent<XPostLoader>();
            loader.Load(data);
            loader.originalData = data;

            // 순서 유지: 최신 글이 가장 위에 있도록 FirstSibling
            go.transform.SetAsLastSibling();

            spawnedPosts.Add(go);
        }
    }


    // ------------------------------------------------
    // 크기 복원 유틸
    // ------------------------------------------------
    private void ApplySize(GameObject go, XPostData data) {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;

        if (data.width > 0f)
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, data.width);

        if (data.height > 0f)
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, data.height);
    }
}
