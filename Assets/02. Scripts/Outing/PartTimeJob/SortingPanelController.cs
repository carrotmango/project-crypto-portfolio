using UnityEngine;
using TMPro;

public class SortingPanelController : MonoBehaviour {
    [Header("Slots (Box0 ~ Box5)")]
    public Transform[] slots;

    [Header("Box")]
    public GameObject boxPrefab;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;

    [Header("Effect")]
    [SerializeField] private Transform exitSlot;
    [SerializeField] private GameObject flyingBoxPrefab;

    [Header("SFX")]
    [SerializeField] private AudioClip hitSuccessClip;
    [SerializeField] private AudioClip hitFailClip;

    private int score = 0;
    private bool inputEnabled = false;

    private void OnEnable() {
        ResetGame();
        EnableInput(false);
    }

    private void Update() {
        if (!inputEnabled) return;

        if (Input.GetKeyDown(KeyCode.A))
            TryHit(BoxType.Type1);

        if (Input.GetKeyDown(KeyCode.D))
            TryHit(BoxType.Type2);
    }

    /* =========================
     * 외부 제어용 (Manager 호출)
     * ========================= */

    public void ResetGame() {
        score = 0;
        UpdateScore();
        ClearBoxes();
        SpawnInitialBoxes();
    }

    public void UpdateTimer(float timeLeft) {
        if (timerText == null) return;
        timerText.text = $"time: {Mathf.CeilToInt(timeLeft)}s";
    }


    public void EnableInput(bool enable) {
        inputEnabled = enable;
    }

    public void ShowCountdown(string text) {
        if (countdownText == null) return;
        countdownText.gameObject.SetActive(true);
        countdownText.text = text;
    }

    public void HideCountdown() {
        if (countdownText == null) return;
        countdownText.gameObject.SetActive(false);
    }

    /* =========================
     * 게임 로직
     * ========================= */

    private void TryHit(BoxType inputType) {
        Transform hitSlot = slots[slots.Length - 1];
        if (hitSlot.childCount == 0) return;

        BoxView box = hitSlot.GetChild(0).GetComponent<BoxView>();
        bool correct = box.Type == inputType;

        if (correct) {
            score += 10;
            PlaySfx(true);

            SpawnFlyingBox(box, inputType);

            Destroy(box.gameObject);
            PullBoxesForward();
        } else {
            score -= 20;
            PlaySfx(false);

            if (inputType == BoxType.Type1)
                box.PlayWrongLeft();
            else
                box.PlayWrongRight();
        }

        if (score < 0) score = 0;
        UpdateScore();

        SortGameManager.Instance.AddScore(correct ? 10 : -20);

    }

    /* =========================
     * 박스 처리
     * ========================= */

    private void SpawnInitialBoxes() {
        for (int i = 0; i < slots.Length; i++) {
            SpawnBox(slots[i]);
        }
    }

    private void ClearBoxes() {
        foreach (Transform slot in slots) {
            for (int i = slot.childCount - 1; i >= 0; i--) {
                Destroy(slot.GetChild(i).gameObject);
            }
        }
    }

    private void PullBoxesForward() {
        for (int i = slots.Length - 1; i > 0; i--) {
            if (slots[i - 1].childCount > 0) {
                Transform box = slots[i - 1].GetChild(0);
                box.SetParent(slots[i], false);
            }
        }
        SpawnBox(slots[0]);
    }

    private void SpawnBox(Transform slot) {
        GameObject boxObj = Instantiate(boxPrefab, slot);
        BoxView view = boxObj.GetComponent<BoxView>();

        BoxType type = Random.value < 0.5f
            ? BoxType.Type1
            : BoxType.Type2;

        view.SetBox(type);
    }

    /* =========================
     * 연출 / 사운드
     * ========================= */

    private void SpawnFlyingBox(BoxView sourceBox, BoxType inputType) {
        if (flyingBoxPrefab == null || exitSlot == null) return;

        GameObject fx = Instantiate(flyingBoxPrefab, exitSlot);
        fx.transform.position = sourceBox.transform.position;

        BoxFlyEffect flying = fx.GetComponent<BoxFlyEffect>();
        flying.SetSprite(sourceBox.GetCurrentSprite());

        if (inputType == BoxType.Type1)
            flying.PlayLeft();
        else
            flying.PlayRight();
    }

    private void PlaySfx(bool success) {
        if (SfxPlayer.Instance == null) return;

        if (success)
            SfxPlayer.Instance.Play(hitSuccessClip);
        else
            SfxPlayer.Instance.Play(hitFailClip);
    }

    /* =========================
     * UI
     * ========================= */

    private void UpdateScore() {
        if (scoreText != null)
            scoreText.text = $"Score : {score}";
    }
}
