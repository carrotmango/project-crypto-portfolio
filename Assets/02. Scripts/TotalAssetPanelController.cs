using System.Collections;
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
    public GameObject statusPanel;

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
        if (contentParent == null) return;
        // 데이터 매니저들이 초기화될 시간을 벌기 위해 코루틴 실행
        StartCoroutine(InitAfterFrame());
    }

    IEnumerator InitAfterFrame() {
        yield return new WaitForEndOfFrame();
        RenderPlatformRows();
    }

    public void RenderPlatformRows() {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        StatusPanelController status = FindFirstObjectByType<StatusPanelController>();

        // 루프 돌기 전 필요한 실시간 자산 미리 계산
        double crypto = CoinManager.Instance.GetBullbitAsset();
        double bank = CoinManager.Instance.GetSatoshiBankAsset();
        double estate = GetEstateAssetValue();
        double total = (status != null) ? status.GetTotalBalance() : (crypto + bank + estate);

        foreach (var asset in userAssets) {
            PlatformInfo info = platformInfos.Find(p => p.id == asset.platformId);
            if (info == null) continue;

            double displayedValue = 0;
            string currentId = info.id;

            // 1. 수치 배정 로직
            if (currentId == "bullbit") {
                displayedValue = crypto;
            } else if (currentId == "satoshi_bank") {
                displayedValue = bank;
            } else if (currentId == "my_total_asset") {
                displayedValue = total;
            } else if (currentId == "real_estate") {
                displayedValue = estate;
            } else {
                displayedValue = asset.cashAsset;
            }

            // 2. 프리팹 생성 및 기본 설정
            GameObject row = Instantiate(platformRowPrefab, contentParent);
            row.name = currentId;

            // 3. 로고 설정 및 사이즈 조절 (기존 수치 유지)
            Image logoImage = row.transform.Find("Platform_logo").GetComponent<Image>();
            logoImage.sprite = info.logo;

            ApplyLogoSize(currentId, logoImage);

            // 4. 텍스트 정보 입력
            row.transform.Find("Platform_name").GetComponent<TextMeshProUGUI>().text = info.displayName;
            row.transform.Find("Platform_Asset").GetComponent<TextMeshProUGUI>().text = $"{displayedValue:N0} KRW";
            row.transform.Find("Platform_Type").GetComponent<TextMeshProUGUI>().text = info.type;

            // 5. 버튼 이벤트 연결
            Button rowButton = row.GetComponent<Button>() ?? row.AddComponent<Button>();
            rowButton.onClick.AddListener(() => OpenPortfolioPanel(currentId));

            if (currentId == "bullbit" && TutorialManager.Instance != null) {
                TutorialManager.Instance.bullbitPortfolioRowButton = rowButton;
            }
        }
    }

    // 로고 사이즈 조절 로직 분리 (가독성)
    private void ApplyLogoSize(string id, Image logoImage) {
        if (id == "bullbit") {
            logoImage.rectTransform.sizeDelta = new Vector2(25f, 55f);
            logoImage.rectTransform.anchoredPosition = new Vector2(-220f, -42.5f);
        } else if (id == "satoshi_bank") {
            logoImage.rectTransform.sizeDelta = new Vector2(20f, 50f);
            logoImage.rectTransform.anchoredPosition = new Vector2(-220f, -35f);
        } else if (id == "real_estate") {
            logoImage.rectTransform.sizeDelta = new Vector2(20f, 80f);
            logoImage.rectTransform.anchoredPosition = new Vector2(-220f, -35f);
        } else if (id == "my_total_asset") {
            logoImage.rectTransform.sizeDelta = new Vector2(10f, 30f);
            logoImage.rectTransform.anchoredPosition = new Vector2(-220f, -42.5f);
        }
    }

    public void UpdatePlatformAssetTexts() {
        StatusPanelController status = FindFirstObjectByType<StatusPanelController>();
        double realTotal = (status != null) ? status.GetTotalBalance() : 0;

        foreach (Transform row in contentParent) {
            string id = row.name;
            double rowValue = 0;

            if (id == "bullbit") rowValue = CoinManager.Instance.GetBullbitAsset();
            else if (id == "satoshi_bank") rowValue = CoinManager.Instance.GetSatoshiBankAsset();
            else if (id == "real_estate") rowValue = GetEstateAssetValue();
            else if (id == "my_total_asset") rowValue = realTotal;

            var assetText = row.transform.Find("Platform_Asset")?.GetComponent<TextMeshProUGUI>();
            if (assetText != null) assetText.text = $"{rowValue:N0} KRW";
        }
    }

    private double GetEstateAssetValue() {
        if (RealEstatePanelController.Instance == null) return 0;
        var estates = RealEstatePanelController.Instance.GetAllEstates();
        if (estates == null) return 0;

        double total = 0;
        foreach (var estate in estates) {
            if (estate.owned) total += estate.price;
        }
        return total;
    }

    // --- 패널 오픈 로직 (기존 유지) ---
    void OpenPortfolioPanel(string platformId) {
        bullbitPortfolioPanel.SetActive(false);
        satoshiPortfolioPanel.SetActive(false);

        switch (platformId) {
            case "bullbit":
                Transform totalAsset = GameObject.Find("TotalAssetPanel").transform;
                bullbitPortfolioPanel.transform.SetParent(totalAsset.parent, false);
                bullbitPortfolioPanel.transform.SetSiblingIndex(totalAsset.GetSiblingIndex() + 1);
                bullbitPortfolioPanel.transform.localPosition = Vector3.zero;
                bullbitPortfolioPanel.SetActive(true);
                break;
            case "satoshi_bank":
                satoshiPortfolioPanel.transform.SetAsLastSibling();
                satoshiPortfolioPanel.transform.localPosition = Vector3.zero;
                satoshiPortfolioPanel.SetActive(true);
                break;
            case "my_total_asset":
                statusPanel.SetActive(true);
                break;
        }
    }

    public void ClosePortfolioPanels() {
        bullbitPortfolioPanel.SetActive(false);
        satoshiPortfolioPanel.SetActive(false);
        totalAssetPanel.SetActive(true);
    }
}