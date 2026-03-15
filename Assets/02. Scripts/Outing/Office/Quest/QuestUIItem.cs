using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestUIItem : MonoBehaviour {
    [Header("UI 연결")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI rewardText;
    public Button rewardButton;
    public TextMeshProUGUI buttonText;

    private QuestSO _so;
    private QuestProgress _progress;

    [Header("색상 설정")]
    public Color normalColor = Color.white;
    public Color completedColor = Color.green;
    public Color claimedColor = Color.gray;

    public void Setup(QuestSO so, QuestProgress progress) {
        _so = so;
        _progress = progress;

        // [수정] 글자 세팅하는 부분을 통째로 분리해서 호출!
        RefreshLanguage();

        rewardButton.onClick.RemoveAllListeners();
        rewardButton.onClick.AddListener(() => QuestManager.Instance.ClaimReward(_so.questID));
    }

    // ==========================================
    // [NEW] 패널이 화면에 켜질 때마다 현재 언어로 새로고침!
    // ==========================================
    private void OnEnable() {
        if (_so != null && _progress != null) {
            RefreshLanguage();
        }
    }

    // ==========================================
    // [NEW] 텍스트만 싹 다시 입혀주는 전용 함수
    // ==========================================
    public void RefreshLanguage() {
        titleText.text = LocalizationManager.GetText(_so.title);

        if (descriptionText != null) {
            descriptionText.text = LocalizationManager.GetText(_so.description);
        }

        // 보상 리스트 텍스트 처리
        if (_so.rewards != null && _so.rewards.Count > 0) {
            var firstReward = _so.rewards[0];
            string displayName = GetRewardDisplayName(firstReward);
            string displayStr = $"{firstReward.amount:N0} {displayName}";

            if (_so.rewards.Count > 1) {
                int extraCount = _so.rewards.Count - 1;
                string extraFormat = LocalizationManager.GetText("LBL_REWARD_EXTRA");
                displayStr += string.Format(extraFormat, extraCount);
            }
            rewardText.text = displayStr;
        } else {
            rewardText.text = LocalizationManager.GetText("LBL_REWARD_NONE");
        }

        UpdateUI();
    }

    // 내부 ID를 플레이어가 보는 이름으로 바꿔주는 함수
    private string GetRewardDisplayName(QuestReward reward) {
        string defaultUnit = LocalizationManager.GetText("UNIT_CURRENCY");
        string usdUnit = LocalizationManager.GetText("CURRENCY_USD");

        if (reward.type == RewardType.Cash) {
            switch (reward.targetID) {
                case "Bullbit": return defaultUnit;
                case "Satoshi": return defaultUnit;
                case "FourNance": return usdUnit;
                default: return defaultUnit;
            }
        } else if (reward.type == RewardType.Crypto) {
            return reward.targetID;
        }

        return reward.targetID;
    }

    public void UpdateUI() {
        progressText.text = $"{_progress.currentCount:N0} / {_so.targetCount:N0}";

        if (_progress.isClaimed) {
            rewardButton.interactable = false;
            buttonText.text = LocalizationManager.GetText("BTN_QUEST_COMPLETED");
            SetTextColor(claimedColor);
        } else if (_progress.IsCompleted(_so.targetCount)) {
            rewardButton.interactable = true;
            buttonText.text = LocalizationManager.GetText("BTN_QUEST_CLAIM");
            SetTextColor(completedColor);
        } else {
            rewardButton.interactable = false;
            buttonText.text = LocalizationManager.GetText("BTN_QUEST_ONGOING");
            SetTextColor(normalColor);
        }
    }

    private void SetTextColor(Color color) {
        titleText.color = color;
        if (descriptionText != null) descriptionText.color = color;
        progressText.color = color;
        rewardText.color = color;
    }
}