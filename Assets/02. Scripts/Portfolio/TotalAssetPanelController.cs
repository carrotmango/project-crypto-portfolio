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
    //public GameObject satoshiPortfolioPanel;
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

        double crypto = CoinManager.Instance.GetBullbitAsset();
        double bank = CoinManager.Instance.GetSatoshiBankAsset();
        double estate = GetEstateAssetValue();
        double total = (status != null) ? status.GetTotalBalance() : (crypto + bank + estate);

        // [수정] 공통 화폐 단위 불러오기 (KRW 하드코딩 제거용)
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");

        foreach (var asset in userAssets) {
            PlatformInfo info = platformInfos.Find(p => p.id == asset.platformId);
            if (info == null) continue;

            double displayedValue = 0;
            string currentId = info.id;

            if (currentId == "bullbit") displayedValue = crypto;
            else if (currentId == "satoshi_bank") displayedValue = bank;
            else if (currentId == "my_total_asset") displayedValue = total;
            else if (currentId == "real_estate") displayedValue = estate;
            else displayedValue = asset.cashAsset;

            GameObject row = Instantiate(platformRowPrefab, contentParent);
            row.name = currentId;

            Image logoImage = row.transform.Find("Platform_logo").GetComponent<Image>();
            logoImage.sprite = info.logo;

            ApplyLogoSize(currentId, logoImage);

            // ★ [핵심] 인스펙터에 적어둔 Key(info.displayName)를 번역기에 넣어서 돌립니다!
            row.transform.Find("Platform_name").GetComponent<TextMeshProUGUI>().text = LocalizationManager.GetText(info.displayName);
            row.transform.Find("Platform_Type").GetComponent<TextMeshProUGUI>().text = LocalizationManager.GetText(info.type);
            row.transform.Find("Platform_Asset").GetComponent<TextMeshProUGUI>().text = $"{displayedValue:N0} {unit}";

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
        string unit = LocalizationManager.GetText("UNIT_CURRENCY"); // [추가]

        foreach (Transform row in contentParent) {
            string id = row.name;
            double rowValue = 0;

            if (id == "bullbit") rowValue = CoinManager.Instance.GetBullbitAsset();
            else if (id == "satoshi_bank") rowValue = CoinManager.Instance.GetSatoshiBankAsset();
            else if (id == "real_estate") rowValue = GetEstateAssetValue();
            else if (id == "my_total_asset") rowValue = realTotal;

            var assetText = row.transform.Find("Platform_Asset")?.GetComponent<TextMeshProUGUI>();
            // [수정] KRW 대신 unit 적용
            if (assetText != null) assetText.text = $"{rowValue:N0} {unit}";
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
        //satoshiPortfolioPanel.SetActive(false);

        switch (platformId) {
            case "bullbit":
                Transform totalAsset = GameObject.Find("TotalAssetPanel").transform;
                bullbitPortfolioPanel.transform.SetParent(totalAsset.parent, false);
                bullbitPortfolioPanel.transform.SetSiblingIndex(totalAsset.GetSiblingIndex() + 1);
                bullbitPortfolioPanel.transform.localPosition = Vector3.zero;
                bullbitPortfolioPanel.SetActive(true);
                break;
            case "satoshi_bank":
                //satoshiPortfolioPanel.transform.SetAsLastSibling();
                //satoshiPortfolioPanel.transform.localPosition = Vector3.zero;
                //satoshiPortfolioPanel.SetActive(true);
                break;
            case "my_total_asset":
                statusPanel.SetActive(true);
                break;
        }
    }

    public void ClosePortfolioPanels() {
        bullbitPortfolioPanel.SetActive(false);
        //satoshiPortfolioPanel.SetActive(false);
        totalAssetPanel.SetActive(true);
    }

    public void OpenBullbitPortfolio() {
        bullbitPortfolioPanel.SetActive(true);
    }

    public void CloseAllAssetPanels() {
        if (bullbitPortfolioPanel != null) bullbitPortfolioPanel.SetActive(false);
        if (totalAssetPanel != null) totalAssetPanel.SetActive(false);
    }
}