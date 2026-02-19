using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Quest_", menuName = "Quest/QuestData")]
public class QuestSO : ScriptableObject {
    [Header("분류")]
    public QuestCategory category;

    [Header("기본 정보")]
    public string questID;
    public string title;
    [TextArea] public string description;

    [Header("달성 조건")]
    public QuestType type;
    public double targetCount; // [수정] int -> double

    [Header("테마 조건 (옵션)")]
    public bool useThemeCondition;  // 체크하면 특정 테마만 인정
    public CoinTheme targetTheme;   // 목표 테마 (예: Meme)

    [Header("보상")]
    public List<QuestReward> rewards;

    [Header("연계 설정")]
    public string prerequisiteQuestID;
}