using System;

[Serializable]
public class ScheduledEvent {
    public EventRule rule;
    public DateTime executeAt;
}
