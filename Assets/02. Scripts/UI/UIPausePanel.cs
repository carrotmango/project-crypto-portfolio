using UnityEngine;

public class UIPausePanel : MonoBehaviour {
    private void OnEnable() {
        UIPauseManager.RegisterPanel();
    }

    private void OnDisable() {
        UIPauseManager.UnregisterPanel();
    }
}
