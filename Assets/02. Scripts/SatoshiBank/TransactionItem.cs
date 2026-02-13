using TMPro;
using UnityEngine;

public class TransactionItem : MonoBehaviour {
    public TextMeshProUGUI dateText;    // 날짜/시간
    public TextMeshProUGUI targetText;  // 입금처 (대상)
    public TextMeshProUGUI amountText;  // 금액
    public TextMeshProUGUI typeText;    // 입금인지 출금인지 표시할 칸 (추가)

    public void Setup(TransactionData data) {
        dateText.text = data.date;
        targetText.text = data.target;
        typeText.text = data.type; // "입금" 또는 "출금" 글자 표시

        // 금액 표기 및 색상 변경 (가독성 업그레이드)
        if (data.type == "입금") {
            amountText.text = $"+{data.amount:N0}";
            amountText.color = new Color32(50, 214, 149, 255);
        } else {
            amountText.text = $"-{data.amount:N0}";
            amountText.color = new Color32(230, 60, 60, 255);
        }
    }
}

