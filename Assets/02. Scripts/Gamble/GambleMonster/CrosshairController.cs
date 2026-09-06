using UnityEngine;

public class CrosshairController : MonoBehaviour {
    public RectTransform crosshairRect; // 크로스헤어 이미지
    public Canvas canvas;                // Screen Space - Camera 모드의 Canvas
    private Camera uiCamera;             // Canvas가 참조하는 카메라

    void Start() {
        if (canvas != null)
            uiCamera = canvas.worldCamera;
        else
            uiCamera = Camera.main;
    }

    void Update() {
        if (crosshairRect == null || !crosshairRect.gameObject.activeSelf)
            return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            uiCamera,
            out localPoint
        );

        crosshairRect.localPosition = localPoint;
    }

    public void Show() {
        if (crosshairRect != null)
            crosshairRect.gameObject.SetActive(true);
        Cursor.visible = false;
    }

    public void Hide() {
        if (crosshairRect != null)
            crosshairRect.gameObject.SetActive(false);
        Cursor.visible = true;
    }
}
