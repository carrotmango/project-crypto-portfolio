using UnityEngine;
using System.Collections.Generic;

public class ResearchNewsManager : MonoBehaviour {
    [Header("UI References")]
    public Transform contentParent;      // 뉴스 리스트가 생성될 부모 (Vertical Layout)
    public GameObject newsPreviewPrefab; // 뉴스 미리보기 아이템 프리팹
    public GameObject noContentObject;

    [Header("Dependencies")]
    public NewsRepository newsRepo;      // 데이터 저장소 연결
    public ResearchDetailController detailController;

    // 패널이 켜질 때마다 리스트 갱신
    private void OnEnable() {
        RefreshResearchList();
    }

    public void RefreshResearchList() {

        // 1. 기초 널 체크
        if (newsRepo == null || contentParent == null || detailController == null) return;

        // 2. 부모로부터 현재 보고 있는 코인 심볼 가져오기
        string currentSymbol = detailController.CurrentCoinSymbol.Trim().ToUpper();
        if (string.IsNullOrEmpty(currentSymbol)) return;

        // 3. 기존 리스트 청소
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        int activeNewsCount = 0;

        // 4. 필터링 및 생성
        var researchNews = newsRepo.GetResearchArticles();
        foreach (var item in researchNews) {
            if (item.data.article == null || item.data.article.targetSymbols == null) continue;

            bool isTarget = false;
            foreach (var s in item.data.article.targetSymbols) {
                string target = s.Trim().ToUpper(); // JSON 데이터도 대문자로 안전하게

                if (target == "ALL" || target == currentSymbol) {
                    isTarget = true;
                    break;
                }
            }

            if (isTarget) {
                GameObject go = Instantiate(newsPreviewPrefab, contentParent);
                go.GetComponent<ResearchNewsLoader>().Setup(item.data, item.occurredTime);
                activeNewsCount++;
            }

            if (noContentObject != null) {
                // 뉴스가 0개면 true, 1개 이상이면 false
                noContentObject.SetActive(activeNewsCount == 0);
            }

        }
        Debug.Log($"[리서치] 현재 코인: {currentSymbol} / 히스토리에 있는 정제된 기사 수: {researchNews.Count}");
    }
}