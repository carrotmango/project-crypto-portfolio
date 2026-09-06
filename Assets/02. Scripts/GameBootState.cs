public static class GameBootState {
    public static bool saveLoaded;
    public static bool playerReady;

    public static void Reset() {
        saveLoaded = false;
        playerReady = false;
    }

    public static bool IsAllReady() {
        return saveLoaded && playerReady;
    }
}
