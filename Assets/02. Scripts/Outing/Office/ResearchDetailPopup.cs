using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;

public class ResearchDetailPopup : MonoBehaviour {
    [Header("Inside Panel")]
    public GameObject insidePanel;      // News Prefab_Inside (본체)

    [Header("Top Content")]
    public TextMeshProUGUI titleText;   // Title

    [Header("Scroll Content")]
    public TextMeshProUGUI authorAndTimeText; // Aurthor And Time
    public GameObject contentImageContainer;  // Content Image 오브젝트 (가변용)
    public Image contentImage;                // Content Image 컴포넌트
    public TextMeshProUGUI contextText;       // Context (뉴스 본문)

    [Header("Bottom Content")]
    public TextMeshProUGUI expertText;        // Expert (한줄평)
    public TextMeshProUGUI relativeTagsText;  // Relative Tags (해시태그)
    public Button closeButton;                // Close (플로팅 버튼)

    public void Setup(UIEventData data, DateTime occurredTime) {
        var art = data.article;
        if (art == null) return;

        // 1. 텍스트 데이터 셋팅
        titleText.text = art.title;
        contextText.text = art.summary;

        // [다국어 처리 수정] 작성자 및 시간 포맷팅
        string timeStr = occurredTime.ToString("MM/dd/yyyy HH:mm");
        authorAndTimeText.text = string.Format(LocalizationManager.GetText("LBL_AUTHOR_AND_TIME"), art.provider, timeStr);

        // [다국어 처리 수정] 전문가 의견 포맷팅
        expertText.text = string.Format(LocalizationManager.GetText("LBL_EXPERT_OPINION"), art.expertOpinion);

        // 2. 이미지 로딩 (NewsLoader의 TrySetSprite 로직 적용)
        string imageName = art.newsImage;
        TrySetArticleImage(contentImage, "image/content", imageName, contentImageContainer);

        // 3. 관련 태그 생성
        if (art.targetSymbols != null && art.targetSymbols.Length > 0) {
            List<string> tags = new List<string>();
            foreach (var s in art.targetSymbols) {
                if (s != "ALL") tags.Add($"#{s}");
            }
            relativeTagsText.text = string.Join(" ", tags);
            relativeTagsText.gameObject.SetActive(tags.Count > 0);
        } else {
            relativeTagsText.gameObject.SetActive(false);
        }

        Button bgButton = GetComponent<Button>();
        if (bgButton != null) {
            bgButton.onClick.RemoveAllListeners();
            bgButton.onClick.AddListener(ClosePopup);
        }

        // 4. DOTween 애니메이션 실행
        OpenMenuWithTween();

        // 5. 닫기 버튼 연결
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(ClosePopup);
    }

    private void OpenMenuWithTween() {
        gameObject.SetActive(true);
        insidePanel.transform.localScale = Vector3.one * 0.5f;
        insidePanel.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void ClosePopup() {
        insidePanel.transform.DOScale(0.5f, 0.15f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }

    private void TrySetArticleImage(Image target, string folder, string fileName, GameObject container) {
        if (string.IsNullOrEmpty(fileName)) {
            if (container != null) container.SetActive(false);
            return;
        }

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string path = $"{folder}/{baseName}";
        var sprite = Resources.Load<Sprite>(path);

        if (sprite == null) {
            if (container != null) container.SetActive(false);
            return;
        }

        if (container != null) container.SetActive(true);
        if (target != null) {
            target.sprite = sprite;
            target.enabled = true;
        }
    }
}