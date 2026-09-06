[System.Serializable]
public class PlayerSyncData {
    public string walletAddress;
    public string playerName;
    public double totalAsset;

    public PlayerSyncData(string wallet, string name, double asset) {
        walletAddress = wallet;
        playerName = name;
        totalAsset = asset;
    }
}
