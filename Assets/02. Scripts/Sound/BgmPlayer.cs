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
    public AudioClip lobbyBgm;

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

        // 초기화 시 모든 상태 리셋
        audioSource.playOnAwake = false;
        audioSource.Stop();
        audioSource.clip = null;
        audioSource.volume = 0; // 처음엔 0으로 시작해서 페이드인 되게 함

        isTransitioning = false; // 중요: 이 값이 true면 CrossFade가 안 먹힐 수 있음

        masterVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1.0f);
    }

    void Update() {
        if (isTradingMode && !isTransitioning && audioSource.clip != null) {
            //  곡이 완전히 끝나기 0.2초 전에 다음 곡으로 CrossFade를 시작
            float remainingTime = audioSource.clip.length - audioSource.time;

            if (!audioSource.isPlaying || (remainingTime < 0.2f && !audioSource.loop)) {
                Debug.Log("[BGM] 다음 트레이딩 곡으로 전환");
                PlayNextTradingBgm();
            }
        }
    }

    public void PlayLobbyBgm() {
        if (lobbyBgm == null || (audioSource.clip == lobbyBgm && audioSource.isPlaying)) return;
        isTradingMode = false; // 한 곡 반복 모드
        CrossFade(lobbyBgm, true); // loop = true
    }



    private bool IsCurrentClipInTradingList() {
        if (audioSource.clip == null) return false;
        return tradingBgms.Contains(audioSource.clip);
    }

    public void PlayTradingBgm() {
        // 이미 트레이딩 중이면 중복 실행 방지
        if (isTradingMode && IsCurrentClipInTradingList()) return;

        isTradingMode = true;

        if (tradingBgms.Count > 0) {
            // [수정] 곡이 딱 한 개라면 유니티 loop를 true로, 여러 개라면 false(Update에서 관리)
            bool shouldLoop = (tradingBgms.Count == 1);
            CrossFade(tradingBgms[currentTradingIndex], shouldLoop);
        }
    }

    private void PlayNextTradingBgm() {
        if (tradingBgms.Count == 0) return;

        isTransitioning = true;

        // 다음 인덱스 계산 (1개일 땐 계속 0)
        currentTradingIndex = (currentTradingIndex + 1) % tradingBgms.Count;

        Debug.Log($"[BGM] 전환: {currentTradingIndex}번 - {tradingBgms[currentTradingIndex].name}");
        CrossFade(tradingBgms[currentTradingIndex], (tradingBgms.Count == 1));
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

    private void CrossFade(AudioClip nextClip, bool useLoop) {
        if (nextClip == null) return;

        // 확실하게 기존의 모든 볼륨 트윈을 제거하고 상태 초기화
        audioSource.DOKill();
        isTransitioning = true;

        // 1. 페이드 아웃
        audioSource.DOFade(0, 0.5f).SetUpdate(true).OnComplete(() => {
            audioSource.Stop();
            audioSource.clip = nextClip;
            audioSource.loop = useLoop; // 로비면 true, 트레이딩이면 false
            audioSource.Play();

            // 2. 페이드 인
            audioSource.DOFade(masterVolume, 0.5f).SetUpdate(true).OnComplete(() => {
                isTransitioning = false;
                Debug.Log($"[BGM] 재생 시작: {nextClip.name}, 루프 여부: {audioSource.loop}");
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