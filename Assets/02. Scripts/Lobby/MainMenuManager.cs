using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems; // 클릭 감지용
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour {
    public List<TextMeshProUGUI> menuTexts;
    public float flashSpeed = 0.5f;

    [Header("UI Panels")]
    public GameObject settingsPanel;
    public GameObject mainMenuPanel;    // 추가: 메인 메뉴의 전체 부모 패널
    public GameObject characterPanel;   // 추가: 캐릭터 선택 패널

    private Dictionary<int, Coroutine> activeFlashRoutines = new Dictionary<int, Coroutine>();
    // 추가: 각 메뉴의 잠금 상태를 저장 (돌아왔을 때 복구하기 위함)
    private List<bool> lockStates = new List<bool>();

    void Awake() {
        // 메뉴 개수만큼 상태 리스트 초기화
        for (int i = 0; i < menuTexts.Count; i++) {
            lockStates.Add(false);
        }
        lockStates[1] = true; // 로드게임은 처음에 잠금
    }

    void Start() {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Start 대신 OnEnable에서 실행되므로 여기서는 생략 가능하지만, 
        // 초기 클릭 이벤트 등록을 위해 둡니다.
        for (int i = 0; i < menuTexts.Count; i++) {
            AddClickEvent(i);
        }
    }

    // 패널이 SetActive(true) 될 때마다 자동으로 실행됨
    private void OnEnable() {
        if (menuTexts == null || menuTexts.Count == 0) return;

        // 기존 루틴 정리
        StopAllCoroutines();
        activeFlashRoutines.Clear();

        // 저장된 상태대로 다시 깜빡임 시작
        for (int i = 0; i < menuTexts.Count; i++) {
            StartFlashing(i, lockStates[i]);
        }
    }

    public void BackToMainMenu() {
        Debug.Log("게임을 완전히 리셋하여 로비로 돌아갑니다.");

        // 로비매니저의 RestartGame 로직과 동일하게 씬을 다시 로드합니다.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }



    // 텍스트 클릭 시 실행
    public void OnMenuClick(int index) {
        if (menuTexts[index].raycastTarget == false) return;

        switch (index) {
            case 0: // NEW GAME
                    // 메인 메뉴를 끄고 캐릭터 패널이 열리도록 처리 (LobbyManager에서 열겠지만 여기서 꺼주는 게 확실함)
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

                LobbyManager lobby = FindAnyObjectByType<LobbyManager>();
                if (lobby != null) {
                    lobby.OnNewGameClicked();
                }
                break;
            case 2: // SETTINGS (설정 버튼 인덱스에 맞게 수정하세요)
                if (settingsPanel != null) settingsPanel.SetActive(true);
                break;
            case 4: // EXIT
                Application.Quit();
                break;
        }
    }

    // --- 닫기 버튼용 함수 (인스펙터 연결용) ---
    public void CloseSettings() {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // --- 이하 기존 반짝이 로직 동일 ---
    public void SetMenuLock(int index, bool isLocked) {
        if (index < 0 || index >= menuTexts.Count) return;

        lockStates[index] = isLocked; // 상태 저장

        if (activeFlashRoutines.ContainsKey(index)) {
            if (activeFlashRoutines[index] != null)
                StopCoroutine(activeFlashRoutines[index]);
            activeFlashRoutines.Remove(index);
        }

        // 이 스크립트가 활성화 상태일 때만 루틴 시작
        if (gameObject.activeInHierarchy) {
            StartFlashing(index, isLocked);
        }
    }

    private void StartFlashing(int index, bool isLocked) {
        menuTexts[index].raycastTarget = !isLocked;
        // WaitForSecondsRealtime 덕분에 TimeScale 0에서도 작동함
        activeFlashRoutines[index] = StartCoroutine(FlashRoutine(menuTexts[index], isLocked));
    }

    IEnumerator FlashRoutine(TextMeshProUGUI text, bool isLocked) {
        Color bright = isLocked ? new Color(0.4f, 0.4f, 0.4f, 1f) : Color.white;
        Color dimmed = isLocked ? new Color(0.15f, 0.15f, 0.15f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);

        while (true) {
            text.color = dimmed;
            yield return new WaitForSecondsRealtime(flashSpeed);
            text.color = bright;
            yield return new WaitForSecondsRealtime(flashSpeed);
        }
    }

    private void AddClickEvent(int index) {
        GameObject obj = menuTexts[index].gameObject;
        EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { OnMenuClick(index); });
        trigger.triggers.Add(entry);
    }
}