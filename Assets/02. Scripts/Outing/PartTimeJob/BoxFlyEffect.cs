using UnityEngine;
using UnityEngine.UI;

public class BoxFlyEffect : MonoBehaviour {
    public Image image;

    private Animator animator;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    public void SetSprite(Sprite sprite) {
        image.sprite = sprite;
    }

    public void PlayLeft() {
        animator.SetTrigger("FlyLeft");
    }

    public void PlayRight() {
        animator.SetTrigger("FlyRight");
    }

    // Animation Event
    public void OnAnimationEnd() {
        Destroy(gameObject);
    }
}
