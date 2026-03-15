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

            if (gainers.Count == 0 && losers.Count == 0) {
                yield return null;
                continue;
            }

            // 1. 주요 상승 (Top Gainers) 표시
            string gainerTitle = LocalizationManager.GetText("LBL_TOP_GAINERS");
            group.SetData(gainerTitle, gainers);

            yield return FadeIn();
            yield return new WaitForSeconds(displayTime);
            yield return FadeOut();

            // 2. 주요 하락 (Top Losers) 표시
            string loserTitle = LocalizationManager.GetText("LBL_TOP_LOSERS");
            group.SetData(loserTitle, losers);

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
