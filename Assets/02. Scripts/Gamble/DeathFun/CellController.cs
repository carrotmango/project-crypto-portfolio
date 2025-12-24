using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CellController : MonoBehaviour, IPointerClickHandler {
    [Header("상태")]
    public bool isBomb = false;

    [Header("구성 요소")]
    public Image skullIcon;
    public StageManager stageManager;
    public DeathFunPanel deathFunPanel;

    private RectTransform rectTransform;
    private Image image;
    private bool isClicked = false;
    private bool isInteractable = false;
    private bool isDisabled = false;

    private static readonly Color DarkGreen = new Color(0.1f, 0.4f, 0.1f);
    private static readonly Color DarkRed = new Color(0.4f, 0.1f, 0.1f);
    private static readonly Color DimGray = new Color(0.5f, 0.5f, 0.5f);

    private bool wasPassed = false;
    //private bool wasFailed = false;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        if (skullIcon != null)
            skullIcon.gameObject.SetActive(false);

        if (stageManager == null)
            stageManager = Object.FindFirstObjectByType<StageManager>();
        if (deathFunPanel == null)
            deathFunPanel = Object.FindFirstObjectByType<DeathFunPanel>();
    }

    public void DisableClick() {
        isDisabled = true;
        isInteractable = false;
        //image.color = Color.yellow;
    }

    public void EnableClick() {
        isClicked = false;
        isDisabled = false;
        isInteractable = true;
        image.color = Color.white;
    }

    public void SetBomb(bool bomb) {
        isBomb = bomb;
        isClicked = false;
        isDisabled = false;
        image.color = Color.gray;

        if (skullIcon != null)
            skullIcon.gameObject.SetActive(false);
    }

    public void SetInteractable(bool value) {
        if (isDisabled) return;
        isInteractable = value;
        image.color = isInteractable ? Color.white : Color.gray;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (isClicked || isDisabled || !isInteractable) return;

        isClicked = true;
        isDisabled = true;

        stageManager?.DisableAllCellsOnFloor(stageManager.CurrentFloor);

        Debug.Log($"[클릭] {gameObject.name} / 폭탄 여부: {isBomb}");

        StartCoroutine(DelayedShakeAnimation());
    }

    private IEnumerator DelayedShakeAnimation() {
        yield return new WaitForSecondsRealtime(0.2f);

        image.color = isBomb ? Color.red : Color.green;

        if (isBomb && skullIcon != null)
            skullIcon.gameObject.SetActive(true);

        yield return StartCoroutine(ShakeAnimation());

        if (isBomb) {
            //wasFailed = true;
            Debug.Log($"[실패] {gameObject.name}");

            if (stageManager != null) {
                int failedIndex = transform.GetSiblingIndex();
                stageManager.RevealAllOnGameOver(stageManager.CurrentFloor, failedIndex);
            }

            deathFunPanel?.OnFail();
        } else {
            wasPassed = true;
            Debug.Log($"[통과] {gameObject.name}");

            if (stageManager != null) {
                stageManager.RevealBombs(stageManager.CurrentFloor);
                stageManager.PassFloor();
            }

            deathFunPanel?.OnPassFloor();
        }
    }

    private IEnumerator ShakeAnimation() {
        Vector3 originalPos = rectTransform.localPosition;
        float duration = 0.2f;
        float elapsed = 0f;
        float strength = 5f;

        while (elapsed < duration) {
            float x = Mathf.Sin(elapsed * 50f) * strength;
            rectTransform.localPosition = originalPos + new Vector3(x, 0, 0);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        rectTransform.localPosition = originalPos;
    }


    public void ResetCell() {
        isClicked = false;
        wasPassed = false;
        //wasFailed = false;
        isDisabled = false;
        isInteractable = false;

        image.color = Color.white;

        if (skullIcon != null)
            skullIcon.gameObject.SetActive(false);
    }


    public void OnGameOverReveal(bool isPassedRow, bool isFailedRow, bool isFailedCell) {
        isDisabled = true;
        isInteractable = false;

        if (isFailedCell) {
            image.color = DarkRed;
            if (skullIcon != null) skullIcon.gameObject.SetActive(true);
        } else if (wasPassed) {
            image.color = DarkGreen;
            if (isBomb && skullIcon != null) skullIcon.gameObject.SetActive(true);
        } else if (isFailedRow) {
            image.color = DimGray;
            if (isBomb && skullIcon != null) skullIcon.gameObject.SetActive(true);
        }
    }


}
