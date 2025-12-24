using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BoxView : MonoBehaviour {
    public Image image;

    [Header("Type1 Designs")]
    public Sprite[] type1Sprites;

    [Header("Type2 Designs")]
    public Sprite[] type2Sprites;


    public BoxType Type { get; private set; }


    public void SetBox(BoxType type) {
        Type = type;

        switch (type) {
            case BoxType.Type1:
                image.sprite = type1Sprites[Random.Range(0, type1Sprites.Length)];
                break;
            case BoxType.Type2:
                image.sprite = type2Sprites[Random.Range(0, type2Sprites.Length)];
                break;
        }
    }

    public Sprite GetCurrentSprite() {
        return image.sprite;
    }

    public void PlayWrongLeft() {
        StopAllCoroutines();
        StartCoroutine(Shake(Vector3.left));
    }

    public void PlayWrongRight() {
        StopAllCoroutines();
        StartCoroutine(Shake(Vector3.right));
    }

    IEnumerator Shake(Vector3 dir) {
        Vector3 originPos = transform.localPosition;
        Quaternion originRot = transform.localRotation;

        float duration = 0.1f;
        float elapsed = 0f;

        float moveAmount = 8f;
        float rotateAmount = 8f; // 도리도리 각도 (Z축)

        // 1 밀면서 회전
        while (elapsed < duration) {
            transform.localPosition = originPos + dir * moveAmount;
            transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                dir.x * rotateAmount
            );

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;

        // 원위치 + 원각도 복귀
        while (elapsed < duration) {
            transform.localPosition = originPos;
            transform.localRotation = originRot;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = originPos;
        transform.localRotation = originRot;
    }

}

