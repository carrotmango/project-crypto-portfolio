using UnityEngine;

using System;

[System.Serializable]
public class DialogueNode {
    public string text;                 // 대사 내용
    public bool isChoice;               // 분기 여부
    public DialogueNode next;           // 기본 다음 노드
    public DialogueNode yesNext;        // 예
    public DialogueNode noNext;         // 아니오
    public DialogueAction action;       // 특수 행동
    public HighlightTarget highlightTarget; // 추가
    public float blockPanelAlpha = -1f;
}

public enum DialogueAction {
    None,
    HighlightAppButton,
    HighlightBullbitButton,
    HighlightBitcoinBuy,
    HighlightEthBuy,
    HighlightPortfolioButton,
    HighlightSymbolLabel_ON,
    HighlightSymbolLabel_OFF,
    WaitForAppClick,
    WaitForSatoshiDepositButton,
    WaitForWithdrawButton,
    WaitForTransfer,
    WaitForClickBullPort,
    MissionPause,   // 미션 수행 대기
    MissionResume,   // 미션 끝나고 돌아오기
    WaitForBTCDetailPanel,
    WaitForDetailBuy,
    WaitForDetailSell,
    HighlightCompanyButton
}

public enum HighlightTarget {
    None,
    Bullbit,
    spotButton,
    Satoshi,
    perpDim,
    perpAndEstateDim,
    PartTimeJob,
    Estate,
    Xbird,
    Gamble,
    SatoshiDimWithButton,
    WithdrawButton,
    TransferDim,
    bankExplainDim,
    AmountDim,
    BorderDim,
    BorderDim2,
    OfficeBorder,
    depositDim,
    skillDim,
    questDim,
    researchDim,
    timeDim
}


