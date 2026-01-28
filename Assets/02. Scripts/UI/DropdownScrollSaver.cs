using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(TMP_Dropdown))]
public class DropdownScrollSaver : MonoBehaviour, IPointerClickHandler {
    // 마지막 스크롤 위치 저장 (1.0 = 맨 위, 0.0 = 맨 아래)
    private float lastScrollPosition = 1.0f;
    private TMP_Dropdown dropdown;

    void Awake() {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    // 유저가 드롭다운을 클릭했을 때 발동
    public void OnPointerClick(PointerEventData eventData) {
        // 드롭다운 리스트가 생성될 시간을 벌기 위해 1프레임 대기
        StartCoroutine(RestoreScrollPosition());
    }

    IEnumerator RestoreScrollPosition() {
        // UI가 생성되는 프레임 대기
        yield return null;

        // 1. 씬에 생성된 "Dropdown List" 오브젝트를 찾음 (TMP 기본 이름)
        // (드롭다운이 열리면 자동으로 이 이름으로 생성됨)
        Transform dropdownList = null;

        // 캔버스 전체에서 찾거나, 최상위에서 찾음 (가장 확실한 방법)
        GameObject foundObj = GameObject.Find("Dropdown List");

        if (foundObj != null) {
            dropdownList = foundObj.transform;
        }

        if (dropdownList != null) {
            // 2. 그 안의 ScrollRect 컴포넌트 찾기
            ScrollRect scrollRect = dropdownList.GetComponentInChildren<ScrollRect>();

            if (scrollRect != null) {
                // 3. [복구] 저장해둔 위치로 즉시 이동
                scrollRect.verticalNormalizedPosition = lastScrollPosition;

                // 4. [기록] 유저가 스크롤할 때마다 위치 실시간 저장
                scrollRect.onValueChanged.AddListener((Vector2 val) => {
                    lastScrollPosition = val.y;
                });
            }
        }
    }
}