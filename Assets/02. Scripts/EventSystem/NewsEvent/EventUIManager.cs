using System;
using UnityEngine;

public class EventUIManager : MonoBehaviour {

    public static EventUIManager Instance;

    [Header("Panels")]
    public NewsPanel newsPanel;
    public ResearchDetailController researchDetail;
    public ResearchDetailPopup detailPopup; // 씬에 미리 배치해둔 NewsPrefab 연결



    private void Awake() {
        Instance = this;
    }

    public void Show(UIEventData data) {
        DateTime gameTime = CoinManager.Instance.CurrentDateTime;

        switch (data.type) {
            case UIEventType.News: {
                    // [추가] 1. 뉴스가 발생했으니 히스토리에 무조건 기록합니다!
                    // (EventUIManager에 public NewsRepository newsRepo; 연결 필요)
                    var repo = FindAnyObjectByType<NewsRepository>();
                    if (repo != null) repo.AddToHistory(data, gameTime);
                    if (data.postYn) {
                        newsPanel.Show(data, gameTime);
                    } else {
                        Debug.Log($"[EventUIManager] '{data.key}'는 postYn=false이므로 피드에 띄우지 않습니다.");
                    }

                    // 3. 리서치 데이터(Article) 갱신 (이것도 피드 표시 여부와 상관없이 무조건 실행!)
                    if (data.article != null && !string.IsNullOrEmpty(data.article.title)) {
                        if (researchDetail != null) {
                            var newsMgr = researchDetail.newsGroup.GetComponent<ResearchNewsManager>();
                            if (newsMgr != null) newsMgr.RefreshResearchList();
                        }
                    }
                    break;
                }

            case UIEventType.RumorSimple: {
                    UIManager.Instance.ShowConfirm(data.title, data.message);
                    break;
                }

            case UIEventType.RumorDM: {
                    UIManager.Instance.ShowIncomingCall(data.title, data.message);
                    break;
                }
        }
    }
    public void OpenNewsDetail(UIEventData data) {
        if (detailPopup == null) return;

        // 현재 게임 시간 가져오기
        DateTime gameTime = CoinManager.Instance.CurrentDateTime;

        detailPopup.gameObject.SetActive(true);
        detailPopup.Setup(data, gameTime);
    }
}
