using System;

[Serializable]
public class GameData {
    public string playerName;
    public int characterIndex;
    public string birthday;
    public double bullbitCash;
    public double satoshiBankCash;

    public GameData() {
        playerName = "Unknown";
        characterIndex = 0;
        birthday = "1¿ù 1ÀÏ";
        bullbitCash = 1000000;
        satoshiBankCash = 0;
    }
}
