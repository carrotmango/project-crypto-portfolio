using UnityEngine;
using UnityEngine.UI; // RawImage용
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

    // 내부 변수
    private int targetNumber;
    private bool isGameActive = false;
    private long currentWinningAmount = 0;

    // --- 구매 버튼 (돈 나가는 곳) ---
    public void OnClick_Buy() {
        if (playerManager.satoshiBankCash >= ticketPrice) {
            playerManager.satoshiBankCash -= ticketPrice;
            Debug.Log($"복권 구매 완료! 잔액: {playerManager.satoshiBankCash}원");

            lotteryPanel.SetActive(true);
            StartGame();
        } else {
            Debug.Log("잔액이 부족합니다 행님!");
        }
    }

    void StartGame() {
        isGameActive = true;
        currentWinningAmount = 0;

        // 1. 행운 숫자 (1~9) 정하기
        targetNumber = Random.Range(1, 10);
        answerText.text = targetNumber.ToString();

        // 2. 당첨 여부 결정
        PrizeInfo selectedPrize = PickRandomPrize();

        // 3. 슬롯 배치 (★여기가 중요함)
        SetupSlots(selectedPrize);

        // 4. 은박지 리셋
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

    // ★★★ [수정 핵심] 중복 방지 로직 적용 ★★★
    void SetupSlots(PrizeInfo result) {
        // 1. 오답으로 쓸 숫자들 목록을 미리 만듭니다 (정답 숫자 제외!)
        List<int> availableFakeNumbers = new List<int>();
        for (int k = 1; k <= 9; k++) {
            // 정답 숫자(targetNumber)는 오답 목록에 절대 넣지 않음
            if (k != targetNumber) availableFakeNumbers.Add(k);
        }

        // 2. 오답 목록을 마구 섞습니다 (Shuffle)
        // 이렇게 하면 앞에서부터 하나씩 꺼내도 랜덤하고, 서로 겹치지도 않습니다.
        for (int k = 0; k < availableFakeNumbers.Count; k++) {
            int temp = availableFakeNumbers[k];
            int randomIndex = Random.Range(k, availableFakeNumbers.Count);
            availableFakeNumbers[k] = availableFakeNumbers[randomIndex];
            availableFakeNumbers[randomIndex] = temp;
        }

        // 3. 당첨될 자리 정하기 (당첨금이 있을 때만)
        int winIndex = -1; // -1이면 당첨 없음
        if (result.prizeAmount > 0) {
            winIndex = Random.Range(0, 6);
        }

        // 4. 슬롯 채우기
        int fakeDataIndex = 0; // 섞어둔 오답 리스트에서 하나씩 꺼낼 인덱스

        for (int i = 0; i < 6; i++) {
            // A. 당첨 자리인 경우
            if (i == winIndex) {
                myNumberTexts[i].text = targetNumber.ToString(); // 정답 숫자
                myPrizeAmountTexts[i].text = NumberToKorean(result.prizeAmount);
                currentWinningAmount = result.prizeAmount;
            }
            // B. 꽝 자리인 경우
            else {
                // 아까 섞어둔 리스트에서 하나 꺼냄 (정답이 아님이 보장됨 + 서로 안 겹침)
                int fakeNum = availableFakeNumbers[fakeDataIndex];
                fakeDataIndex++; // 다음 숫자로 넘어감

                myNumberTexts[i].text = fakeNum.ToString();

                // 꽝일 때 가짜 금액 표시
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

        for (int i = 0; i < total; i++) {
            if (pixels[i].a < 0.1f) cleared++;
        }

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

        if (currentWinningAmount > 0) {
            playerManager.satoshiBankCash += currentWinningAmount;
            Debug.Log($"★ {currentWinningAmount}원 입금 완료! 현재 잔액: {playerManager.satoshiBankCash}");
        } else {
            Debug.Log("꽝입니다. 5천원 날렸습니다...");
        }

        lotteryPanel.SetActive(false);
        isGameActive = false;
    }

    void ResetCovers() {
        foreach (var dim in dimCovers) {
            Texture2D newTex = new Texture2D(defaultCoverTex.width, defaultCoverTex.height);
            newTex.filterMode = FilterMode.Point;
            newTex.SetPixels(defaultCoverTex.GetPixels());
            newTex.Apply();

            dim.texture = newTex;
            dim.raycastTarget = true;
        }
    }

    bool IsMyDim(RawImage img) {
        foreach (var d in dimCovers) if (d == img) return true;
        return false;
    }
}