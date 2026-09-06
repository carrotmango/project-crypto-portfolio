using UnityEngine;
using UnityEngine.Pool; // 유니티 내장 풀링 시스템 사용
using System.Collections.Generic;
using System.Linq;

public class TotalNewsManager : MonoBehaviour {
    [Header("UI References")]
    public Transform contentParent;
    public GameObject newsPreviewPrefab;
    public GameObject noContentObject;

    [Header("Dependencies")]
    public NewsRepository newsRepo;

    // ★ 오브젝트 풀 선언
    private IObjectPool<GameObject> newsPool;
    private List<GameObject> activeNewsItems = new List<GameObject>();

    private void Awake() {
        // 풀 초기화 설정
        newsPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(newsPreviewPrefab, contentParent), // 부족할 때 생성
            actionOnGet: (target) => target.SetActive(true),               // 꺼낼 때 활성화
            actionOnRelease: (target) => target.SetActive(false),           // 넣을 때 비활성화
            actionOnDestroy: (target) => Destroy(target),                  // 풀 용량 초과 시 파괴
            defaultCapacity: 20, // 기본 20개 준비
            maxSize: 100         // 최대 100개까지 관리
        );
    }

    private void OnEnable() {
        RefreshAllNews();
        if (CoinManager.Instance != null) {
            CoinManager.Instance.OnMarketUpdated += RefreshAllNews;
        }
    }

    private void OnDisable() {
        if (CoinManager.Instance != null) {
            CoinManager.Instance.OnMarketUpdated -= RefreshAllNews;
        }
    }

    public void RefreshAllNews() {
        if (newsRepo == null || contentParent == null) return;

        // 1. 활성화된 아이템들을 풀로 반납 (Destroy 대신 Release)
        foreach (var item in activeNewsItems) {
            newsPool.Release(item);
        }
        activeNewsItems.Clear();

        // 2. 뉴스 정렬
        var sortedNews = newsRepo.GetResearchArticles()
            .OrderByDescending(n => n.occurredTime)
            .ToList();

        if (noContentObject != null) noContentObject.SetActive(sortedNews.Count == 0);
        if (sortedNews.Count == 0) return;

        // 3. 풀에서 빌려와서 배치
        foreach (var item in sortedNews) {
            if (item.data.article == null) continue;

            // Instantiate 대신 Pool에서 Get!
            GameObject go = newsPool.Get();
            go.transform.SetParent(contentParent); // 부모 설정 다시 확인

            var loader = go.GetComponent<ResearchNewsLoader>();
            if (loader != null) {
                loader.Setup(item.data, item.occurredTime);
            }

            // 최신 글이 위로 가도록 순서 조정
            go.transform.SetAsLastSibling();
            activeNewsItems.Add(go);
        }
    }
}