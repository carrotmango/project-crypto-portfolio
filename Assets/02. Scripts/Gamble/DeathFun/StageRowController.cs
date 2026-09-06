using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageRowController : MonoBehaviour {
    public TextMeshProUGUI multiplierText;
    public Transform cellContainer; // 자식에 7개 Cell이 있어야 함
    public GameObject cellPrefab;


    public void Init(double multiplier, int activeCount, int bombIndex = -1) {
        multiplierText.text = $"{multiplier:F2}x";

        for (int i = 0; i < cellContainer.childCount; i++) {
            var cell = cellContainer.GetChild(i).gameObject;

            if (i < activeCount) {
                cell.SetActive(true);
                var controller = cell.GetComponent<CellController>();
                if (controller != null) {
                    controller.SetBomb(i == bombIndex); // 폭탄 여부 설정
                }
            } else {
                cell.SetActive(false);
            }
        }
    }
    public void SetRowInteractable(bool isInteractable) {
        for (int i = 0; i < cellContainer.childCount; i++) {
            var cell = cellContainer.GetChild(i).gameObject;
            var controller = cell.GetComponent<CellController>();
            if (controller != null) {
                controller.SetInteractable(isInteractable);
            }
        }
    }


}
