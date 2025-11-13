using System.Collections;
using UnityEngine;

public abstract class Monster : MonoBehaviour {
    protected Vector2 moveDirection = Vector2.one.normalized;
    protected float moveSpeed = 3f;
    protected float hp = 3f;

    protected SpriteRenderer sRenderer;
    protected Animator animator;
    protected SpawnManager spawnManager;

    protected bool isMove = true;
    protected bool isHit = false;

    public virtual void Init(Vector2 direction, float speed) {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        spawnManager = FindFirstObjectByType<SpawnManager>();
        sRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    protected virtual void Update() {
        if (isMove)
            Move();
        CheckBoundsAndFlip();
    }

    protected virtual void Move() {
        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
    }

    protected void CheckBoundsAndFlip() {
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
        bool flipped = false;

        if (viewPos.x < 0f || viewPos.x > 1f) {
            moveDirection.x *= -1;
            flipped = true;
        }
        if (viewPos.y < 0f || viewPos.y > 1f) {
            moveDirection.y *= -1;
            flipped = true;
        }

        if (flipped && sRenderer != null)
            sRenderer.flipX = moveDirection.x < 0;
    }

    // 외부에서 피격 명령 내릴 때 호출
    public void OnHit() {
        if (!isHit)
            StartCoroutine(Hit(2f));
    }

    protected IEnumerator Hit(float damage) {
        if (isHit) yield break;
        isHit = true;
        isMove = false;

        hp -= damage;
        if (animator != null)
            animator.SetTrigger("Hit");

        if (hp <= 0) {
            if (animator != null)
                animator.SetTrigger("Death");

            int coinAmount = GetRandomCoinAmount();
            spawnManager?.DropCoin(transform.position, coinAmount);

            var gm = FindFirstObjectByType<GambleMonster>();
            gm?.AddScore(coinAmount);

            yield return new WaitForSeconds(0.8f);
            Destroy(gameObject);
            yield break;
        }

        yield return new WaitForSeconds(0.5f);
        isMove = true;
        isHit = false;
    }

    private int GetRandomCoinAmount() {
        float rand = Random.Range(0f, 100f);
        if (rand < 60f) return Random.Range(6, 11);
        if (rand < 85f) return Random.Range(11, 16);
        if (rand < 95f) return Random.Range(16, 25);
        if (rand < 96f) return 100;
        return 1;
    }
}
