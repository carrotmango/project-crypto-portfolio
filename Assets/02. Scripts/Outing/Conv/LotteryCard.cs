using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PrizeInfo {
    public string rankName;
    public long prizeAmount;
    [Range(0, 100)]
    public float probability;
}

public class LotteryCard : MonoBehaviour {
    [Header("--- 플레이어 데이터 연결 (필수!) ---")]
    public PlayerManager playerManager;
    public long ticketPrice = 5000;

    [Header("--- 카메라 설정 ---")]
    public Camera uiCamera;

    [Header("--- UI 연결 (TMP) ---")]
    public GameObject lotteryPanel;
    public TextMeshProUGUI answerText;
    public TextMeshProUGUI[] myNumberTexts;
    public TextMeshProUGUI[] myPrizeAmountTexts;

    [Header("--- 긁기 설정 ---")]
    public RawImage[] dimCovers;
    public Texture2D defaultCoverTex;
    public int brushSize = 25;

    [Header("--- 당첨 확률 설정 ---")]
    public List<PrizeInfo> prizeTable;

    [Header("--- 기록(History) 설정 ---")]
    public Transform historyContent;       // Scroll View의 Content 오브젝트
    public GameObject historyItemPrefab;   // 기록 한 줄짜리 프리팹 (Text 하나 있는 거)


    private List<GameObject> historyObjList = new List<GameObject>(); // 생성된 기록 오브젝트 관리용

    // 내부 변수
    private int targetNumber;
    private bool isGameActive = false;
    private long currentWinningAmount = 0;

    // --- 구매 버튼 ---
    public void OnClick_Buy() {
        if (playerManager.satoshiBankCash >= ticketPrice) {
            playerManager.satoshiBankCash -= ticketPrice;

            PlayerManager.Instance.AddLotteryBuy(ticketPrice);

            Debug.Log($"복권 구매 완료! 잔액: {playerManager.satoshiBankCash}원");
            TransactionManager.Instance.AddRecord("복권", ticketPrice, "출금", "사토시 현금");

            lotteryPanel.SetActive(true);
            StartGame();
        } else {
            Debug.Log("잔액이 부족합니다 행님!");
        }
    }

    void StartGame() {
        isGameActive = true;
        currentWinningAmount = 0;

        targetNumber = Random.Range(1, 10);
        answerText.text = targetNumber.ToString();

        PrizeInfo selectedPrize = PickRandomPrize();
        SetupSlots(selectedPrize);
        ResetCovers();
    }

    PrizeInfo PickRandomPrize() {
        float totalWeight = 0;
        foreach (var p in prizeTable) totalWeight += p.probability;
        float rnd = Random.Range(0, totalWeight);
        float current = 0;
        foreach (var p in prizeTable) {
            current += p.probability;
            if (rnd <= current) return p;
        }
        return prizeTable[prizeTable.Count - 1];
    }

    void SetupSlots(PrizeInfo result) {
        List<int> availableFakeNumbers = new List<int>();
        for (int k = 1; k <= 9; k++) {
            if (k != targetNumber) availableFakeNumbers.Add(k);
        }

        for (int k = 0; k < availableFakeNumbers.Count; k++) {
            int temp = availableFakeNumbers[k];
            int randomIndex = Random.Range(k, availableFakeNumbers.Count);
            availableFakeNumbers[k] = availableFakeNumbers[randomIndex];
            availableFakeNumbers[randomIndex] = temp;
        }

        int winIndex = -1;
        if (result.prizeAmount > 0) {
            winIndex = Random.Range(0, 6);
        }

        int fakeDataIndex = 0;
        for (int i = 0; i < 6; i++) {
            if (i == winIndex) {
                myNumberTexts[i].text = targetNumber.ToString();
                myPrizeAmountTexts[i].text = NumberToKorean(result.prizeAmount);
                currentWinningAmount = result.prizeAmount;
            } else {
                int fakeNum = availableFakeNumbers[fakeDataIndex];
                fakeDataIndex++;
                myNumberTexts[i].text = fakeNum.ToString();
                long fakePrize = prizeTable[Random.Range(0, prizeTable.Count - 1)].prizeAmount;
                if (fakePrize == 0) fakePrize = 5000;
                myPrizeAmountTexts[i].text = NumberToKorean(fakePrize);
            }
        }
    }

    string NumberToKorean(long amount) {
        if (amount >= 100000000) return (amount / 100000000) + "억원";
        if (amount >= 10000) return (amount / 10000) + "만원";
        return amount + "원";
    }

    void Update() {
        if (isGameActive && Input.GetMouseButton(0)) {
            Scratch();
        }
    }

