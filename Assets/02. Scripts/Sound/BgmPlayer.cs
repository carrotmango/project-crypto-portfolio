using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class BgmPlayer : MonoBehaviour {
    public static BgmPlayer Instance { get; private set; }

    [Header("BGM 리스트")]
    public List<AudioClip> tradingBgms;
    public AudioClip outingBgm;
    public AudioClip arcadeBgm;

    private AudioSource audioSource;
    private int currentTradingIndex = 0;
    private bool isTradingMode = false;
    private bool isTransitioning = false;

    private float masterVolume = 1.0f;
    private const string BGM_VOLUME_KEY = "BgmMasterVolume"; // PlayerPrefs용 키

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // [추가] 저장된 볼륨 불러오기 (기본값 1.0)
        masterVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1.0f);
        audioSource.volume = masterVolume;
    }

    void Update() {
        if (isTradingMode && !isTransitioning && audioSource.clip != null) {
            if (!audioSource.isPlaying && audioSource.time == 0) {
                Debug.Log($"[BGM] 곡 종료 감지! 다음 트레이딩 곡으로 전환 시도. 현재 인덱스: {currentTradingIndex}");
                PlayNextTradingBgm();
            }
        }
    }

    private bool IsCurrentClipInTradingList() {
        if (audioSource.clip == null) return false;
        return tradingBgms.Contains(audioSource.clip);
    }

    public void PlayTradingBgm() {
        if (isTradingMode && IsCurrentClipInTradingList()) return;

        isTradingMode = true;
        isTransitioning = false;

        if (tradingBgms.Count > 0) {
            CrossFade(tradingBgms[currentTradingIndex], false);
        }
    }

    private void PlayNextTradingBgm() {
        if (tradingBgms.Count <= 1) return;

        isTransitioning = true;
        currentTradingIndex = (currentTradingIndex + 1) % tradingBgms.Count;

        Debug.Log($"[BGM] 다음 곡 결정됨: {currentTradingIndex}번 - {tradingBgms[currentTradingIndex].name}");
        CrossFade(tradingBgms[currentTradingIndex], false);
    }

    public void PlayOutingBgm() {
        isTradingMode = false;
        isTransitioning = false;
        CrossFade(outingBgm, true);
    }

    public void PlayArcadeBgm() {
        isTradingMode = false;
        isTransitioning = false;
        CrossFade(arcadeBgm, true);
    }

    private void CrossFade(AudioClip nextClip, bool loop) {
        if (nextClip == null) return;

        audioSource.DOKill();
        isTransitioning = true;

        audioSource.DOFade(0, 0.5f).SetUpdate(true).OnComplete(() => {
            audioSource.Stop();
            audioSource.clip = nextClip;
            audioSource.loop = isTradingMode ? false : loop;
            audioSource.Play();

            audioSource.DOFade(masterVolume, 0.5f).SetUpdate(true).OnComplete(() => {
                Debug.Log($"[BGM] '{nextClip.name}' 페이드인 완료. 볼륨: {masterVolume}");
                isTransitioning = false;
            });
        });
    }

    public float GetVolume() => masterVolume;

    public void StopBgm() => audioSource.DOFade(0, 0.5f).OnComplete(() => audioSource.Stop());

    public void SetVolume(float value) {
        masterVolume = value;

        // [추가] 볼륨 설정 시 즉시 저장
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, value);
        PlayerPrefs.Save();

        // 페이드 중이 아닐 때만 즉시 볼륨 반영
        if (!DOTween.IsTweening(audioSource)) {
            audioSource.volume = value;
        }
    }

    public void PlayIntroBgm() { }
}