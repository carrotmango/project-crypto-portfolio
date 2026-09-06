using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ResearchNewsLoader : MonoBehaviour {
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI providerText;
    public TextMeshProUGUI timeText;
    public Button clickButton;

    private UIEventData currentData;

    public void Setup(UIEventData data, DateTime occurredTime) {
        currentData = data;

        // article 객체가 있을 때만 셋팅
        if (data.article != null) {
            titleText.text = data.article.title;
            providerText.text = data.article.provider;
            timeText.text = GetRelativeTime(occurredTime);
        }

        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => {
            // [3단계] 여기서 가변 팝업을 띄울 겁니다!
            EventUIManager.Instance.OpenNewsDetail(currentData);
        });
    }

    private string GetRelativeTime(DateTime occurredTime) {
        // 1. 기준 시간을 현실 시간이 아닌 게임 시간(CoinManager)으로 설정
        DateTime gameNow = CoinManager.Instance.CurrentDateTime;
        TimeSpan diff = gameNow - occurredTime;

        // 미래 시간 방어 로직 (혹시나 발생 시간이 현재보다 앞선 경우)
        if (diff.TotalSeconds < 0) return LocalizationManager.GetText("LBL_TIME_JUST_NOW");

        // 2. 1시간 이내 (60분 미만) -> 방금 (Just now)
        if (diff.TotalMinutes < 60) {
            return LocalizationManager.GetText("LBL_TIME_JUST_NOW");
        }

        // 3. 당일 (24시간 이내) -> n시간 전 ({0} hours ago)
        if (diff.TotalHours < 24) {
            return string.Format(LocalizationManager.GetText("LBL_TIME_HOURS_AGO"), (int)diff.TotalHours);
        }

        // 4. 그 이상 -> n일 전 ({0} days ago)
        return string.Format(LocalizationManager.GetText("LBL_TIME_DAYS_AGO"), (int)diff.TotalDays);
    }
}