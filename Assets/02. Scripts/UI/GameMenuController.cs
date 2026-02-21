using DG.Tweening;
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
    //public BgmPlayer bgmPlayer;
    public SfxPlayer sfxPlayer;



    void Start() {
        menuPanel.SetActive(false);

        hamburgerButton.onClick.AddListener(OpenMenu);
        if (closeButton != null) {
            closeButton.onClick.AddListener(CloseMenu);
        }

        saveButton.onClick.AddListener(OnClickSave);
        exitButton.onClick.AddListener(OnClickExit);

        if (BgmPlayer.Instance != null && bgmSlider != null) {
            // [수정] BgmPlayer에 GetVolume()이 없다면 audioSource에 직접 접근하거나 
            // 아까 제가 제안드린 대로 BgmPlayer에 GetVolume을 추가했다면 그대로 유지
            bgmSlider.value = BgmPlayer.Instance.GetVolume();
            bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        if (sfxPlayer != null && sfxSlider != null) {
            sfxSlider.value = sfxPlayer.GetVolume();
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
    }


    private void OnBgmVolumeChanged(float value) {
        if (BgmPlayer.Instance != null) {
            BgmPlayer.Instance.SetVolume(value);
        }
    }

    private void OnSfxVolumeChanged(float value) {
        if (sfxPlayer != null) {
            sfxPlayer.SetVolume(value);
        }
    }



    private void OpenMenu() {
        menuPanel.SetActive(true);
        menuPanel.transform.localScale = Vector3.one * 0.5f;
        menuPanel.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);

        if (BgmPlayer.Instance != null && bgmSlider != null) {
            bgmSlider.value = BgmPlayer.Instance.GetVolume();
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
