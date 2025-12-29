using UnityEngine;

public class SfxPlayer : MonoBehaviour {
    public static SfxPlayer Instance;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private AudioClip loopingClip;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    // 기존 효과음 (원샷)
    public void Play(AudioClip clip) {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    public void PlayLoop(AudioClip clip) {
        if (clip == null || audioSource == null) return;

        // 이미 같은 루프가 재생 중이면 무시
        if (audioSource.isPlaying && loopingClip == clip) return;

        loopingClip = clip;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }
    public void StopLoop() {
        if (audioSource == null) return;

        audioSource.Stop();
        audioSource.loop = false;
        audioSource.clip = null;
        loopingClip = null;
    }

    // 볼륨 제어 (슬라이더 연동)
    public void SetVolume(float volume) {
        if (audioSource == null) return;
        audioSource.volume = volume;
    }

    public float GetVolume() {
        return audioSource != null ? audioSource.volume : 1f;
    }
}
