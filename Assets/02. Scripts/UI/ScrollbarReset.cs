using UnityEngine;
using UnityEngine.UI;

public class ScrollbarReset : MonoBehaviour
{
    public ScrollRect myScrollRect;

    void OnEnable() // 패널이 켜질 때마다 실행
    {
        // 스크롤 위치를 맨 위(1.0)로 강제 고정
        // 0이면 맨 아래, 1이면 맨 위입니다.
        if (myScrollRect != null) {
            myScrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
