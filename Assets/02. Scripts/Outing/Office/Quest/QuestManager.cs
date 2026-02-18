using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour {
    public static QuestManager Instance;

    public List<QuestSO> allQuests; // 인스펙터에서 SO 등록
    public GameObject questPrefab;
    public Transform contentParent;

    private Dictionary<string, QuestProgress> progressDict = new Dictionary<string, QuestProgress>();
    private Dictionary<string, QuestUIItem> uiItemDict = new Dictionary<string, QuestUIItem>();

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start() {
        GenerateQuests();
    }

    void GenerateQuests() {
        foreach (var so in allQuests) {
            QuestProgress newProgress = new QuestProgress { questID = so.questID };
            progressDict.Add(so.questID, newProgress);

            GameObject go = Instantiate(questPrefab, contentParent);
            QuestUIItem uiItem = go.GetComponent<QuestUIItem>();
            uiItem.Setup(so, newProgress);

            uiItemDict.Add(so.questID, uiItem);
        }

        foreach (var kv in uiItemDict) {
            string id = kv.Key;
            QuestUIItem item = kv.Value;

            // 만약 이미 보상을 받았다면?
            if (progressDict[id].isClaimed) {
                // 맨 아래로 이동!
                item.transform.SetAsLastSibling();
            }
        }
    }

    // 예전 'AddQuestProgress'를 대신하는 함수
    // [수정] int amount -> double amount
    // [수정] 어떤 코인을 거래했는지(symbol) 파라미터 추가!
    // symbol이 null이면 "모든 코인"을 대상으로 함 (기존 퀘스트 호환)
    public void ProcessAction(QuestType type, double amount, string symbol = null) {
        foreach (var so in allQuests) {
            if (so.type != type) continue;
            if (!progressDict.ContainsKey(so.questID)) continue;

            // [추가] 테마 조건 체크
            if (so.useThemeCondition && !string.IsNullOrEmpty(symbol)) {
                // 1. 거래한 코인의 정보를 찾음
                var coinMeta = System.Array.Find(CoinMetaDatabase.AllCoins, c => c.Symbol == symbol);

                // 2. 코인 정보가 없거나, 테마가 다르면 카운트 안 함
                if (coinMeta == null || coinMeta.Theme != so.targetTheme) {
                    continue;
                }
            }

            var progress = progressDict[so.questID];
            if (!progress.isClaimed && !progress.IsCompleted(so.targetCount)) {
                progress.currentCount += amount;
                uiItemDict[so.questID].UpdateUI();
            }
        }
    }

    public void ClaimReward(string id) {
        var so = allQuests.Find(s => s.questID == id);
        var progress = progressDict[id];

        if (progress.IsCompleted(so.targetCount) && !progress.isClaimed) {
            progress.isClaimed = true;

            // 리스트에 있는 모든 보상을 지급
            foreach (var reward in so.rewards) {
                switch (reward.type) {
                    // 1. 현금 보상 (Cash)
                    case RewardType.Cash:
                        if (reward.targetID == "Bullbit") {
                            // 불비트 예수금 증가 (입금 내역도 관리됨)
                            PlayerManager.Instance.ChangeBullbitCash(reward.amount);
                        } else if (reward.targetID == "Satoshi") {
                            // 사토시 은행 예금 증가
                            PlayerManager.Instance.ChangeSatoshiMoney(reward.amount);
                        } else if (reward.targetID == "FourNance") {
                            // 선물 거래소 (직접 접근 혹은 함수 추가 필요)
                            PlayerManager.Instance.fournanceCash += reward.amount;
                        }

                        Debug.Log($"[보상] 현금 지급 ({reward.targetID}): {reward.amount:N0}원");
                        break;

                    // 2. 코인 보상 (Crypto) -> 에어드랍 로직
                    case RewardType.Crypto:
                        // RegisterTransferIn 함수 사용 (외부 입금/에어드랍 전용)
                        // price를 0으로 넣으면 '공짜'로 받은 것이 되어 평단가가 확 낮아집니다. (개이득!)
                        PlayerManager.Instance.RegisterTransferIn(reward.targetID, 0, reward.amount);

                        Debug.Log($"[보상] 코인 에어드랍 ({reward.targetID}): {reward.amount}개");
                        break;

                    // 3. 아이템 등
                    case RewardType.Item:
                        Debug.Log($"[보상] 아이템 지급: {reward.targetID} {reward.amount}개");
                        break;
                }
            }

            // UI 갱신 (완료됨 표시)
            uiItemDict[id].UpdateUI();

            uiItemDict[id].transform.SetAsLastSibling();
        }
    }
}