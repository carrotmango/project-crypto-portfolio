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

    private bool isTransitioning = false;
    DateTime Now => CoinManager.Instance.CurrentDateTime;
    DateTime lastUIRefreshDate;
    //DateTime lastIncomeProcessDate = DateTime.MinValue;

    public static RealEstatePanelController Instance { get; private set; }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    void Start() {
        LoadTestData();
        CreateCards();
        RefreshPage();
    }

    void Update() {
        //CheckEstateIncome();
        CheckEstatePrice();
        CheckUIRefreshByDay();
    }


    void LoadTestData() {
        dataList = new List<RealEstateData>()
        {
        new RealEstateData {
            id = "room_001",
            name = "작은 원룸",
            price = 5000000,
            monthlyYield = 0.013f,
            owned = false,
            imageKey = "estate_500"
        },
        new RealEstateData {
            id = "room_002",
            name = "안산시 원룸",
            price = 10000000,
            monthlyYield = 0.021f,
            owned = false,
            imageKey = "estate_1000"
        },
                new RealEstateData {
            id = "room_002",
            name = "오이도 쓰리룸",
            price = 70000000,
            monthlyYield = 0.016f,
            owned = false,
            imageKey = "estate_7000"
        },
        new RealEstateData {
            id = "room_003",
            name = "구로동 원룸",
            price = 50000000,
            monthlyYield = 0.019f,
            owned = false,
            imageKey = "estate_5000"
        },
        new RealEstateData {
            id = "room_004",
            name = "성산동 원룸",
            price = 100000000,
            monthlyYield = 0.018f,
            owned = false,
            imageKey = "estate_10000"
        }
    };

        LoadImages();
    }

    void LoadImages() {
        foreach (var data in dataList) {
            if (!string.IsNullOrEmpty(data.imageKey)) {
                data.image = Resources.Load<Sprite>(
                    $"Image/estates/{data.imageKey}"
                );

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

        if (PlayerManager.Instance.satoshiBankCash < data.price)
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowConfirm("잔액이 부족합니다.");
                return; // 마통 불가
            }

        PlayerManager.Instance.satoshiBankCash -= data.price;

        data.owned = true;
        data.ownedText = "보유중";
        data.buyDate = Now;
        data.nextIncomeDate = Now.AddDays(2);

        RefreshPage();
    }
    public void SellEstate(RealEstateData data) {
        if (!data.owned)
            return;

        PlayerManager.Instance.satoshiBankCash += data.price;

        data.owned = false;
        data.ownedText = "미보유";

        RefreshPage();
    }

    //void CheckEstateIncome() {

    //    // 오늘 이미 정산했으면 중단
    //    if (lastIncomeProcessDate.Date == Now.Date)
    //        return;

    //    lastIncomeProcessDate = Now.Date;

    //    int totalIncome = 0;

    //    foreach (var data in dataList) {
    //        if (!data.owned)
    //            continue;

    //        if (Now.Date >= data.nextIncomeDate.Date) {
    //            int income = Mathf.RoundToInt(data.price * data.monthlyYield);
    //            totalIncome += income;

    //            // 테스트용 3일 주기
    //            data.nextIncomeDate = data.nextIncomeDate.AddDays(3);

    //            float drift = UnityEngine.Random.Range(-0.0015f, 0.0015f);
    //            data.monthlyYield = Mathf.Clamp(
    //                data.monthlyYield + drift,
    //                0.01f,
    //                0.03f
    //            );

    //            Debug.Log($"[부동산] {data.name} 월세 +{income}");
    //        }
    //    }

    //    if (totalIncome > 0) {
    //        PlayerManager.Instance.satoshiBankCash += totalIncome;
    //        Debug.Log($"[부동산] 오늘 월세 총 정산 +{totalIncome}");
    //        RefreshPage();
    //    }
    //}


    void CheckEstatePrice() {
        foreach (var data in dataList) {
            if (data.lastPriceUpdateDate == default)
                data.lastPriceUpdateDate = Now;

            int days = (Now - data.lastPriceUpdateDate).Days;

            if (days >= UnityEngine.Random.Range(3, 6)) {
                float change = UnityEngine.Random.Range(-0.004f, 0.004f);
                data.price = Mathf.RoundToInt(data.price * (1f + change));
                data.lastPriceUpdateDate = Now;
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
        int totalIncome = 0;

        foreach (var estate in dataList) {
            if (!estate.owned)
                continue;

            if (now.Date >= estate.nextIncomeDate.Date) {
                int income = Mathf.RoundToInt(
                    estate.price * estate.monthlyYield
                );

                PlayerManager.Instance.satoshiBankCash += income;

                DailyIncomeManager.Instance.AddEstateIncome(
                    estate.name,
                    income
                );

                estate.nextIncomeDate = estate.nextIncomeDate.AddDays(2);
            }
        }

        if (totalIncome > 0) {
            PlayerManager.Instance.satoshiBankCash += totalIncome;

            SMSNotificationManager.Instance.ReceiveSMS(
                "부동산",
                $"월세 {totalIncome:N0}원이 입금되었...",
                $"월세 {totalIncome:N0}원이\n계좌로 입금되었습니다."
            );

            Debug.Log($"[부동산] 월세 총 정산 +{totalIncome}");
        }
    }
    public List<RealEstateData> GetAllEstates() {
        return dataList;
    }
}
