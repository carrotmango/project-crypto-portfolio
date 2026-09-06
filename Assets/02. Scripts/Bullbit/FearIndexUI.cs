using TMPro;
using UnityEngine;

public class FearIndexUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI labelText;

    private void Update() {
        var fear = FearIndexManager.Instance.GetFearIndex();

        scoreText.text = fear.ToString();
        labelText.text = FearIndexManager.Instance.GetFearLabel();
    }
}
