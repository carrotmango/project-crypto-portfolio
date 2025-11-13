using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GambleMonster : MonoBehaviour {
    [Header("기본 UI / 매니저")]
    public GameObject gambleMonsterPanel;
    public SpawnManager spawnManager;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI gameTimerText;
    private Coroutine timerCoroutine;
    public GambleManager gambleManager;

    [Header("점수 관련")]
    public int score = 0;
    public TextMeshProUGUI scoreText;
    [SerializeField] private Transform coinContainer;

    [Header("사운드 관련")]
    public AudioSource audioSource;
    public AudioClip gunshotClip;
    public AudioClip reloadClip;
    public AudioClip gunEmptyClip;

    [Header("탄약 관련")]
    public int maxAmmo = 7;
    public int currentAmmo;
    public bool isReloading = false;
    public Image[] bulletIcons; // 7개 총알 아이콘 연결

    [Header("몬스터 스폰 관련")]
    public float spawnMinDelay = 0.2f;
    public float spawnMaxDelay = 0.3f;
    public float spawnDuration = 30f;

    [Header("크로스헤어 관련")]
    public CrosshairController crosshairController;

    [Header("결과창 관련")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultDetailText;
    public Button confirmButton;


    void Start() {
        currentAmmo = maxAmmo;
        UpdateAmmoUI();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmResult);
    }

    void Update() {
        // 결과창이 떠 있으면 입력 차단
        if (resultPanel != null && resultPanel.activeSelf)
            return;

        // 재장전
        if (Input.GetKeyDown(KeyCode.R) && !isReloading) {
            StartCoroutine(Reload());
        }

        // 🔫 마우스 왼쪽 클릭 시 한 발 쏘기
        if (Input.GetMouseButtonDown(0)) {
            TryShootOnce();
        }
    }

    // ------------------ 탄약 처리 ------------------
    public bool TryShoot() {
        if (isReloading) return false;
        if (currentAmmo <= 0) {
            Debug.Log("탄약 없음! R을 눌러 재장전하세요.");
            return false;
        }

        currentAmmo--;
        UpdateAmmoUI();
        return true;
    }

    private IEnumerator Reload() {
        isReloading = true;
        Debug.Log("재장전 중...");

        if (reloadClip != null && audioSource != null)
            audioSource.PlayOneShot(reloadClip);

        yield return new WaitForSeconds(1f); // 재장전 시간
        currentAmmo = maxAmmo;
        isReloading = false;
        UpdateAmmoUI();
        Debug.Log("재장전 완료!");
    }

    private void UpdateAmmoUI() {
        if (bulletIcons != null && bulletIcons.Length > 0) {
            for (int i = 0; i < bulletIcons.Length; i++) {
                bulletIcons[i].enabled = i < currentAmmo;
            }
        }
    }

    // ------------------ 게임 시작 ------------------
    public void GambleStart() {
        Time.timeScale = 1f;
        score = 0;
        UpdateScoreUI();

        // 탄약 완전 초기화
        isReloading = false;
        currentAmmo = maxAmmo;
        UpdateAmmoUI();

        gambleMonsterPanel.SetActive(true);
        crosshairController?.Show(); //  게임 시작 시 크로스헤어 표시

        countdownText?.gameObject.SetActive(true);
        scoreText?.gameObject.SetActive(true);
        gameTimerText?.gameObject.SetActive(true);

        countdownText.text = "";
        gameTimerText.text = "Time: 30s";
        scoreText.text = "Score: 0";

        StartCoroutine(CountdownAndSpawnRoutine());
    }



    // ------------------ 카운트다운 + 스폰 ------------------
    private IEnumerator CountdownAndSpawnRoutine() {
        if (countdownText != null) {
            countdownText.gameObject.SetActive(true);

            countdownText.text = "3";
            yield return new WaitForSeconds(1f);
            countdownText.text = "2";
            yield return new WaitForSeconds(1f);
            countdownText.text = "1";
            yield return new WaitForSeconds(1f);
            countdownText.text = "Start!";
            yield return new WaitForSeconds(0.5f);

            countdownText.gameObject.SetActive(false);
        }

        SpawnImmediateWave();

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(GameTimerRoutine());
        StartCoroutine(SpawnRoutine());
    }

    // ------------------ 스폰 루프 ------------------
    private IEnumerator SpawnRoutine() {
        float elapsed = 0f;
        int maxMonsters = 30;

        while (elapsed < spawnDuration) {
            float delay = Random.Range(spawnMinDelay, spawnMaxDelay);
            yield return new WaitForSeconds(delay);

            int currentCount = transform.childCount;
            if (currentCount >= maxMonsters) continue;

            int spawnCount = Random.Range(3, 6);
            int spawnable = Mathf.Min(spawnCount, maxMonsters - currentCount);

            for (int i = 0; i < spawnable; i++) {
                spawnManager.SpawnMonsterInside(this.transform);
            }

            elapsed += delay;
        }
    }

    // ------------------ 즉시 초기 스폰 ------------------
    private void SpawnImmediateWave() {
        int maxMonsters = 30;
        int currentCount = transform.childCount;

        if (currentCount >= maxMonsters) return;

        int spawnCount = Random.Range(3, 6);
        int spawnable = Mathf.Min(spawnCount, maxMonsters - currentCount);

        for (int i = 0; i < spawnable; i++) {
            spawnManager.SpawnMonsterInside(this.transform);
        }
    }

    // ------------------ 점수 관리 ------------------
    public void AddScore(int amount) {
        score += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI() {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    // ------------------ 게임 타이머 ------------------
    private IEnumerator GameTimerRoutine() {
        float timeLeft = spawnDuration; // 기존 30f 대신 spawnDuration 사용
        while (timeLeft > 0f) {
            if (gameTimerText != null)
                gameTimerText.text = $"Time: {Mathf.CeilToInt(timeLeft)}s";

            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }

        EndGame();
    }


    // ------------------ 게임 종료 처리 ------------------
    private void EndGame() {
        StopAllCoroutines(); 
        ClearMonsters();     
        crosshairController?.Hide();

        ShowResultPanel();   // 결과창 띄우기
    }


    // ------------------ 결과창 표시 ------------------
    private void ShowResultPanel() {
        if (resultPanel == null || gambleManager == null) return;

        ClearMonsters();
        resultPanel.SetActive(true);

        double unit = 0;
        if (gambleManager.selectedBetButton == gambleManager.bet1Button)
            unit = 2000;
        else if (gambleManager.selectedBetButton == gambleManager.bet2Button)
            unit = 25000;
        else if (gambleManager.selectedBetButton == gambleManager.bet3Button)
            unit = 300000;

        double reward = score * unit;

        if (resultTitleText != null)
            resultTitleText.text = "게임 결과";

        if (resultDetailText != null)
            resultDetailText.text =
                $"획득한 코인 수: {score}\n" +
                $"1코인당 금액: {unit:N0} 원\n" +
                $"총 획득 금액: {reward:N0} 원";
    }


    public void OnConfirmResult() {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        gambleMonsterPanel.SetActive(false);

        gambleManager.GambleFinish(score, gambleManager.selectedBetButton);

        // 다음 라운드 준비 (이건 나중에)
        isReloading = false;
        currentAmmo = maxAmmo;
        UpdateAmmoUI();

        gameTimerText.text = "";
        score = 0;
        UpdateScoreUI();
    }


    private void ClearMonsters() {
        // 몬스터 제거
        for (int i = transform.childCount - 1; i >= 0; i--) {
            Transform child = transform.GetChild(i);
            if (child.CompareTag("Monster")) {
                Destroy(child.gameObject);
            }
        }

        // 코인 제거
        if (coinContainer != null) {
            for (int i = coinContainer.childCount - 1; i >= 0; i--) {
                Destroy(coinContainer.GetChild(i).gameObject);
            }
        }
    }

    private void TryShootOnce() {
        // 탄약 없음 → 빈 총소리만 재생
        if (currentAmmo <= 0) {
            if (audioSource != null && gunEmptyClip != null)
                audioSource.PlayOneShot(gunEmptyClip);
            Debug.Log("딸깍! 탄약 없음");
            return;
        }

        if (!TryShoot()) return;

        // 총소리 재생
        if (audioSource != null && gunshotClip != null) {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(gunshotClip);
        }

        // Raycast로 명중 판정
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider != null) {
            Monster monster = hit.collider.GetComponent<Monster>();
            if (monster != null) {
                monster.OnHit();
            }
        }
    }

}
