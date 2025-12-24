using UnityEngine;
using UnityEngine.UI;

public class GameMenuController : MonoBehaviour {
    [Header("UI")]
    public GameObject menuPanel;       // 햄버거 메뉴 패널
    public Button hamburgerButton;     // 햄버거 버튼
    public Button closeButton;         // 닫기 버튼(선택)
    public Button saveButton;          // 저장
    public Button exitButton;          // 게임 종료

    [Header("Audio")]
    public Slider bgmSlider;
    public Slider sfxSlider;
    public BgmPlayer bgmPlayer;
    public SfxPlayer sfxPlayer;



    void Start() {
        menuPanel.SetActive(false);

        hamburgerButton.onClick.AddListener(OpenMenu);
        if (closeButton != null) {
            closeButton.onClick.AddListener(CloseMenu);
        }

        saveButton.onClick.AddListener(OnClickSave);
        exitButton.onClick.AddListener(OnClickExit);

        if (bgmPlayer != null && bgmSlider != null) {
            bgmSlider.value = bgmPlayer.GetVolume();
            bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        if (sfxPlayer != null && sfxSlider != null) {
            sfxSlider.value = sfxPlayer.GetVolume();
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
    }


    private void OnBgmVolumeChanged(float value) {
        if (bgmPlayer != null) {
            bgmPlayer.SetVolume(value);
        }
    }

    private void OnSfxVolumeChanged(float value) {
        if (sfxPlayer != null) {
            sfxPlayer.SetVolume(value);
        }
    }



    private void OpenMenu() {
        menuPanel.SetActive(true);

        if (bgmPlayer != null && bgmSlider != null) {
            bgmSlider.value = bgmPlayer.GetVolume();
        }

        if (sfxPlayer != null && sfxSlider != null) {
            sfxSlider.value = sfxPlayer.GetVolume();
        }
    }



    private void CloseMenu() {
        menuPanel.SetActive(false);
    }

    private void OnClickSave() {
        Debug.Log("Save clicked");
        GameSaveController.OnSaveClicked();

        if (UIManager.Instance != null) {
            UIManager.Instance.ShowConfirm("저장되었습니다!");
        }
        // 저장 기능 호출
    }

    private void OnClickExit() {
        Debug.Log("Exit clicked");

        var lobby = FindAnyObjectByType<LobbyManager>();
        if (lobby != null) {
            lobby.RestartGame();
        } else {
            Debug.LogWarning("로비매니저 없음");
        }
    }


    void Update() {
        if (menuPanel.activeSelf) {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                CloseMenu();
            }
        }
    }
}
