using TMPro;
using UnityEngine;

public class TransactionItem : MonoBehaviour {
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI typeText;

    public void Setup(TransactionData data) {
        dateText.text = data.date;

        // 1. 내역(Target) 현지화 - 여기서 한글을 영어 키값으로 바꿔줍니다.
        targetText.text = GetLocalizedTarget(data.target);

        string unit = LocalizationManager.GetText("UNIT_CURRENCY");

        // 2. "입금" 여부 판정 (데이터는 무조건 한글 "입금"이어야 함)
        if (data.type == "입금") {
            typeText.text = LocalizationManager.GetText("LOG_DEPOSIT");
            amountText.text = $"+{data.amount:N0} {unit}";
            amountText.color = new Color32(50, 214, 149, 255);
        } else {
            typeText.text = LocalizationManager.GetText("LOG_WITHDRAW");
            amountText.text = $"-{data.amount:N0} {unit}";
            amountText.color = new Color32(230, 60, 60, 255);
        }
    }

    private string GetLocalizedTarget(string rawTarget) {
        // 형님이 사용하시는 모든 한글 키워드를 여기에 매핑해야 영어가 나옵니다!
        switch (rawTarget) {
            case "급여": return LocalizationManager.GetText("LOG_SALARY");
            case "아르바이트": return LocalizationManager.GetText("LOG_PART_TIME_JOB");
            case "오락실": return LocalizationManager.GetText("LOG_ARCADE");
            case "부동산": return LocalizationManager.GetText("LOG_REAL_ESTATE");
            case "자본투입": return LocalizationManager.GetText("LOG_CAPITAL_INJECT");

            // 대출 관련
            case "대출실행": return LocalizationManager.GetText("LOG_LOAN_ISSUE");
            case "대출상환": return LocalizationManager.GetText("LOG_LOAN_REPAY");
            case "대출이자": return LocalizationManager.GetText("LOG_LOAN_INTEREST");
            case "대출이자(조기상환)": return LocalizationManager.GetText("LOG_LOAN_EARLY_FEE");

            // 거래소 & 스킬
            case "불비트": return LocalizationManager.GetText("LOG_BULLBIT");
            case "스킬강화": return LocalizationManager.GetText("LOG_SKILL_UPGRADE");

            // 예금 관련
            case "예금가입": return LocalizationManager.GetText("LOG_JOIN_DEPOSIT");
            case "예금만기": return LocalizationManager.GetText("LOG_EXPIRE_DEPOSIT");
            case "예금 중도해지": return LocalizationManager.GetText("LOG_CANCEL_DEPOSIT");

            // 세금 관련
            case "소득세": return LocalizationManager.GetText("LOG_TAX");
            case "소득세(강제징수)": return LocalizationManager.GetText("LOG_TAX_ENFORCED");

            // [추가됨] 해외 거래소 관련
            case "포넨스": return LocalizationManager.GetText("LOG_FOURNANCE");
            case string s when s.Contains("해외송금"): return LocalizationManager.GetText("LOG_OVERSEAS_REMITTANCE");

            // 매핑되지 않은 단어가 들어오면 일단 그대로 출력 (디버깅용)
            default: return rawTarget;
        }
    }
}