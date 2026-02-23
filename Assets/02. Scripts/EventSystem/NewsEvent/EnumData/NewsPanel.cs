using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 텍스트 제어를 위해 추가

public class NewsPanel : MonoBehaviour {

    public Transform contentParent;
    public GameObject newsItemPrefab;
    public NewsRepository newsRepo;
    private NewsCategory currentCategory = NewsCategory.All;
    public GameObject emptyNoticeObject;

    public RetroToggleController communityToggle;

    [Header("탭 텍스트 설정")]
    // 이제 이미지 대신 텍스트 컴포넌트를 직접 넣습니다.
    public List<TextMeshProUGUI> tabTexts;
    public Color activeTextColor = new Color32(0, 191, 255, 255); // 활성화된 글자색 (하늘색)
    public Color inactiveTextColor = Color.white; // 비활성화된 글자색 (흰색)

    private void Start() {
        // NewsCategory.All은 0번이므로 0번 탭을 새로고침합니다.
        RefreshXbird(NewsCategory.All);
    }

    public void RefreshXbird(NewsCategory category) {
        currentCategory = category;
        UpdateTabVisuals((int)category);

        foreach (Transform child in contentParent) Destroy(child.gameObject);

        var history = newsRepo.GetNewsByCategory(category);

        if (history == null || history.Count == 0) {
            if (emptyNoticeObject != null) emptyNoticeObject.SetActive(true);
        } else {
            if (emptyNoticeObject != null) emptyNoticeObject.SetActive(false);

            foreach (var item in history) {
                // [핵심 필터 로직]
                // 1. 이 글이 시스템 생성 똥글(noise_)인가?
                bool isNoise = item.data.key.StartsWith("noise_");
                // 2. 현재 토글이 꺼져(핵심 모드) 있는가?
                bool isFilterActive = communityToggle != null && !communityToggle.IsCommunityVisible;

                // 똥글인데 필터가 켜져 있다면 화면에 그리지 않고 건너뜁니다.
                if (isNoise && isFilterActive) continue;

                Show(item.data, item.occurredTime);
            }
        }
    }

    private void UpdateTabVisuals(int selectedIndex) {
        for (int i = 0; i < tabTexts.Count; i++) {
            if (tabTexts[i] == null) continue;

            // 선택된 탭의 글자만 하늘색으로 변경
            tabTexts[i].color = (i == selectedIndex) ? activeTextColor : inactiveTextColor;
        }
    }
    public void OnToggleRefresh() {
        // 현재 선택된 카테고리 상태 그대로 리스트만 다시 그립니다.
        RefreshXbird(currentCategory);
    }

    public void OnClickTab(int categoryIndex) {
        NewsCategory selected = (NewsCategory)categoryIndex;
        RefreshXbird(selected);
    }

    public void Show(UIEventData data, DateTime gameTime) {
        if (newsItemPrefab == null || contentParent == null) return;

        if (currentCategory != NewsCategory.All && data.category != currentCategory) {
            return;
        }

        if (emptyNoticeObject != null) emptyNoticeObject.SetActive(false);

        var go = Instantiate(newsItemPrefab, contentParent);
        go.transform.SetAsFirstSibling();
        ApplySize(go, data);

        var loader = go.GetComponent<NewsLoader>();
        if (loader != null) loader.Load(data, gameTime);
    }

    private void ApplySize(GameObject go, UIEventData data) {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        if (data.width > 0f) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, data.width);
        if (data.height > 0f) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, data.height);
    }
}