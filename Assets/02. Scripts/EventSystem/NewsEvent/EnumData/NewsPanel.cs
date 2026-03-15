using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewsPanel : MonoBehaviour {

    public Transform contentParent;
    public GameObject newsItemPrefab;
    public NewsRepository newsRepo;
    private NewsCategory currentCategory = NewsCategory.All;
    public GameObject emptyNoticeObject;

    public RetroToggleController communityToggle;

    [Header("탭 텍스트 설정")]
    public List<TextMeshProUGUI> tabTexts;
    public Color activeTextColor = new Color32(0, 191, 255, 255);
    public Color inactiveTextColor = Color.white;

    [Header("알림 뱃지 UI (메인)")]
    public GameObject newsBadgeObject;
    public TextMeshProUGUI newsBadgeText;

    [Header("스크롤 감지용 UI")]
    public ScrollRect scrollRect;
    public RectTransform viewportRect;

    private class ActiveNews {
        public RectTransform rt;
        public NewsRepository.OccurredNewsData data;
        public NewsLoader loader;
    }
    private List<ActiveNews> activeNewsList = new List<ActiveNews>();

    private void Start() {
        if (scrollRect != null) {
            // 💡 뷰포트가 비어있으면 안전하게 자동 할당
            if (viewportRect == null) viewportRect = scrollRect.viewport;
            if (viewportRect == null) viewportRect = scrollRect.GetComponent<RectTransform>();

            scrollRect.onValueChanged.AddListener(OnScrollChanged);
            scrollRect.scrollSensitivity = 20f;
        }
        RefreshXbird(NewsCategory.All);
    }

    // 매 프레임 감시 (패널 켜져있을 때 무조건 작동)
    private void Update() {
        if (gameObject.activeInHierarchy) {
            CheckVisibleItemsAndMarkRead();
        }
    }

    public void RefreshXbird(NewsCategory category) {
        currentCategory = category;
        UpdateTabVisuals((int)category);

        foreach (Transform child in contentParent) Destroy(child.gameObject);
        activeNewsList.Clear();

        var history = newsRepo.GetNewsByCategory(category);
        int shownCount = 0;

        if (history != null && history.Count > 0) {
            foreach (var item in history) {
                UIEventData eventData = item.data;
                bool isCommunity = ((int)eventData.category == 1);

                if (category == NewsCategory.All && isCommunity) continue;
                bool isFilterActive = communityToggle != null && !communityToggle.IsCommunityVisible;
                if (isCommunity && isFilterActive) continue;

                var go = Instantiate(newsItemPrefab, contentParent);
                go.transform.SetAsFirstSibling();
                ApplySize(go, eventData);

                var loader = go.GetComponent<NewsLoader>();
                if (loader != null) {
                    loader.Load(eventData, item.occurredTime, item.isRead);

                    // 🔥 [핵심 1] 탭 다녀오거나 켰을 때 DB가 true(읽음)면 무조건 파란불 끄고 시작!
                    loader.SetBadge(!item.isRead);
                }

                activeNewsList.Add(new ActiveNews {
                    rt = go.GetComponent<RectTransform>(),
                    data = item,
                    loader = loader
                });

                shownCount++;
            }
        }

        if (emptyNoticeObject != null) emptyNoticeObject.SetActive(shownCount == 0);

        if (gameObject.activeInHierarchy) {
            Canvas.ForceUpdateCanvases();
            if (contentParent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
        }

        UpdateUnreadBadge();
    }

    private void OnScrollChanged(Vector2 pos) {
        CheckVisibleItemsAndMarkRead();
    }

    private void CheckVisibleItemsAndMarkRead() {
        if (viewportRect == null) return;

        bool isChanged = false;

        foreach (var active in activeNewsList) {
            if (active.data.isRead) continue; // 이미 처리된 건 무시

            // 내 화면에 닿았는지(최초로 봤는지) 판별
            if (IsVisibleInViewport(active.rt, viewportRect)) {
                // 🔥 [핵심 2] 닿는 순간 무조건 읽음(True) 처리!
                active.data.isRead = true;
                isChanged = true;

                // 💡 행님 요청대로 '보고 있는 동안'엔 불 안 끕니다. 
                // 다른 탭(현물, 오피스 등) 갔다가 돌아올 때 위 RefreshXbird()가 돌면서 영원히 꺼집니다.
            }
        }

        if (isChanged) UpdateUnreadBadge();
    }

    // 🚀 [최종 병기] 쓸데없는 수학 계산 싹 빼고 유니티 100% 공식 사각형 충돌 함수로 교체!
    private bool IsVisibleInViewport(RectTransform item, RectTransform viewport) {
        if (item == null || viewport == null) return false;

        Vector3[] vCorners = new Vector3[4];
        viewport.GetWorldCorners(vCorners);
        Rect viewRect = new Rect(vCorners[0].x, vCorners[0].y, vCorners[2].x - vCorners[0].x, vCorners[2].y - vCorners[0].y);

        Vector3[] iCorners = new Vector3[4];
        item.GetWorldCorners(iCorners);
        Rect itemRect = new Rect(iCorners[0].x, iCorners[0].y, iCorners[2].x - iCorners[0].x, iCorners[2].y - iCorners[0].y);

        // UI 사각형끼리 단 1픽셀이라도 겹치면 "본 것"으로 완벽 인식!
        return viewRect.Overlaps(itemRect);
    }

    public void UpdateUnreadBadge() {
        if (newsBadgeObject == null || newsBadgeText == null || newsRepo == null) return;

        int unreadCount = 0;
        var allHistory = newsRepo.GetNewsByCategory(NewsCategory.All);

        if (allHistory != null) {
            foreach (var item in allHistory) {
                if (((int)item.data.category != 1) && !item.isRead) {
                    unreadCount++;
                }
            }
        }

        if (unreadCount > 0) {
            newsBadgeObject.SetActive(true);
            newsBadgeText.text = unreadCount > 99 ? "99+" : unreadCount.ToString();
        } else {
            newsBadgeObject.SetActive(false);
        }
    }

    public void Show(UIEventData data, DateTime gameTime) {
        if (!data.postYn) return;
        if (newsItemPrefab == null || contentParent == null) return;

        bool isCommunity = ((int)data.category == 1);

        if (currentCategory == NewsCategory.All && isCommunity) return;
        if (currentCategory != NewsCategory.All && (int)data.category != (int)currentCategory) return;
        if (communityToggle != null && !communityToggle.IsCommunityVisible && isCommunity) return;

        if (emptyNoticeObject != null) emptyNoticeObject.SetActive(false);

        var go = Instantiate(newsItemPrefab, contentParent);
        go.transform.SetAsFirstSibling();
        ApplySize(go, data);

        var allHistory = newsRepo.GetNewsByCategory(NewsCategory.All);
        var sourceItem = allHistory.Find(x => x.data.key == data.key);

        var loader = go.GetComponent<NewsLoader>();
        if (loader != null) {
            bool isReadStatus = sourceItem != null ? sourceItem.isRead : true;
            loader.Load(data, gameTime, isReadStatus);
            if (sourceItem != null) loader.SetBadge(!sourceItem.isRead);
        }

        if (sourceItem != null) {
            activeNewsList.Add(new ActiveNews { rt = go.GetComponent<RectTransform>(), data = sourceItem, loader = loader });
        }

        if (gameObject.activeInHierarchy) {
            Canvas.ForceUpdateCanvases();
            if (contentParent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
        }
    }

    private void ApplySize(GameObject go, UIEventData data) {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        float finalHeight = data.height;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, data.width > 0 ? data.width : 550f);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight > 0 ? finalHeight : 150f);
    }

    private void UpdateTabVisuals(int selectedIndex) {
        for (int i = 0; i < tabTexts.Count; i++) {
            if (tabTexts[i] == null) continue;
            tabTexts[i].color = (i == selectedIndex) ? activeTextColor : inactiveTextColor;
        }
    }

    public void OnToggleRefresh() => RefreshXbird(currentCategory);
    public void OnClickTab(int categoryIndex) => RefreshXbird((NewsCategory)categoryIndex);

    public void CleanUpUI(int maxCount) {
        int currentNoiseUI_Count = 0;
        for (int i = 0; i < contentParent.childCount; i++) {
            Transform child = contentParent.GetChild(i);
            if (child.name.StartsWith("noise_")) {
                currentNoiseUI_Count++;
                if (currentNoiseUI_Count > maxCount) Destroy(child.gameObject);
            }
        }
    }
}