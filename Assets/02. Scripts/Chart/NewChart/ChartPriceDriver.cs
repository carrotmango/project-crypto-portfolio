using UnityEngine;
public class ChartPriceDriver : MonoBehaviour {
    [Header("Follow Settings")]
    [Tooltip("목표 가격을 따라가는 기본 속도")]
    public float followSpeed = 2.5f;

    [Tooltip("가격이 목표에 가까워질수록 감쇠되는 계수")]
    public float damping = 0.85f;

    [Header("Shake Settings")]
    [Tooltip("기본 흔들림 강도")]
    public float baseShake = 0.002f; // 비율 (0.2%)

    [Tooltip("급락/급등 시 흔들림 배수")]
    public float eventShakeMultiplier = 3f;

    [Header("State (Read Only)")]
    public double targetPrice;   // CoinManager에서 받는 실제 가격
    public double displayPrice;  // 차트가 실제로 그리는 가격

    private double velocity;     // 가격 이동 속도 (관성)
    private float noiseTimer;

    public bool IsFrozen { get; private set; }

    public void Freeze() {
        IsFrozen = true;
        velocity = 0;
    }


    public void Resume() {
        IsFrozen = false;
        noiseTimer = Random.Range(0f, 100f);
    }



    public void Initialize(double startPrice) {
        targetPrice = startPrice;
        displayPrice = startPrice;
        velocity = 0;
        noiseTimer = Random.Range(0f, 100f);
    }

    public void SetTargetPrice(double price) {
        targetPrice = price;
    }

    public void Tick(float deltaTime, float volatilityMultiplier = 1f) {
        if (IsFrozen) return;
        if (targetPrice <= 0) return;
        if (deltaTime <= 0f) return;

        double diff = targetPrice - displayPrice;

        velocity += diff * followSpeed * deltaTime;
        velocity *= damping;

        noiseTimer += deltaTime * 2f;

        double noise =
            Mathf.PerlinNoise(noiseTimer, 0f) - 0.5f;

        double shakeAmount =
            targetPrice * baseShake * volatilityMultiplier;

        double directionalShake =
            noise * shakeAmount * deltaTime;

        displayPrice += velocity + directionalShake;

        if (diff > 0 && displayPrice > targetPrice)
            displayPrice = targetPrice;
        else if (diff < 0 && displayPrice < targetPrice)
            displayPrice = targetPrice;
    }
}
