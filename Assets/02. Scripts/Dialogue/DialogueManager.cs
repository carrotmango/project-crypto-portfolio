using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour {
    [Header("UI")]
    public TextMeshProUGUI dialogueText;
    public Button dialoguePanelButton;
    public Button yesButton;
    public Button noButton;

    protected DialogueNode currentNode;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool textCompleted = false;

    private enum State { Idle, Dialogue, WaitingForAction, Finished }
    private State state = State.Idle;


    public void StartDialogue(DialogueNode startNode) {
        currentNode = startNode;
        state = State.Dialogue;
        ShowDialogue();
    }

    public void ShowDialogue() {
        if (currentNode == null) return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentNode.text));
    }

    private IEnumerator TypeText(string message) {
        dialogueText.text = "";
        isTyping = true;
        textCompleted = false;

        foreach (char c in message) {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(0.03f);
        }

        isTyping = false;
        textCompleted = true;

        if (currentNode.isChoice) {
            yesButton.gameObject.SetActive(true);
            noButton.gameObject.SetActive(true);
        }

        HandleAction(currentNode.action, phase: 0); // phase=0: 출력 완료 시점
    }

    public void OnDialogueClick() {
        if (state != State.Dialogue) return;

        // (1) 애니메이션 중 → 전체 출력
        if (isTyping) {
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentNode.text;
            isTyping = false;
            textCompleted = true;
            return;
        }

        // (2) 아직 텍스트 다 안 나왔으면 무시
        if (!textCompleted) return;

        // (3) 분기라면 yes/no만 허용
        if (currentNode.isChoice) return;

        // (4) 액션이 필요한 노드라면: 여기서 2번째 클릭에서 실행
        if (currentNode.action != DialogueAction.None) {
            HandleAction(currentNode.action, phase: 1); // phase=1: 대사 완료 후 클릭 시 실행
            return;
        }

        // (5) 일반 다음 대사 진행
        currentNode = currentNode.next;
        if (currentNode == null) {
            EndDialogue();
        } else {
            ShowDialogue();
        }
    }

    public void OnYesClick() {
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        currentNode = currentNode.yesNext;
        ShowDialogue();
    }

    public void OnNoClick() {
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        currentNode = currentNode.noNext;
        ShowDialogue();
    }

    protected virtual void HandleAction(DialogueAction action, int phase) {
        // 기본은 아무 일도 안 함
        // TutorialManager / StoryManager에서 오버라이드
    }

    private void EndDialogue() {
        state = State.Finished;
        dialogueText.text = "";
    }

}
