using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour {
    [Header("Menu Images for Flashing")]
    public List<Image> menuImages; // 반짝이 효과를 줄 이미지들
    public float flashSpeed = 0.5f;

    [Header("UI Panels")]
    public GameObject settingsPanel;
    public GameObject mainMenuPanel;
    public GameObject characterPanel;

    private Dictionary<int, Coroutine> activeFlashRoutines = new Dictionary<int, Coroutine>();
    private List<bool> lockStates = new List<bool>();

    void Awake() {
        if (menuImages != null) {
            for (int i = 0; i < menuImages.Count; i++) lockStates.Add(false);
            if (lockStates.Count > 1) lockStates[1] = true; // 로드게임 기본 잠금
        }
    }

    void Start() {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnEnable() {
        if (menuImages == null || menuImages.Count == 0) return;
        StopAllCoroutines();
        activeFlashRoutines.Clear();
        for (int i = 0; i < menuImages.Count; i++) StartFlashing(i, lockStates[i]);
    }

    // --- 개별 실행 함수 (인스펙터에서 버튼에 직접 연결하세요) ---

    public void OnNewGameClick() {
        Debug.Log("[MainMenu] New Game 버튼 클릭됨!");
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        // 캐릭터 패널을 여기서 직접 켜거나 LobbyManager를 호출
        LobbyManager lobby = FindAnyObjectByType<LobbyManager>();
        if (lobby != null) {
            lobby.OnNewGameClicked();
        } else if (characterPanel != null) {
            characterPanel.SetActive(true);
        }
    }

    public void OnSettingsClick() {
        Debug.Log("[MainMenu] Settings 버튼 클릭됨!");
        if (settingsPanel != null) {
            settingsPanel.SetActive(true);
        } else {
            Debug.LogError("Settings Panel이 인스펙터에 할당되지 않았습니다!");
        }
    }

    public void OnExitClick() {
        Debug.Log("[MainMenu] Exit 버튼 클릭됨!");
        Application.Quit();
    }

    public void CloseSettings() {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // --- 반짝이 로직 (기존 유지) ---

    public void SetMenuLock(int index, bool isLocked) {
        if (index < 0 || index >= menuImages.Count) return;
        lockStates[index] = isLocked;
        if (activeFlashRoutines.ContainsKey(index)) {
            if (activeFlashRoutines[index] != null) StopCoroutine(activeFlashRoutines[index]);
            activeFlashRoutines.Remove(index);
        }
        if (gameObject.activeInHierarchy) StartFlashing(index, isLocked);
    }

    private void StartFlashing(int index, bool isLocked) {
        menuImages[index].raycastTarget = !isLocked;
        activeFlashRoutines[index] = StartCoroutine(FlashRoutine(menuImages[index], isLocked));
    }

    IEnumerator FlashRoutine(Image image, bool isLocked) {
        Color origin = image.color;
        float bAlpha = isLocked ? 0.4f : origin.a;
        float dAlpha = isLocked ? 0.15f : origin.a * 0.5f;
        Color bCol = new Color(origin.r, origin.g, origin.b, bAlpha);
        Color dCol = new Color(origin.r, origin.g, origin.b, dAlpha);

        while (true) {
            image.color = dCol;
            yield return new WaitForSecondsRealtime(flashSpeed);
            image.color = bCol;
            yield return new WaitForSecondsRealtime(flashSpeed);
        }
    }
        public void BackToMainMenu() {

        Debug.Log("게임을 완전히 리셋하여 로비로 돌아갑니다.");



        // 로비매니저의 RestartGame 로직과 동일하게 씬을 다시 로드합니다.

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    }
}