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
    public Color normalColor = Color.white;      // 진행 중일 때 색상
    public Color completedColor = Color.green;   // 보상 받을 수 있을 때 색상 (선택 사항)
    public Color claimedColor = Color.gray;      // 보상 받은 후(완료) 색상

    public void Setup(QuestSO so, QuestProgress progress) {
        _so = so;
        _progress = progress;

        titleText.text = _so.title;

        if (descriptionText != null) {
            descriptionText.text = _so.description;
        }

        // [수정] 보상 리스트가 있을 때 처리
        if (_so.rewards != null && _so.rewards.Count > 0) {
            // 첫 번째 보상 가져오기
            var firstReward = _so.rewards[0];

            // ★ 내부 ID를 화면용 이름으로 변환 (Bullbit -> 원)
            string displayName = GetRewardDisplayName(firstReward);

            // 금액 + 변환된 이름 조합 (예: "500,000 원")
            string displayStr = $"{firstReward.amount:N0} {displayName}";

            // 보상이 2개 이상이면 "외 N건" 추가
            if (_so.rewards.Count > 1) {
                displayStr += $" 외 {_so.rewards.Count - 1}건";
            }
            rewardText.text = displayStr;
        } else {
            rewardText.text = "보상 없음";
        }

        UpdateUI();

        rewardButton.onClick.RemoveAllListeners();
        rewardButton.onClick.AddListener(() => QuestManager.Instance.ClaimReward(_so.questID));
    }

    // [추가] 내부 ID를 플레이어가 보는 이름으로 바꿔주는 함수
    private string GetRewardDisplayName(QuestReward reward) {
        // 1. 현금인 경우
        if (reward.type == RewardType.Cash) {
            switch (reward.targetID) {
                case "Bullbit": return "원";           // 불비트 예수금 -> "원"
                case "Satoshi": return "원";    // 사토시 은행 -> "원 (예금)"
                case "FourNance": return "USD";      // 선물 거래소 -> "USDT"
                default: return "원";               // 그 외
            }
        }
        // 2. 코인인 경우 (보통 ID가 곧 이름이므로 그대로 씀)
        else if (reward.type == RewardType.Crypto) {
            return reward.targetID; // BTC, DOGE 등은 그대로 표시
        }

        return reward.targetID;
    }

    public void UpdateUI() {
        progressText.text = $"{_progress.currentCount:N0} / {_so.targetCount:N0}";

        // 1. 이미 보상을 받은 상태 (완료 & 수령)
        if (_progress.isClaimed) {
            rewardButton.interactable = false;
            buttonText.text = "완료됨";

            // [추가] 텍스트 색상을 회색으로 변경
            SetTextColor(claimedColor);
        }
        // 2. 목표는 달성했지만 아직 보상을 안 받은 상태
        else if (_progress.IsCompleted(_so.targetCount)) {
            rewardButton.interactable = true;
            buttonText.text = "보상 받기";

            // [추가] 눈에 띄게 초록색 등으로 (선택)
            SetTextColor(completedColor);
        }
        // 3. 진행 중
        else {
            rewardButton.interactable = false;
            buttonText.text = "진행 중";

            // [추가] 기본 색상
            SetTextColor(normalColor);
        }
    }

    // 텍스트 색깔을 한꺼번에 바꿔주는 함수
    private void SetTextColor(Color color) {
        titleText.color = color;
        descriptionText.color = color;
        progressText.color = color;
        rewardText.color = color;
    }
}