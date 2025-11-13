using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class StageManager : MonoBehaviour {
    [Header("프리팹 및 부모")]
    public GameObject stageRowPrefab;
    public Transform contentParent; // ScrollView 안의 Viewport 안의 Content

    [Header("현재 상태")]
    public int CurrentFloor => currentFloor;

    // 배율 테이블 (1층부터 25층까지)
    private readonly double[] multipliers = {
        1.10, 1.32, 1.66, 2.21, 3.32, 4.43, 5.54, 6.64, 7.75,
        9.30, 11.6, 15.5, 23.2, 31.0, 38.7, 46.5, 54.3, 65.1,
        81.4, 108, 162, 217, 271, 325, 380
    };

    // 층별 활성 셀 수
    private readonly int[] cellCounts = {
        7, 6, 5, 4, 3, 4, 5, 6, 7,
        6, 5, 4, 3, 4, 5, 6, 7, 6,
        5, 4, 3, 4, 5, 6, 7
    };

    private List<StageRowController> stageRows = new();
    private int currentFloor = 0;

    void Start() {
        for (int i = 0; i < multipliers.Length; i++) {
            GameObject rowObj = Instantiate(stageRowPrefab, contentParent);
            rowObj.name = $"StageRow{i + 1}";

            var controller = rowObj.GetComponent<StageRowController>();
            double multiplier = multipliers[i];
            int cellCount = cellCounts[i];
            int bombIndex = Random.Range(0, cellCount);

            controller.Init(multiplier, cellCount, bombIndex);
            stageRows.Add(controller);
        }

        UpdateFloorState();  // 1층 활성화
        ScrollToBottom();    // 스크롤 맨 아래로 이동
    }

    private void UpdateFloorState() {
        for (int i = 0; i < stageRows.Count; i++) {
            bool isCurrent = (i == currentFloor);
            stageRows[i].SetRowInteractable(isCurrent);
        }

        Debug.Log($"[StageManager] 현재 층: {currentFloor + 1}층 활성화");
    }

    private void ScrollToBottom() {
        Canvas.ForceUpdateCanvases(); // UI 강제 업데이트

        var scrollRect = contentParent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null) {
            scrollRect.verticalNormalizedPosition = 0f; // 0: 맨 아래
        } else {
            Debug.LogWarning("[StageManager] ScrollRect를 찾지 못했습니다.");
        }
    }

    public void PassFloor() {
        currentFloor++;
        if (currentFloor < stageRows.Count) {
            UpdateFloorState();
        } else {
            Debug.Log("[StageManager] 모든 층 완료");
        }
    }

    public void RevealBombs(int floorIndex) {
        if (floorIndex < 0 || floorIndex >= stageRows.Count) return;

        var floor = stageRows[floorIndex];
        foreach (Transform child in floor.cellContainer) {
            var cell = child.GetComponent<CellController>();
            if (cell != null && cell.isBomb) {
                cell.skullIcon?.gameObject.SetActive(true);
            }
        }
    }
    public void RevealAllOnGameOver(int failedFloorIndex, int failedCellIndex) {
        for (int i = 0; i < stageRows.Count; i++) {
            var row = stageRows[i];
            bool isPassedRow = i < failedFloorIndex;
            bool isFailedRow = i == failedFloorIndex;
            bool isUnreachedRow = i > failedFloorIndex;

            for (int j = 0; j < row.cellContainer.childCount; j++) {
                var cellObj = row.cellContainer.GetChild(j).gameObject;
                var cell = cellObj.GetComponent<CellController>();

                if (cell != null) {
                    if (isUnreachedRow) {
                        // 도달하지 않은 층: 폭탄만 표시
                        if (cell.isBomb && cell.skullIcon != null)
                            cell.skullIcon.gameObject.SetActive(true);
                    } else {
                        // 기존 로직 유지
                        bool isFailedCell = (isFailedRow && j == failedCellIndex);
                        cell.OnGameOverReveal(isPassedRow, isFailedRow, isFailedCell);
                    }
                }
            }
        }
    }



    public double GetMultiplier(int floorIndex) {
        if (floorIndex >= 0 && floorIndex < multipliers.Length)
            return multipliers[floorIndex];
        else
            return 1.0;
    }

    public void DisableAllCells() {
        foreach (Transform child in contentParent) {
            var row = child.GetComponent<StageRowController>();
            if (row == null) continue;

            foreach (Transform cell in row.cellContainer) {
                var ctrl = cell.GetComponent<CellController>();
                if (ctrl != null) ctrl.DisableClick();
            }
        }
    }
    public void RandomizeBombsOnly() {
        foreach (var row in stageRows) {
            int activeCellCount = 0;

            // 활성 셀 수 계산
            for (int i = 0; i < row.cellContainer.childCount; i++) {
                if (row.cellContainer.GetChild(i).gameObject.activeSelf)
                    activeCellCount++;
            }

            int bombIndex = Random.Range(0, activeCellCount); // 폭탄 인덱스 재설정

            for (int i = 0; i < row.cellContainer.childCount; i++) {
                var cell = row.cellContainer.GetChild(i).GetComponent<CellController>();
                if (cell != null) {
                    cell.ResetCell(); // 상태 초기화
                    cell.SetBomb(i == bombIndex);
                    cell.EnableClick();
                }
            }

        }

        // 첫 층 다시 활성화
        currentFloor = 0;
        UpdateFloorState(); 
    }
    public void DisableAllCellsOnFloor(int floor) {
        if (floor < 0 || floor >= stageRows.Count) return;

        var row = stageRows[floor];
        foreach (Transform child in row.cellContainer) {
            var cell = child.GetComponent<CellController>();
            if (cell != null) {
                cell.SetInteractable(false);
            }
        }
    }


}