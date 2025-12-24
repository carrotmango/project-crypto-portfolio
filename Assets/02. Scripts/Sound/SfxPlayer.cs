using UnityEngine;

public class SfxPlayer : MonoBehaviour {
    public static SfxPlayer Instance;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public void Play(AudioClip clip) {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    public void SetVolume(float volume) {
        if (audioSource == null) return;
        audioSource.volume = volume;
    }

    public float GetVolume() {
        return audioSource != null ? audioSource.volume : 1f;
    }
}
