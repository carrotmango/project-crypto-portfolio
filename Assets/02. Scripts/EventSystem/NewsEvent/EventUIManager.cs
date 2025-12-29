using System;
using UnityEngine;

public class EventUIManager : MonoBehaviour {

    public static EventUIManager Instance;

    [Header("Panels")]
    public NewsPanel newsPanel;

    private void Awake() {
        Instance = this;
    }

    public void Show(UIEventData data) {

        switch (data.type) {

            case UIEventType.News: {
                    DateTime gameTime = CoinManager.Instance.CurrentDateTime;
                    newsPanel.Show(data, gameTime);
                    break;
                }

            case UIEventType.RumorSimple: {
                    UIManager.Instance.ShowConfirm(data.title, data.message);
                    break;
                }

            case UIEventType.RumorDM: {
                    UIManager.Instance.ShowIncomingCall(data.title, data.message);
                    break;
                }
        }
    }
}
