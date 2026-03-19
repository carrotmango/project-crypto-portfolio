using System.Collections.Generic;
using UnityEngine;

public class DailyIncomeManager : MonoBehaviour {
    public static DailyIncomeManager Instance;

    private long salaryIncome = 0;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddSalary(long amount, string label) {
        if (amount <= 0) return;
        salaryIncome += amount;
    }
    public void AddEstateIncome(string estateName, long amount) {
        // RealEstatePanelController가 직접 알림을 보내므로 여기선 집계하지 않음
    }

    // 하루 종료(00:00) 시 호출
    // [DailyIncomeManager.cs 내부의 FlushAndNotify 함수 수정]
    public void FlushAndNotify() {
        if (salaryIncome <= 0) return;

        long total = salaryIncome;
        string unit = LocalizationManager.GetText("UNIT_CURRENCY");

        // UI용 텍스트들은 번역해도 됩니다.
        string notiTitle = LocalizationManager.GetText("NOTI_SALARY_TITLE");
        string notiMsg = string.Format(LocalizationManager.GetText("NOTI_SALARY_MSG"), total.ToString("N0"), unit);

        string fullBody = LocalizationManager.GetText("SMS_SALARY_STATEMENT");
        fullBody += string.Format(LocalizationManager.GetText("SMS_SALARY_BASE"), total.ToString("N0"), unit);
        fullBody += "\n--------------------------------\n";
        fullBody += string.Format(LocalizationManager.GetText("SMS_SALARY_NET"), total.ToString("N0"), unit);
        fullBody += LocalizationManager.GetText("SMS_SALARY_FOOTER");

        // ★★★ [가장 중요한 수정] ★★★
        // 기록을 남길 때는 번역된 logSalary, logDeposit을 쓰지 말고 
        // 무조건 한글 원본 "급여", "입금"을 직접 넣으세요.
        TransactionManager.Instance.AddRecord("급여", total, "입금", "사토시 현금");

        if (GlobalNotificationManager.Instance != null) {
            GlobalNotificationManager.Instance.ShowNotification(
                "Bank",
                notiTitle,
                notiMsg,
                () => {
                    if (UIManager.Instance != null) {
                        string senderMsg = string.Format(LocalizationManager.GetText("SMS_BANK_SENDER"), fullBody);
                        UIManager.Instance.ShowSMSResult(senderMsg);
                    }
                }
            );
        }

        salaryIncome = 0;
    }
}