using UnityEngine;
using System;

public class NewsManager : MonoBehaviour {
    public XPostRepository repo;
    public EventOrchestrator orchestrator;
    public CoinManager coinManager;

    public void Tick() {
        DateTime now = coinManager.CurrentDateTime;

        foreach (var author in repo.authors) {
            foreach (var data in author.events) {
                if (orchestrator.HasExecuted(data.key)) continue;

                if (DateTime.TryParse(data.startDate, out var start) &&
                    DateTime.TryParse(data.endDate, out var end)) {
                    if (now >= start && now <= end) {
                        if (data.eventMustRequired || UnityEngine.Random.value < 0.02f) {
                            orchestrator.RunTweetEvent(author.id, data.key);
                        }
                    }
                }
            }
        }
    }
}