    void Scratch() {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results) {
            RawImage hitImage = result.gameObject.GetComponent<RawImage>();
            if (hitImage != null && IsMyDim(hitImage)) {
                if (hitImage.raycastTarget)
                    ErasePixels(hitImage, result.screenPosition);
            }
        }
    }

    void ErasePixels(RawImage target, Vector2 screenPos) {
        // (내용 동일하여 생략, 기존 코드 그대로 유지)
        Texture2D tex = target.texture as Texture2D;
        if (tex == null) return;
        RectTransform rt = target.rectTransform;
        Vector2 localPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, uiCamera, out localPos)) {
            float uvX = (localPos.x + rt.rect.width / 2) / rt.rect.width;
            float uvY = (localPos.y + rt.rect.height / 2) / rt.rect.height;
            int px = (int)(uvX * tex.width);
            int py = (int)(uvY * tex.height);
            bool pixelChanged = false;
            for (int y = -brushSize / 2; y < brushSize / 2; y++) {
                for (int x = -brushSize / 2; x < brushSize / 2; x++) {
                    if (px + x >= 0 && px + x < tex.width && py + y >= 0 && py + y < tex.height) {
                        if (tex.GetPixel(px + x, py + y).a > 0.1f) {
                            tex.SetPixel(px + x, py + y, Color.clear);
                            pixelChanged = true;
                        }
                    }
                }
            }
            if (pixelChanged) {
                tex.Apply();
                CheckAutoClear(target, tex);
            }
        }
    }

    void CheckAutoClear(RawImage target, Texture2D tex) {
        Color[] pixels = tex.GetPixels();
        int total = pixels.Length;
        int cleared = 0;
        for (int i = 0; i < total; i++) if (pixels[i].a < 0.1f) cleared++;
        float percent = (float)cleared / total;

        if (percent >= 0.65f) {
            for (int i = 0; i < total; i++) pixels[i] = Color.clear;
            tex.SetPixels(pixels);
            tex.Apply();
            target.raycastTarget = false;
            CheckAllClearedAndFinish();
        }
    }

    void CheckAllClearedAndFinish() {
        bool allCleared = true;
        foreach (var dim in dimCovers) {
            if (dim.raycastTarget == true) {
                allCleared = false;
                break;
            }
        }
        if (allCleared) {
            StartCoroutine(FinishRoutine());
        }
    }

    IEnumerator FinishRoutine() {
        yield return new WaitForSecondsRealtime(1.5f);

        string resultString = "낙첨"; // 기본값

        if (currentWinningAmount > 0) {
            playerManager.satoshiBankCash += currentWinningAmount;
            PlayerManager.Instance.AddLotteryWin(currentWinningAmount);

            resultString = NumberToKorean(currentWinningAmount);
            Debug.Log($"★ {currentWinningAmount}원 입금 완료!");
            TransactionManager.Instance.AddRecord("복권", currentWinningAmount, "입금", "사토시 현금");
        } else {
            Debug.Log("꽝입니다...");
        }

        AddHistoryLog(resultString);


        lotteryPanel.SetActive(false);
        isGameActive = false;
    }

    void AddHistoryLog(string resultStr) {
        // CoinManager에서 현재 게임 날짜 가져오기
        string dateStr = CoinManager.Instance.GetCurrentDateString();

        // 프리팹 생성
        GameObject newItem = Instantiate(historyItemPrefab, historyContent);

        // 텍스트 설정
        TextMeshProUGUI itemText = newItem.GetComponentInChildren<TextMeshProUGUI>();
        if (itemText != null) {
            itemText.text = $"{dateStr} : {resultStr}";
        }

        // (아래 정렬/삭제 로직은 그대로...)
        newItem.transform.SetAsFirstSibling();
        historyObjList.Insert(0, newItem);

        if (historyObjList.Count > 30) {
            GameObject oldItem = historyObjList[historyObjList.Count - 1];
            historyObjList.RemoveAt(historyObjList.Count - 1);
            Destroy(oldItem);
        }
    }

    void ResetCovers() {
        foreach (var dim in dimCovers) {
            Texture2D newTex = new Texture2D(defaultCoverTex.width, defaultCoverTex.height);
            newTex.filterMode = FilterMode.Point;
            newTex.SetPixels(defaultCoverTex.GetPixels());
            newTex.Apply();
            dim.texture = newTex;
            dim.enabled = true;
            dim.raycastTarget = true;
        }
    }

    bool IsMyDim(RawImage img) {
        foreach (var d in dimCovers) if (d == img) return true;
        return false;
    }

    public void OnClickInstantScratch() {
        if (!isGameActive) return;
        foreach (var dim in dimCovers) {
            dim.enabled = false;
            dim.raycastTarget = false;
        }
        CheckAllClearedAndFinish();
    }
}