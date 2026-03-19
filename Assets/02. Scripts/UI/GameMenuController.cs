using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameMenuController : MonoBehaviour {
    [Header("UI Panels")]
    public GameObject menuPanel;
    public Button hamburgerButton;
    public Button closeButton;

    [Header("Buttons")]
    public Button saveButton;
    public Button exitButton;

    [Header("Audio - Sliders")]
    public Slider lobbyBgmSlider;
    public Slider lobbySfxSlider;
    public Slider ingameBgmSlider;
    public Slider ingameSfxSlider;

    [Header("Audio Percent Texts")]
    public TMP_Text lobbyBgmText;
    public TMP_Text lobbySfxText;
    public TMP_Text ingameBgmText;
    public TMP_Text ingameSfxText;

    [Header("Lobby Only - Resolution & Screen Mode")]
    public TMP_Dropdown resDropdown;
    public TMP_Dropdown screenModeDropdown;

    [Header("Language Settings")]
    public TMP_Dropdown langDropdown;

    public SfxPlayer sfxPlayer;
    private List<Resolution> resolutions = new List<Resolution>();

    void Start() {
        if (menuPanel != null) menuPanel.SetActive(false);

        if (hamburgerButton) hamburgerButton.onClick.AddListener(OpenMenu);
        if (closeButton) closeButton.onClick.AddListener(CloseMenu);
        if (saveButton) saveButton.onClick.AddListener(OnClickSave);
        if (exitButton) exitButton.onClick.AddListener(OnClickExit);

        InitAllSliders();
        InitResolutionAndScreenMode();
        InitLanguage();

        // 1. 화면 모드 리스너
        if (screenModeDropdown != null) {
            screenModeDropdown.onValueChanged.AddListener(SetScreenMode);
        }

        // 2. 해상도 리스너 
        if (resDropdown != null) {
            resDropdown.onValueChanged.AddListener(SetResolution);
        }

        if (langDropdown != null) {
            langDropdown.onValueChanged.AddListener(SetLanguage);
        }
    }

    #region 오디오 실시간 연동 (50% 초기값 포함)
    private void InitAllSliders() {
        // 1. [리스너 제거] 초기화 중에 슬라이더가 매니저 값을 오염시키는 걸 막음
        if (lobbyBgmSlider) lobbyBgmSlider.onValueChanged.RemoveAllListeners();
        if (ingameBgmSlider) ingameBgmSlider.onValueChanged.RemoveAllListeners();
        if (lobbySfxSlider) lobbySfxSlider.onValueChanged.RemoveAllListeners();
        if (ingameSfxSlider) ingameSfxSlider.onValueChanged.RemoveAllListeners();

        // 2. [BGM 값 결정] 저장된 값(PlayerPrefs)을 먼저 찾고, 없으면 0.5f
        float bgmVal = PlayerPrefs.GetFloat("Saved_BGM_Volume", 0.5f);
        if (BgmPlayer.Instance != null) {
            BgmPlayer.Instance.SetVolume(bgmVal);
        }

        // 3. [SFX 값 결정] 저장된 값(PlayerPrefs)을 먼저 찾고, 없으면 0.5f
        float sfxVal = PlayerPrefs.GetFloat("Saved_SFX_Volume", 0.5f);
        if (sfxPlayer != null) {
            sfxPlayer.SetVolume(sfxVal);
        }

        // 4. [슬라이더 및 텍스트 UI 동기화]
        if (lobbyBgmSlider) lobbyBgmSlider.value = bgmVal;
        if (ingameBgmSlider) ingameBgmSlider.value = bgmVal;
        if (lobbySfxSlider) lobbySfxSlider.value = sfxVal;
        if (ingameSfxSlider) ingameSfxSlider.value = sfxVal;

        UpdateBgmUI(bgmVal);
        UpdateSfxUI(sfxVal);

        // 5. [리스너 재등록]
        if (lobbyBgmSlider) lobbyBgmSlider.onValueChanged.AddListener((v) => SyncBgm(v, lobbyBgmSlider));
        if (ingameBgmSlider) ingameBgmSlider.onValueChanged.AddListener((v) => SyncBgm(v, ingameBgmSlider));
        if (lobbySfxSlider) lobbySfxSlider.onValueChanged.AddListener((v) => SyncSfx(v, lobbySfxSlider));
        if (ingameSfxSlider) ingameSfxSlider.onValueChanged.AddListener((v) => SyncSfx(v, ingameSfxSlider));

        Debug.Log($"[AudioInit] 로드 완료 - BGM: {bgmVal * 100}%, SFX: {sfxVal * 100}%");
    }

    // --- 동기화 함수들에도 '저장' 로직 추가 ---
    private void SyncBgm(float value, Slider source) {
        if (BgmPlayer.Instance != null) BgmPlayer.Instance.SetVolume(value);
        UpdateBgmUI(value);

        // 유저가 조절할 때마다 하드에 저장
        PlayerPrefs.SetFloat("Saved_BGM_Volume", value);

        if (source == lobbyBgmSlider && ingameBgmSlider) ingameBgmSlider.SetValueWithoutNotify(value);
        if (source == ingameBgmSlider && lobbyBgmSlider) lobbyBgmSlider.SetValueWithoutNotify(value);
    }

    private void SyncSfx(float value, Slider source) {
        if (sfxPlayer != null) sfxPlayer.SetVolume(value);
        UpdateSfxUI(value);

        // 유저가 조절할 때마다 하드에 저장
        PlayerPrefs.SetFloat("Saved_SFX_Volume", value);

        if (source == lobbySfxSlider && ingameSfxSlider) ingameSfxSlider.SetValueWithoutNotify(value);
        if (source == ingameSfxSlider && lobbySfxSlider) lobbySfxSlider.SetValueWithoutNotify(value);
    }

    private void UpdateBgmUI(float value) {
        string percent = Mathf.RoundToInt(value * 100) + "%";
        if (lobbyBgmText) lobbyBgmText.text = percent;
        if (ingameBgmText) ingameBgmText.text = percent;
    }

    private void UpdateSfxUI(float value) {
        string percent = Mathf.RoundToInt(value * 100) + "%";
        if (lobbySfxText) lobbySfxText.text = percent;
        if (ingameSfxText) ingameSfxText.text = percent;
    }
    #endregion

    #region 해상도 및 화면 모드 (16:9 필터링)
    private void InitResolutionAndScreenMode() {
        if (resDropdown != null) {
            resolutions.Clear();
            resDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResIndex = 0;

            for (int i = 0; i < Screen.resolutions.Length; i++) {
                Resolution res = Screen.resolutions[i];
                float aspect = (float)res.width / res.height;
                if (Mathf.Abs(aspect - (16f / 9f)) < 0.01f && res.width >= 1280) {
                    string option = res.width + " x " + res.height;
                    if (!options.Contains(option)) {
                        options.Add(option);
                        resolutions.Add(res);
                        if (res.width == Screen.width && res.height == Screen.height)
                            currentResIndex = options.Count - 1;
                    }
                }
            }
            resDropdown.AddOptions(options);
            resDropdown.value = currentResIndex;
            resDropdown.RefreshShownValue();
        }

        // [수정됨] 화면 모드 드롭다운 옵션 강제 재설정 (전체 창 모드 제거)
        if (screenModeDropdown != null) {
            screenModeDropdown.ClearOptions();
            List<string> modes = new List<string> { "전체 화면", "창 모드" }; // 2개로 축소
            screenModeDropdown.AddOptions(modes);

            // 현재 화면이 창 모드면 1, 그 외(전체화면 등)면 0으로 설정
            if (Screen.fullScreenMode == FullScreenMode.Windowed) {
                screenModeDropdown.value = 1;
            } else {
                screenModeDropdown.value = 0;
            }

            screenModeDropdown.RefreshShownValue();
        }
    }

    public void SetResolution(int index) {
        if (index < 0 || index >= resolutions.Count) return;
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
    }

    public void SetScreenMode(int index) {
        FullScreenMode targetMode;
        switch (index) {
            case 0:
                targetMode = FullScreenMode.FullScreenWindow; // 전체 화면
                break;
            case 1:
            default:
                targetMode = FullScreenMode.Windowed; // 창 모드
                break;
        }

        // 해상도와 모드를 한 번에 세팅 (가장 안전한 방법)
        int resIndex = resDropdown.value;
        if (resIndex < resolutions.Count) {
            Resolution res = resolutions[resIndex];
            Screen.SetResolution(res.width, res.height, targetMode);
        }

        // UI 레이아웃 즉시 갱신
        Canvas.ForceUpdateCanvases();
    }
    #endregion

    private void OpenMenu() {
        if (menuPanel == null) return;
        menuPanel.SetActive(true);

        // [수정] 강제 0.5f 복구 로직 삭제! 저장된 실제 볼륨값을 그대로 UI에 반영합니다.
        if (BgmPlayer.Instance != null && lobbyBgmSlider != null) {
            float currentBgm = BgmPlayer.Instance.GetVolume();
            lobbyBgmSlider.value = currentBgm; // 0이어도 0 그대로 표시
        }

        if (sfxPlayer != null && lobbySfxSlider != null) {
            float currentSfx = sfxPlayer.GetVolume();
            lobbySfxSlider.value = currentSfx; // 0이어도 0 그대로 표시
        }

        // 도트윈 애니메이션 (기존 유지)
        menuPanel.transform.localScale = Vector3.one * 0.5f;
        menuPanel.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    #region 언어 설정 (기본: 한국어)
    private void InitLanguage() {
        // [추가] 가장 먼저 데이터를 로드해야 합니다!
        LocalizationManager.LoadData();

        if (langDropdown == null) return;

        // 1. 드롭다운 옵션 설정 (인덱스 주의: 0은 English, 1은 한국어)
        langDropdown.ClearOptions();

        // JSON 키값을 활용해서 드롭다운 텍스트도 다국어 처리하고 싶다면 이렇게:
        List<string> options = new List<string> {
            LocalizationManager.GetText("LANG_EN"), // English
            LocalizationManager.GetText("LANG_KR")  // 한국어
        };
        langDropdown.AddOptions(options);

        // 2. 저장된 언어 불러오기 (값이 없으면 1번 '한국어'를 기본값으로 사용)
        int savedLang = PlayerPrefs.GetInt("Saved_Language", 1);

        // 3. UI 업데이트 (이벤트 발생 없이 값만 세팅)
        langDropdown.SetValueWithoutNotify(savedLang);
        langDropdown.RefreshShownValue();

        // 4. 실제 게임 내 언어 적용 (여기서 씬의 모든 텍스트가 바뀜)
        ApplyLanguage(savedLang);

        Debug.Log($"[Language] 초기화 및 데이터 로드 완료: {(savedLang == 1 ? "한국어" : "English")}");
    }

    public void SetLanguage(int index) {
        PlayerPrefs.SetInt("Saved_Language", index);
        PlayerPrefs.Save();

        ApplyLanguage(index);
    }

    private void ApplyLanguage(int index) {
        // 1. UI 언어 변경
        LocalizationManager.SetLanguage(index);

        // 2. 드롭다운 글자 갱신
        UpdateDropdownOptions();

        // 3. 커뮤니티 데이터 재로드
        if (CommunityDataLoader.Instance != null) {
            CommunityDataLoader.Instance.LoadData();
        }

        // 4. [핵심 추가] UI & 정식 뉴스 데이터 재로드 (UI.json / UI_En.json)
        NewsRepository newsRepo = FindAnyObjectByType<NewsRepository>();
        if (newsRepo != null) {
            newsRepo.LoadData();
        }

        // 5. [핵심 추가] 트위터(Xbird) 뉴스 데이터 재로드 (News.json / News_En.json)
        XPostRepository xpostRepo = FindAnyObjectByType<XPostRepository>();
        if (xpostRepo != null) {
            xpostRepo.Load();
        }


        RandomEventManager randomEvtMgr = FindAnyObjectByType<RandomEventManager>();
        if (randomEvtMgr != null) {
            randomEvtMgr.LoadEvents();
        }

        if (CoinManager.Instance != null && CoinManager.Instance.coins != null) {
            foreach (var coin in CoinManager.Instance.coins) {
                // Meta 데이터베이스에서 번역된 최신 이름을 찾아서
                var meta = System.Array.Find(CoinMetaDatabase.AllCoins, m => m.Symbol == coin.Symbol);
                if (meta != null) {
                    // 코인 객체의 이름을 강제로 덮어씌움!
                    coin.Name = meta.Name;
                }
            }
        }
    }

    // 드롭다운 글자 자체도 언어 바꿀 때마다 갱신해주기
    private void UpdateDropdownOptions() {
        if (langDropdown != null && langDropdown.options.Count >= 2) {
            langDropdown.options[0].text = LocalizationManager.GetText("LANG_EN");
            langDropdown.options[1].text = LocalizationManager.GetText("LANG_KR");
            langDropdown.RefreshShownValue();
        }

        // [수정됨] 옵션이 2개(전체화면, 창모드)일 때만 작동하도록 변경
        if (screenModeDropdown != null && screenModeDropdown.options.Count >= 2) {
            screenModeDropdown.options[0].text = LocalizationManager.GetText("OPT_FULLSCREEN"); // 전체 화면
            screenModeDropdown.options[1].text = LocalizationManager.GetText("OPT_WINDOWED");   // 창 모드
            // (OPT_BORDERLESS 삭제)
            screenModeDropdown.RefreshShownValue();
        }
    }
    #endregion

    private void CloseMenu() => menuPanel.SetActive(false);
    private void OnClickSave() => GameSaveController.OnSaveClicked();
    private void OnClickExit() { if (FindAnyObjectByType<LobbyManager>()) FindAnyObjectByType<LobbyManager>().RestartGame(); }
}