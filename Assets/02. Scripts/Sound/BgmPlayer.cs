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
    private bool isTransitioning = false; // [추가] 전환 중인지 체크

    private float masterVolume = 1.0f;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        masterVolume = audioSource.volume;
    }

    void Update() {
        // [수정] 재생 중이 아니고, 전환 중도 아니며, 클립이 할당되어 있을 때만 다음 곡으로!
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
        // [수정] 이미 트레이딩 곡이 재생 중이라면 인덱스를 0으로 바꾸지 말고 그냥 리턴!
        if (isTradingMode && IsCurrentClipInTradingList()) return;

        isTradingMode = true;
        isTransitioning = false;

        // [수정] 여기서 currentTradingIndex = 0; 을 지워야 합니다.
        // 대신 현재 인덱스에 맞는 곡을 틀어줍니다.
        if (tradingBgms.Count > 0) {
            CrossFade(tradingBgms[currentTradingIndex], false);
        }
    }

    private void PlayNextTradingBgm() {
        if (tradingBgms.Count <= 1) return; // 곡이 1개 이하면 넘길 필요 없음

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

            // [핵심] Play() 직후 아주 잠깐 기다렸다가 감지 로직을 풀어줘야 안전합니다.
            audioSource.DOFade(masterVolume, 0.5f).SetUpdate(true).OnComplete(() => {
                Debug.Log($"[BGM] '{nextClip.name}' 페이드인 완료. 이제 다음 종료를 감지합니다.");
                isTransitioning = false;
            });
        });
    }

    public float GetVolume() => masterVolume;

    public void StopBgm() => audioSource.DOFade(0, 0.5f).OnComplete(() => audioSource.Stop());

    public void SetVolume(float value) {
        masterVolume = value;
        if (!DOTween.IsTweening(audioSource)) {
            audioSource.volume = value;
        }
    }

    public void PlayIntroBgm() { }
}