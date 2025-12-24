using UnityEngine;
using System.Collections;

public class SortGameManager : MonoBehaviour {
    public static SortGameManager Instance;

    [SerializeField] private GameObject sortingPanel;
    [SerializeField] private SortingPanelController panelController;

    private int score;
    private bool isPlaying;

    private float gameDuration = 30f;
    private float timeLeft;


    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public void StartGame() {
        if (isPlaying) return;

        Time.timeScale = 1f;
        score = 0;
        isPlaying = true;

        sortingPanel.SetActive(true);
        panelController.ResetGame();

        StartCoroutine(GameRoutine());
    }

    private IEnumerator GameRoutine() {
        yield return Countdown();

        panelController.EnableInput(true);

        timeLeft = gameDuration;

        while (timeLeft > 0f) {
            timeLeft -= Time.unscaledDeltaTime;
            panelController.UpdateTimer(timeLeft);
            yield return null;
        }

        EndGame();
    }


    private IEnumerator Countdown() {
        panelController.ShowCountdown("3");
        yield return new WaitForSecondsRealtime(1f);
        panelController.ShowCountdown("2");
        yield return new WaitForSecondsRealtime(1f);
        panelController.ShowCountdown("1");
        yield return new WaitForSecondsRealtime(1f);
        panelController.ShowCountdown("Start!");
        yield return new WaitForSecondsRealtime(0.5f);
        panelController.HideCountdown();

        panelController.EnableInput(true);
    }

    public void AddScore(int value) {
        if (!isPlaying) return;
        score += value;
    }
    private void EndGame() {
        isPlaying = false;
        panelController.EnableInput(false);
        panelController.UpdateTimer(0);

        sortingPanel.SetActive(false);
        PartTimeJobController.Instance.OnSortWorkFinished(score);
    }

}
