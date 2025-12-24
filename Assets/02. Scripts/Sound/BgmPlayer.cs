using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BgmPlayer : MonoBehaviour {
    public static BgmPlayer Instance { get; private set; }

    public AudioClip introBgm;

    private AudioSource audioSource;

    void Awake() {
        // ΩÃ±€≈Ê º≥¡§
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    public void PlayIntroBgm() {
        if (introBgm == null) return;

        audioSource.clip = introBgm;
        audioSource.Play();
    }

    public void StopBgm() {
        if (audioSource.isPlaying)
            audioSource.Stop();
    }

    public void SetVolume(float value) {
        audioSource.volume = value;
    }

    public float GetVolume() {
        return audioSource.volume;
    }
}
