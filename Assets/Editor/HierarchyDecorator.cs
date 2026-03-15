using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HierarchyDecorator {
    static HierarchyDecorator() {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceID, Rect selectionRect) {
        // 오브젝트 가져오기
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        // 이름이 "!"로 시작하는 오브젝트만 색칠 (판단용 기호)
        if (obj.name.StartsWith("!")) {
            // 1. 배경색 설정 (진한 녹색 - 완료 느낌)
            Color backgroundColor = new Color(0.15f, 0.35f, 0.15f);
            // 2. 글자색 설정 (흰색)
            Color textColor = Color.white;

            // 선택되었을 때의 기본 강조색과 겹치지 않게 처리
            if (Selection.activeGameObject != obj) {
                // 줄 전체 배경 그리기
                Rect fullRect = new Rect(selectionRect.xMin, selectionRect.yMin, selectionRect.width + selectionRect.xMax, selectionRect.height);
                EditorGUI.DrawRect(fullRect, backgroundColor);

                // 글자 다시 그리기
                GUIStyle style = new GUIStyle();
                style.normal.textColor = textColor;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleLeft;

                // 아이콘 위치를 고려한 여백 추가
                Rect textRect = new Rect(selectionRect.xMin + 18, selectionRect.yMin, selectionRect.width, selectionRect.height);
                EditorGUI.LabelField(textRect, obj.name, style);
            }
        }
    }
}