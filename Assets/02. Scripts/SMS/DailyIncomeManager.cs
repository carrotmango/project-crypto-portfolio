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
    public void FlushAndNotify() {
        // 급여가 없으면 알림 안 보냄
        if (salaryIncome <= 0) return;

        long total = salaryIncome;

        // 1. 알림창 제목/내용 (심플하게)
        string notiTitle = "급여 입금";
        string notiMsg = $"급여 {total:N0}원이 입금되었습니다.";

        // 2. 상세 팝업 내용
        string fullBody = "[급여 명세서]\n\n";
        fullBody += $"■ 기본 급여: +{total:N0}원\n";
        fullBody += "\n--------------------------------\n";
        fullBody += $"실 수령액: {total:N0}원\n";
        fullBody += "사토시 은행 계좌로 지급되었습니다.";

        TransactionManager.Instance.AddRecord("급여", total, "입금", "사토시 현금");

        // 3. 글로벌 알림 호출 (파란색 Bank 테마)
        if (GlobalNotificationManager.Instance != null) {
            GlobalNotificationManager.Instance.ShowNotification(
                "Bank",
                notiTitle,
                notiMsg,
                () => {         // 클릭 시 상세 명세서 팝업
                    if (UIManager.Instance != null) {
                        UIManager.Instance.ShowSMSResult(
                            $"발신인: 사토시 은행\n\n{fullBody}"
                        );
                    }
                }
            );
        }

        // 초기화
        salaryIncome = 0;
    }
}