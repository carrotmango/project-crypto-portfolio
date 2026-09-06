using UnityEngine;

public class EventDispatchManager : MonoBehaviour {
    public EventOrchestrator orchestrator;

    public void Dispatch(string authorId, string eventKey) {
        orchestrator.RunTweetEvent(authorId, eventKey);
    }
}
