using System.Collections.Generic;
using UnityEngine;

public class DailyIncomeManager : MonoBehaviour {
    public static DailyIncomeManager Instance;

    private int salaryIncome = 0;
    private int estateIncomeTotal = 0; // ★ 핵심: 부동산 합계
    private List<string> estateLogs = new();

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 급여 등록
    public void AddSalary(int amount, string label) {
        if (amount <= 0) return;
        salaryIncome += amount;
    }

    // 부동산 수입 등록 (건물 단위)
    public void AddEstateIncome(string estateName, int amount) {
        if (amount <= 0) return;

        estateIncomeTotal += amount; // ★ 여기서 누적
        estateLogs.Add($"{estateName}: +{amount:N0}원");
    }

    // 하루 종료 시 호출
    public void FlushAndNotify() {
        if (salaryIncome == 0 && estateIncomeTotal == 0)
            return;

        int total = salaryIncome + estateIncomeTotal;

        string body = "";

        if (salaryIncome > 0) {
            body += "■ 급여\n";
            body += $"- 대표 급여: +{salaryIncome:N0}원\n\n";
        }

        if (estateLogs.Count > 0) {
            body += "■ 부동산\n";
            foreach (var log in estateLogs) {
                body += $"- {log}\n";
            }
            body += "\n";
        }

        body += $"총 수입: +{total:N0}원";

        SMSNotificationManager.Instance.ReceiveSMS(
            "일일 수입 정산",
            $"오늘 수입 +{total:N0}원",
            body
        );

        Clear();
    }

    void Clear() {
        salaryIncome = 0;
        estateIncomeTotal = 0;
        estateLogs.Clear();
    }
}
