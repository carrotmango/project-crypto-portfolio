using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealEstatePanelController : MonoBehaviour {
    public Transform cardRoot;
    public RealEstateCard cardPrefab;

    private List<RealEstateCard> activeCards = new List<RealEstateCard>();
    private List<RealEstateData> dataList;

    private int currentPage = 0;
    private const int ITEMS_PER_PAGE = 3;
    public GameObject leftArrow;
    public GameObject rightArrow;

    public CanvasGroup pageCanvasGroup;
    public float fadeDuration = 0.25f;

    public event Action OnRealEstateChanged;

    private bool isTransitioning = false;
    DateTime Now => CoinManager.Instance.CurrentDateTime;
    DateTime lastUIRefreshDate;

    public static RealEstatePanelController Instance { get; private set; }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start() {
        // [핵심 수정] 번역기가 켜질 시간을 벌기 위해 코루틴으로 지연 실행
        StartCoroutine(InitDataRoutine());
    }

    IEnumerator InitDataRoutine() {
        yield return null; // 1프레임 대기 (언어 매니저 로딩 확보)

        LoadTestData();
        CreateCards();
        RefreshPage();
    }

    void Update() {
        if (dataList == null) return; // 데이터 로딩 전 에러 방지

        CheckEstatePrice();
        CheckUIRefreshByDay();
    }

    void LoadTestData() {
        dataList = new List<RealEstateData>()
        {
        new RealEstateData {
            id = "room_001",
            name = LocalizationManager.GetText("ESTATE_NAME_001"),
            price = 5000000,
            monthlyYield = 0.033f,
            owned = false,
            imageKey = "estate_500"
        },
        new RealEstateData {
            id = "room_002",
            name = LocalizationManager.GetText("ESTATE_NAME_002"),
            price = 10000000,
            monthlyYield = 0.037f,
            owned = false,
            imageKey = "estate_1000"
        },
        new RealEstateData {
            id = "room_003_oido",
            name = LocalizationManager.GetText("ESTATE_NAME_003"),
            price = 70000000,
            monthlyYield = 0.031f,
            owned = false,
            imageKey = "estate_7000"
        },
        new RealEstateData {
            id = "room_004_guro",
            name = LocalizationManager.GetText("ESTATE_NAME_004"),
            price = 50000000,
            monthlyYield = 0.042f,
            owned = false,
            imageKey = "estate_5000"
        },
        new RealEstateData {
            id = "room_005_seongsan",
            name = LocalizationManager.GetText("ESTATE_NAME_005"),
            price = 100000000,
            monthlyYield = 0.045f,
            owned = false,
            imageKey = "estate_10000"
        },
         new RealEstateData {
            id = "room_006_lotte",
            name = LocalizationManager.GetText("ESTATE_NAME_006"),
            price = 3000000000,
            monthlyYield = 0.073f,
            owned = false,
            imageKey = "estate_3B"
        }
    };

        LoadImages();
    }

    // ... (아래 LoadImages 부터 끝까지는 이전과 완전히 동일합니다. 그대로 쓰시면 됩니다.) ...

    void LoadImages() {
        foreach (var data in dataList) {
            if (!string.IsNullOrEmpty(data.imageKey)) {
                data.image = Resources.Load<Sprite>($"Image/estates/{data.imageKey}");

                if (data.image == null) {
                    Debug.LogWarning($"부동산 이미지 로드 실패: {data.imageKey}");
                }
            }
        }
    }

    void CreateCards() {
        for (int i = 0; i < ITEMS_PER_PAGE; i++) {
            RealEstateCard card = Instantiate(cardPrefab, cardRoot);
            card.onClickBuy = OnCardBuyClicked;
            activeCards.Add(card);
        }
    }

    void OnCardBuyClicked(RealEstateData data) {
        if (data.owned)
            SellEstate(data);
        else
            BuyEstate(data);
    }

    public void RefreshPage() {
        int startIndex = currentPage * ITEMS_PER_PAGE;

        for (int i = 0; i < activeCards.Count; i++) {
            int dataIndex = startIndex + i;

            if (dataIndex < dataList.Count) {
                activeCards[i].gameObject.SetActive(true);
                activeCards[i].SetData(dataList[dataIndex]);
            } else {
                activeCards[i].gameObject.SetActive(false);
            }
        }
        UpdateArrowState();
    }

    public void OnClickNext() {
        if (isTransitioning)
            return;

        int maxPage = (dataList.Count - 1) / ITEMS_PER_PAGE;
        if (currentPage < maxPage) {
            StartCoroutine(ChangePage(currentPage + 1));
        }
    }

    public void OnClickPrev() {
        if (isTransitioning)
            return;

        if (currentPage > 0) {
            StartCoroutine(ChangePage(currentPage - 1));
        }
    }

    void UpdateArrowState() {
        int maxPage = (dataList.Count - 1) / ITEMS_PER_PAGE;

        if (leftArrow != null)
            leftArrow.SetActive(currentPage > 0);

        if (rightArrow != null)
            rightArrow.SetActive(currentPage < maxPage);
    }

    IEnumerator ChangePage(int nextPage) {
        isTransitioning = true;

        yield return Fade(1f, 0f);

        currentPage = nextPage;
        RefreshPage();

        yield return Fade(0f, 1f);

        isTransitioning = false;
    }

    IEnumerator Fade(float from, float to) {
        float t = 0f;
        pageCanvasGroup.alpha = from;

        while (t < fadeDuration) {
            t += Time.unscaledDeltaTime;
            pageCanvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }

        pageCanvasGroup.alpha = to;
    }

    public void BuyEstate(RealEstateData data) {
        if (data.owned)
            return;

        if (PlayerManager.Instance.satoshiBankCash < data.price) {
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowConfirm(LocalizationManager.GetText("MSG_INSUFFICIENT_FUNDS"));
                return;
            }
        }

        PlayerManager.Instance.satoshiBankCash -= data.price;

        data.owned = true;
        data.ownedText = LocalizationManager.GetText("LBL_ESTATE_OWNED");
        data.buyDate = Now;
        data.purchasePrice = data.price;
        data.nextIncomeDate = Now.AddDays(7);

        // [핵심 수정] 기록은 무조건 한글 원본 데이터 고정! (출금)
        TransactionManager.Instance.AddRecord("부동산", data.price, "출금", "사토시 현금");

        RefreshPage();
        OnRealEstateChanged?.Invoke();
    }

    public void SellEstate(RealEstateData data) {
        if (!data.owned)
            return;

        PlayerManager.Instance.satoshiBankCash += data.price;

        data.owned = false;
        data.ownedText = LocalizationManager.GetText("LBL_ESTATE_NOT_OWNED");

        // [핵심 수정] 기록은 무조건 한글 원본 데이터 고정! (입금)
        TransactionManager.Instance.AddRecord("부동산", data.price, "입금", "사토시 현금");

        RefreshPage();
        OnRealEstateChanged?.Invoke();
    }

    void CheckEstatePrice() {
        foreach (var data in dataList) {
            if (data.lastPriceUpdateDate == default)
                data.lastPriceUpdateDate = Now;

            int days = (Now - data.lastPriceUpdateDate).Days;

            if (days >= UnityEngine.Random.Range(3, 6)) {

                float change = UnityEngine.Random.Range(-0.004f, 0.004f);
                double newPrice = data.price * (1.0 + change);
                data.price = (long)System.Math.Round(newPrice);

                long minPrice = 1000000;
                if (data.price < minPrice) {
                    data.price = minPrice;
                }

                data.lastPriceUpdateDate = Now;
                OnRealEstateChanged?.Invoke();
            }
        }
    }

    void CheckUIRefreshByDay() {
        if (lastUIRefreshDate.Date != Now.Date) {
            lastUIRefreshDate = Now.Date;

            ProcessDailyEstateIncome(Now);

            RefreshPage();
        }
    }

    public void ProcessDailyEstateIncome(DateTime now) {
        long totalIncome = 0;

        foreach (var estate in dataList) {
            if (!estate.owned) continue;

            if (now.Date >= estate.nextIncomeDate.Date) {
                long income = (long)(estate.price * estate.monthlyYield);
                PlayerManager.Instance.satoshiBankCash += income;
                DailyIncomeManager.Instance.AddEstateIncome(estate.name, income);
                estate.nextIncomeDate = estate.nextIncomeDate.AddDays(7);

                totalIncome += income;
            }
        }

        if (totalIncome > 0) {
            if (GlobalNotificationManager.Instance != null) {

                string unit = LocalizationManager.GetText("UNIT_CURRENCY");
                string message = string.Format(LocalizationManager.GetText("MSG_ESTATE_INCOME"), totalIncome.ToString("N0"), unit);
                string fullDetail = string.Format(LocalizationManager.GetText("SMS_ESTATE_INCOME_DETAIL"), totalIncome.ToString("N0"), unit);

                // [핵심 수정] 기록은 무조건 한글 원본 데이터 고정! (입금)
                TransactionManager.Instance.AddRecord("부동산", totalIncome, "입금", "사토시 현금");

                string notiTitle = LocalizationManager.GetText("NOTI_ESTATE_INCOME_TITLE");

                GlobalNotificationManager.Instance.ShowNotification(
                    "RealEstate",
                    notiTitle,
                    message,
                    () => {
                        if (UIManager.Instance != null) {
                            string senderMsg = string.Format(LocalizationManager.GetText("SMS_ESTATE_SENDER"), fullDetail);
                            UIManager.Instance.ShowSMSResult(senderMsg);
                        }
                    }
                );
            }
            Debug.Log($"[부동산] 수익 총 정산 +{totalIncome}");
        }
    }

    public List<RealEstateData> GetAllEstates() {
        return dataList;
    }
}