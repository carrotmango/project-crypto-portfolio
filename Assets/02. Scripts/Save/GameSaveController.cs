using UnityEngine;

public class GameSaveController : MonoBehaviour
{
    public static void OnSaveClicked()
    {
        SaveManager.Save();
    }

    public static void OnLoadClicked()
    {
        SaveManager.Load();
    }
}

