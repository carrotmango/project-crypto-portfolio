using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TransactionData {
    public string date;
    public string target;
    public double amount;
    public string type;
    public string balanceType;

    // 생성자: 데이터를 편하게 만들기 위한 틀
    public TransactionData(string target, double amount, string type, string balanceType) {
        this.date = CoinManager.Instance.CurrentDateTime.ToString("MM.dd HH:mm");
        this.target = target;
        this.amount = amount;
        this.type = type;
        this.balanceType = balanceType;
    }
} // <--- 이 괄호가 빠져서 에러가 났던 거예요!

public class TransactionManager : MonoBehaviour {
    public static TransactionManager Instance;

    [Header("UI Reference")]
    public GameObject transactionItemPrefab;
    public Transform contentParent;

    public List<TransactionData> history = new List<TransactionData>();

    void Awake() => Instance = this;

    public void AddRecord(string target, double amount, string type, string balanceType) {
        // 이제 여기서 에러가 안 날 겁니다!
        TransactionData newRecord = new TransactionData(target, amount, type, balanceType);
        history.Add(newRecord);

        UpdateUI(newRecord);
        Debug.Log($"[{type}] {target} : {amount:N0}원 기록 완료");
    }

    void UpdateUI(TransactionData data) {
        if (transactionItemPrefab == null || contentParent == null) return;

        // 1. 프리팹 생성
        GameObject newItem = Instantiate(transactionItemPrefab, contentParent);

        // 2. 생성되자마자 부모(Content)의 첫 번째 자식으로 순서를 바꿈 (역순 정렬)
        newItem.transform.SetAsFirstSibling();

        TransactionItem itemScript = newItem.GetComponent<TransactionItem>();
        if (itemScript != null) {
            itemScript.Setup(data);
        }
    }
}