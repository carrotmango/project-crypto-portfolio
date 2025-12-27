using System.Collections.Generic;
using UnityEngine;

public class FearIndexManager : MonoBehaviour {
    public static FearIndexManager Instance;

    [Range(0, 100)]
    [SerializeField] private int fearIndex = 50;

    private CoinManager coinManager;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start() {
        coinManager = CoinManager.Instance;
    }

    public int GetFearIndex() => fearIndex;

    public string GetFearLabel() {
        if (fearIndex <= 20) return "¸Å¿ì °øÆ÷";
        if (fearIndex <= 40) return "°øÆ÷";
        if (fearIndex <= 60) return "Áß¸³";
        if (fearIndex <= 80) return "Å½¿å";
        return "¸Å¿ì Å½¿å";
    }

    // ÇÏ·ç 1È¸ (Àü³¯ Àå ¹æÇâ¼º ±âÁØ)
    public void RecalculateDailyFear() {
        List<CoinData> coins = coinManager.coins;
        if (coins == null || coins.Count == 0) return;

        int up = 0;
        int down = 0;

        foreach (var coin in coins) {
            if (coin.InitialPrice <= 0) continue;

            if (coin.CurrentPrice > coin.InitialPrice)
                up++;
            else if (coin.CurrentPrice < coin.InitialPrice)
                down++;
        }

        int total = up + down;
        if (total == 0) {
            fearIndex = Random.Range(45, 56); // ¿ÏÀü È¾º¸ ¡æ Áß¸³
            return;
        }

        float upRatio = (float)up / total;
        float downRatio = (float)down / total;

        fearIndex = ConvertRatioToFear(upRatio, downRatio);
    }

    private int ConvertRatioToFear(float upRatio, float downRatio) {

        // ¸Å¿ì Å½¿å
        if (upRatio >= 0.70f)
            return Random.Range(81, 100);

        // Å½¿å
        if (upRatio >= 0.55f)
            return Random.Range(65, 81);

        // ¸Å¿ì °øÆ÷
        if (downRatio >= 0.70f)
            return Random.Range(1, 21);

        // °øÆ÷
        if (downRatio >= 0.55f)
            return Random.Range(21, 41);

        // Áß¸³
        return Random.Range(45, 56);
    }
}
