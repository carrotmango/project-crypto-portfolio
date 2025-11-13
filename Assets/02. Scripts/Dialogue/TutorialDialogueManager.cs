using UnityEngine;
using UnityEngine.UI;

public class TutorialDialogueManager : DialogueManager {
    public Button appButton;
    public GameObject appDim;

    protected override void HandleAction(DialogueAction action, int phase) {
        if (action == DialogueAction.HighlightAppButton && phase == 1) {
            // 두 번째 클릭 시 다이얼로그 숨기고 하이라이트
            dialogueText.text = "";
            appDim.SetActive(true);

            HighlightButton(appButton);
            appButton.onClick.RemoveAllListeners();
            appButton.onClick.AddListener(() => {
                StopHighlight(appButton);
                appDim.SetActive(false);
                // 앱 버튼 누르면 다음 대사로 진행
                currentNode = currentNode.next;
                ShowDialogue();
            });
        }
    }

    private void HighlightButton(Button target) {
        // 하이라이트 효과 넣기 (기존 코루틴 그대로 사용 가능)
    }

    private void StopHighlight(Button target) {
        // 하이라이트 중단
    }
}
