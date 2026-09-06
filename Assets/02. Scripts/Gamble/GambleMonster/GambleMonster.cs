using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GambleMonster : MonoBehaviour {

    // 패널 / 매니저
    public GameObject gambleMonsterPanel;
    public GambleManager_renewal gambleManager;
    public SpawnManager spawnManager;

    // UI
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI gameTimerText;
    public TextMeshProUGUI scoreText;

    // 결과창
    public GameObject resultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultDetailText;
    public Button confirmButton;

    // 점수
    private int score = 0;

    // 타이머
    private Coroutine timerCoroutine;
    public float spawnDuration = 30f;

    // 탄약
    public int maxAmmo = 7;
    private int currentAmmo;
    private bool isReloading = false;
    public Image[] bulletIcons;

    // 사운드
    public AudioSource audioSource;
    public AudioClip gunshotClip;
    public AudioClip reloadClip;
    public AudioClip gunEmptyClip;

    // 크로스헤어
    public CrosshairController crosshairController;

    // 체크
    private bool isGameRunning = false;


    private void Start() {
        currentAmmo = maxAmmo;
        UpdateAmmoUI();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmResult);
    }

    private void Update() {
        if (resultPanel != null && resultPanel.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.R) && !isReloading) {
            StartCoroutine(Reload());
        }

        if (Input.GetMouseButtonDown(0)) {
            TryShootOnce();
        }
    }

    // =========================
    // 게임 시작
    // =========================
    public void GambleStart() {
        Time.timeScale = 1f;
        isGameRunning = true;

        score = 0;
        UpdateScoreUI();

        isReloading = false;
        currentAmmo = maxAmmo;
        UpdateAmmoUI();

        gambleMonsterPanel.SetActive(true);
        resultPanel.SetActive(false);

        crosshairController?.Show();

        countdownText.gameObject.SetActive(true);
        scoreText.gameObject.SetActive(true);
        gameTimerText.gameObject.SetActive(true);

        countdownText.text = "";

        // [수정] 타이머, 스코어 현지화
        gameTimerText.text = string.Format(LocalizationManager.GetText("LBL_GAMBLE_TIME"), spawnDuration);
        scoreText.text = string.Format(LocalizationManager.GetText("LBL_GAMBLE_SCORE"), 0);

        StartCoroutine(CountdownAndStart());
    }

    private IEnumerator CountdownAndStart() {
        countdownText.text = "3";
        yield return new WaitForSeconds(1f);
        countdownText.text = "2";
        yield return new WaitForSeconds(1f);
        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        // [수정] Start 텍스트 현지화
        countdownText.text = LocalizationManager.GetText("LBL_GAMBLE_START");
        yield return new WaitForSeconds(0.5f);

        countdownText.gameObject.SetActive(false);

        SpawnImmediateWave();

        timerCoroutine = StartCoroutine(GameTimerRoutine());
        StartCoroutine(SpawnRoutine());
    }

    // =========================
    // 스폰
    // =========================
    private IEnumerator SpawnRoutine() {
        float elapsed = 0f;
        int maxMonsters = 30;

        while (elapsed < spawnDuration && isGameRunning) {
            float delay = Random.Range(0.2f, 0.3f);
            yield return new WaitForSeconds(delay);

            int currentCount = transform.childCount;
            if (currentCount >= maxMonsters) continue;

            int spawnCount = Random.Range(3, 6);
            int spawnable = Mathf.Min(spawnCount, maxMonsters - currentCount);

            for (int i = 0; i < spawnable; i++) {
                spawnManager.SpawnMonsterInside(transform);
            }

            elapsed += delay;
        }
    }

    private void SpawnImmediateWave() {
        int spawnCount = Random.Range(3, 6);
        for (int i = 0; i < spawnCount; i++) {
            spawnManager.SpawnMonsterInside(transform);
        }
    }

    // =========================
    // 점수
    // =========================
    public void AddScore(int amount) {
        score += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI() {
        if (scoreText != null) {
            // [수정] 스코어 현지화
            scoreText.text = string.Format(LocalizationManager.GetText("LBL_GAMBLE_SCORE"), score);
        }
    }

    // =========================
    // 타이머
    // =========================
    private IEnumerator GameTimerRoutine() {
        float timeLeft = spawnDuration;

        while (timeLeft > 0f) {
            if (gameTimerText != null) {
                // [수정] 타이머 현지화
                gameTimerText.text = string.Format(LocalizationManager.GetText("LBL_GAMBLE_TIME"), Mathf.CeilToInt(timeLeft));
            }

            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }

        EndGame();
    }

    // =========================
    // 종료
    // =========================
    private void EndGame() {
        isGameRunning = false;

        timerCoroutine = null;

        ClearMonsters();
        crosshairController?.Hide();

        Time.timeScale = 0f;

        ShowResultPanel();
    }
    private void ShowResultPanel() {
        if (resultPanel == null || gambleManager == null) return;

        resultPanel.SetActive(true);

        long bet = gambleManager.currentBetAmount;
        long unitRate = gambleManager.GetGambleMonsterUnit(bet);
        long reward = score * unitRate;

        // [수정] 결과창 타이틀 현지화
        if (resultTitleText != null) {
            resultTitleText.text = LocalizationManager.GetText("LBL_RESULT_TITLE");
        }

        // [수정] 결과창 세부 내용 현지화
        if (resultDetailText != null) {
            string unit = LocalizationManager.GetText("UNIT_CURRENCY");
            resultDetailText.text = string.Format(LocalizationManager.GetText("LBL_RESULT_MONSTER_DETAIL"), score, reward.ToString("N0"), unit);
        }
    }


    public void OnConfirmResult() {
        Time.timeScale = 1f;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (gambleMonsterPanel != null)
            gambleMonsterPanel.SetActive(false);

        if (gambleManager != null) {
            gambleManager.GambleFinish(score);
            gambleManager.OnReturnFromGame();
            gambleManager.turnOnDim();
        }

        score = 0;
        UpdateScoreUI();
    }


    // =========================
    // 탄약
    // =========================
    private IEnumerator Reload() {
        isReloading = true;

        if (audioSource != null && reloadClip != null)
            if (reloadClip != null) SfxPlayer.Instance.Play(reloadClip);

        yield return new WaitForSeconds(1f);

        currentAmmo = maxAmmo;
        isReloading = false;
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI() {
        for (int i = 0; i < bulletIcons.Length; i++) {
            if (bulletIcons[i] == null) return;
            bulletIcons[i].enabled = i < currentAmmo;
        }
    }


    private void TryShootOnce() {
        if (currentAmmo <= 0) {
            if (audioSource != null && gunEmptyClip != null)
                if (gunEmptyClip != null) SfxPlayer.Instance.Play(gunEmptyClip);
            return;
        }

        if (isReloading) return;

        currentAmmo--;
        UpdateAmmoUI();

        if (audioSource != null && gunshotClip != null)
            if (gunshotClip != null) SfxPlayer.Instance.Play(gunshotClip);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider != null) {
            Monster monster = hit.collider.GetComponent<Monster>();
            if (monster != null) {
                monster.OnHit();
            }
        }
    }

    private void ClearMonsters() {
        for (int i = transform.childCount - 1; i >= 0; i--) {
            Transform child = transform.GetChild(i);

            if (!child.CompareTag("Monster")) continue;

            Destroy(child.gameObject);
        }
    }
}
