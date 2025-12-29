using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour {
    public EventRuleRepository ruleRepo;
    public EventDispatchManager dispatcher;
    public CoinManager coinManager;

    private List<ScheduledEvent> scheduled = new();
    private HashSet<string> executed = new();
    private Dictionary<string, DateTime> executedTime = new();

    private bool initialized;
    private HashSet<string> outcomeResolved = new();


    // -------------------------------------------------
    // Lifecycle
    // -------------------------------------------------
    private void Start() {
        if (initialized) return;
        initialized = true;
        Initialize();
    }

    private void OnEnable() {
        if (coinManager != null) {
            coinManager.OnTimeAdvanced += OnTimeAdvanced;
        }
    }

    private void OnDisable() {
        if (coinManager != null) {
            coinManager.OnTimeAdvanced -= OnTimeAdvanced;
        }
    }

    // -------------------------------------------------
    // 초기 RandomInWindow 이벤트 예약
    // -------------------------------------------------
    public void Initialize() {
        foreach (var rule in ruleRepo.All()) {
            if (rule.executionMode != ExecutionMode.RandomInWindow)
                continue;

            if (rule.repeatType == RepeatType.Once && executed.Contains(rule.key))
                continue;

            float probability = rule.probability <= 0f ? 1f : rule.probability;
            if (UnityEngine.Random.value > probability)
                continue;

            if (!DateTime.TryParse(rule.startDate, out var start) ||
                !DateTime.TryParse(rule.endDate, out var end)) {
                Debug.LogWarning($"[Event] 날짜 파싱 실패: {rule.key}");
                continue;
            }

            if (end <= start) {
                Debug.LogWarning($"[Event] 날짜 범위 오류: {rule.key}");
                continue;
            }

            TimeSpan span = end - start;
            double randSeconds = UnityEngine.Random.value * span.TotalSeconds;

            DateTime executeAt = start.AddSeconds(randSeconds);
            DateTime now = coinManager.CurrentDateTime;

            if (executeAt <= now) {
                Debug.Log($"[Event] 과거 예약 스킵: {rule.key} @ {executeAt}");
                continue;
            }

            scheduled.Add(new ScheduledEvent {
                rule = rule,
                executeAt = executeAt
            });

            Debug.Log($"[Event] 예약됨(Random): {rule.key} @ {executeAt}");
        }
    }

    // -------------------------------------------------
    // 시간 진행 시 실행 체크
    // -------------------------------------------------
    private void OnTimeAdvanced(DateTime now) {
        for (int i = scheduled.Count - 1; i >= 0; i--) {
            if (now >= scheduled[i].executeAt) {
                Execute(scheduled[i]);
                scheduled.RemoveAt(i);
            }
        }
    }

    // -------------------------------------------------
    // 이벤트 실행
    // -------------------------------------------------
    private void Execute(ScheduledEvent s) {
        var rule = s.rule;
        DateTime now = coinManager.CurrentDateTime;

        if (string.IsNullOrEmpty(rule.authorId)) {
            Debug.LogWarning($"[Event] authorId 없음: {rule.key}");
            return;
        }

        // 1. Effect 실행
        if (!string.IsNullOrEmpty(rule.effectKey)) {
            Debug.Log($"[Event] Effect 호출: {rule.effectKey}");
            EffectManager.Instance.Apply(rule.effectKey);
        }

        // 2. UI 디스패치
        dispatcher.Dispatch(rule.authorId, rule.uiKey);

        // 3. 실행 기록
        executed.Add(rule.key);
        executedTime[rule.key] = now;

        Debug.Log($"[Event] 실행됨: {rule.key}");

        // 4. Outcome 해금 (핵심 추가)
        ResolveOutcome(rule);

        // 5. 후속 이벤트 예약
        ScheduleAfterPrerequisite(rule, now);
    }

    public bool HasExecuted(string key) {
        return executed.Contains(key);
    }

    // -------------------------------------------------
    // Outcome 처리
    // -------------------------------------------------
    private void ResolveOutcome(EventRule rule) {
        if (outcomeResolved.Contains(rule.key))
            return;

        outcomeResolved.Add(rule.key);

        if (rule.outcomes == null || rule.outcomes.Length == 0)
            return;

        if (rule.outcomeRule == OutcomeRule.OneOf) {
            var picked = PickWeightedOutcome(rule.outcomes);

            foreach (var entry in rule.outcomes) {
                var outcomeRule = ruleRepo.Get(entry.key);
                if (outcomeRule == null)
                    continue;

                if (entry.key != picked.key) {
                    outcomeRule.gate = false;
                    Debug.Log($"[Outcome] 차단됨: {entry.key}");
                } else {
                    Debug.Log($"[Outcome] 선택됨: {entry.key} (weight={entry.weight})");
                }
            }
        } else if (rule.outcomeRule == OutcomeRule.NonRequired) {
            Debug.Log($"[Outcome] NonRequired - 선택 없음 가능: {rule.key}");
        }
    }

    private OutcomeEntry PickWeightedOutcome(OutcomeEntry[] outcomes) {
        float total = 0f;
        foreach (var o in outcomes)
            total += Mathf.Max(0f, o.weight);

        float roll = UnityEngine.Random.value * total;
        float acc = 0f;

        foreach (var o in outcomes) {
            acc += Mathf.Max(0f, o.weight);
            if (roll <= acc)
                return o;
        }

        return outcomes[outcomes.Length - 1]; // fallback
    }


    // -------------------------------------------------
    // AfterPrerequisite 이벤트 예약
    // -------------------------------------------------
    private void ScheduleAfterPrerequisite(EventRule executedRule, DateTime baseTime) {
        foreach (var rule in ruleRepo.All()) {
            if (rule.executionMode != ExecutionMode.AfterPrerequisite)
                continue;

            if (rule.repeatType == RepeatType.Once && executed.Contains(rule.key))
                continue;

            if (!rule.gate)
                continue;

            if (rule.prerequisites == null || rule.prerequisites.Length == 0)
                continue;

            if (Array.IndexOf(rule.prerequisites, executedRule.key) < 0)
                continue;

            DateTime executeAt = EventTimeCalculator.Calculate(
                baseTime,
                rule.dayOffset,
                rule.timeRule
            );

            DateTime now = coinManager.CurrentDateTime;

            if (executeAt <= now) {
                Debug.Log($"[Event] AfterPrerequisite 과거 예약 스킵: {rule.key} @ {executeAt}");
                continue;
            }

            scheduled.Add(new ScheduledEvent {
                rule = rule,
                executeAt = executeAt
            });

            Debug.Log($"[Event] AfterPrerequisite 예약: {rule.key} @ {executeAt}");
        }
    }
}
