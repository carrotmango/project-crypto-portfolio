using UnityEngine;

public class GameSaveController : MonoBehaviour
{
    public void OnSaveClicked()
    {
        SaveManager.Save();
    }

    public void OnLoadClicked()
    {
        SaveManager.Load();
    }
}

