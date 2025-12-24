using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MajorChangeRotator : MonoBehaviour {
    public MajorChangeGroupController group;
    public float displayTime = 10f;
    public float fadeDuration = 1.5f;

    IEnumerator Start() {
        yield return new WaitUntil(() => GameBootState.playerReady);
        StartCoroutine(RotateRoutine());
    }


    IEnumerator RotateRoutine() {
        Debug.Log("MajorChangeRotator STARTED");

        while (true) {
            CoinManager.Instance.GetMajorDailyChanges(out var gainers, out var losers);

            Debug.Log($"Gainers: {gainers.Count}, Losers: {losers.Count}");

            if (gainers.Count == 0 && losers.Count == 0) {
                yield return null;
                continue;
            }

            group.SetData("주요 상승", gainers);
            yield return FadeIn();
            yield return new WaitForSeconds(displayTime);

            yield return FadeOut();

            group.SetData("주요 하락", losers);
            yield return FadeIn();
            yield return new WaitForSeconds(displayTime);

            yield return FadeOut();
        }
    }


    IEnumerator FadeIn() {
        float t = 0;
        while (t < fadeDuration) {
            t += Time.deltaTime;
            group.canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }
    }

    IEnumerator FadeOut() {
        float t = 0;
        while (t < fadeDuration) {
            t += Time.deltaTime;
            group.canvasGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }
    }
}
