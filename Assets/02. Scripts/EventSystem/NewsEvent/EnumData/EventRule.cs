using System;
using UnityEngine;

[Serializable]
public class EventRule {
    public string key;

    public string authorId;
    public string uiKey;
    public string effectKey;

    public ExecutionMode executionMode;   // 1: RandomInWindow, 2: AfterPrerequisite

    public string[] prerequisites;
    public OutcomeRule outcomeRule;
    public OutcomeEntry[] outcomes;
    public bool gate = true;

    public float probability;
    public RepeatType repeatType;

    // RandomInWindow
    public string startDate;
    public string endDate;

    // AfterPrerequisite
    public DayOffset dayOffset;
    public TimeRule timeRule;
}
