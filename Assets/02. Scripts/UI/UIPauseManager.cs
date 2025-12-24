using UnityEngine;

public static class UIPauseManager {
    private static int openPanelCount = 0;

    public static bool IsPaused => openPanelCount > 0;

    public static void RegisterPanel() {
        openPanelCount++;

        if (openPanelCount > 0)
            Time.timeScale = 0;
    }

    public static void UnregisterPanel() {
        openPanelCount--;

        if (openPanelCount <= 0) {
            openPanelCount = 0;
            Time.timeScale = 1;
        }
    }
}
