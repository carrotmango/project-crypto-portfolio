using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TotalAssetPanelController : MonoBehaviour {
    [Header("Prefab & Parent")]
    public GameObject platformRowPrefab;
    public Transform contentParent;
    public GameObject totalAssetPanel;

    [Header("포트폴리오 패널 연결")]
    public GameObject bullbitPortfolioPanel;
    public GameObject satoshiPortfolioPanel;

    [System.Serializable]
    public class PlatformInfo {
        public string id;
        public string displayName;
        public Sprite logo;
        public string type;
    }
    
    [System.Serializable]
    public class PlatformAsset {
        public string platformId;
        public double cashAsset;
    }

    [Header("플랫폼 마스터 정보 (고정)")]
    public List<PlatformInfo> platformInfos = new();

    [Header("유저 보유 플랫폼 자산")]
    public List<PlatformAsset> userAssets = new();

    void Start() {
        if (contentParent == null) {
            Debug.LogWarning("contentParent없음");
            return;
        }
        RenderPlatformRows();
    }

    public void RenderPlatformRows() {
        foreach (Transform child in contentParent) {
            Destroy(child.gameObject);
        }

        foreach (var asset in userAssets) {
            PlatformInfo info = platformInfos.Find(p => p.id == asset.platformId);
            if (info == null) continue;

            double displayedCash = 0;

            if (asset.platformId == "bullbit") {
                displayedCash = CoinManager.Instance.GetBullbitAsset();
            } else if (asset.platformId == "satoshi_bank") {
                displayedCash = CoinManager.Instance.GetSatoshiBankAsset();  
            } else {
                displayedCash = asset.cashAsset;
            }


            GameObject row = Instantiate(platformRowPrefab, contentParent);
            row.name = asset.platformId;

            // 로고
            Image logoImage = row.transform.Find("Platform_logo").GetComponent<Image>();
            logoImage.sprite = info.logo;

            // 사토시뱅크는 사이즈 조정
            if (info.id == "bullbit") {
                logoImage.rectTransform.sizeDelta = new Vector2(25f, 55f);
                logoImage.rectTransform.anchoredPosition = new Vector2(-220f, -40f);
            }

            if (info.id == "satoshi_bank") {
                logoImage.rectTransform.sizeDelta = new Vector2(20f, 50f);
                logoImage.rectTransform.anchoredPosition = new Vector2(-220f, -35f);
            }

            if (info.id == "real_estate") {
                logoImage.rectTransform.sizeDelta = new Vector2(20f, 80f);
                logoImage.rectTransform.anchoredPosition = new Vector2(-220f, -35f);

            }

            row.transform.Find("Platform_name").GetComponent<TextMeshProUGUI>().text = info.displayName;
            row.transform.Find("Platform_Asset").GetComponent<TextMeshProUGUI>().text = $"{displayedCash:N0} KRW";
            row.transform.Find("Platform_Type").GetComponent<TextMeshProUGUI>().text = info.type;

            string platformId = asset.platformId;

            Button rowButton = row.GetComponent<Button>();
            if (rowButton == null) {
                rowButton = row.AddComponent<Button>();
            }

            rowButton.onClick.AddListener(() => {
                Debug.Log($"[클릭됨] 플랫폼 ID: {platformId}");
                OpenPortfolioPanel(platformId);
            });
            if (info.id == "bullbit" && TutorialManager.Instance != null) {
                TutorialManager.Instance.bullbitPortfolioRowButton = rowButton;
            }
        }
    }

    void OpenPortfolioPanel(string platformId) {
        // 모든 포트폴리오 패널 비활성화
        bullbitPortfolioPanel.SetActive(false);
        satoshiPortfolioPanel.SetActive(false);

        Debug.Log($"[OpenPortfolioPanel] 호출됨: {platformId}");

        switch (platformId) {
            case "bullbit":
                Debug.Log("[패널] 불비트 클릭 → 패널 열기");

                // 기준 오브젝트(총자산 패널)
                Transform totalAsset = GameObject.Find("TotalAssetPanel").transform;

                // 부모를 같은 Canvas로 옮김
                bullbitPortfolioPanel.transform.SetParent(totalAsset.parent, false);

                // TotalAssetPanel 바로 아래로 배치
                bullbitPortfolioPanel.transform.SetSiblingIndex(totalAsset.GetSiblingIndex() + 1);

                // 위치 조정
                bullbitPortfolioPanel.transform.localPosition = Vector3.zero;

                // 패널 활성화
                bullbitPortfolioPanel.SetActive(true);
                break;

            case "satoshi_bank":
                Debug.Log("[패널] 사토시 클릭 → 패널 열기");
                satoshiPortfolioPanel.transform.SetAsLastSibling();
                satoshiPortfolioPanel.transform.localPosition = Vector3.zero;
                satoshiPortfolioPanel.SetActive(true);
                break;

            default:
                Debug.LogWarning($"[경고] 해당 플랫폼 포트폴리오 없음: {platformId}");
                break;
        }
    }

    public void UpdatePlatformAssetTexts() {
        foreach (Transform row in contentParent) {
            string id = row.name;

            if (id == "bullbit") {
                double total = CoinManager.Instance.GetBullbitAsset();
                row.transform.Find("Platform_Asset").GetComponent<TextMeshProUGUI>().text = $"{total:N0} KRW";
            } else if (id == "satoshi_bank") {
                double total = CoinManager.Instance.GetSatoshiBankAsset();
                row.transform.Find("Platform_Asset").GetComponent<TextMeshProUGUI>().text = $"{total:N0} KRW";
            }
        }
    }


    public void ClosePortfolioPanels() {
        bullbitPortfolioPanel.SetActive(false);
        satoshiPortfolioPanel.SetActive(false);
        totalAssetPanel.SetActive(true); // 총자산 패널 다시 켜기
    }

}
