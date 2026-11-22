using System;
using System.Collections.Generic;

// A bounded, event-driven subtitle schedule. This model has no scene or input
// access, and survives same-level reloads through its owning narrator.
public sealed class TrialNarrationSequence
{
    public readonly TrialNarrationProfile Profile;
    private readonly HashSet<TrialNarrationCue> played = new HashSet<TrialNarrationCue>();
    private readonly HashSet<TrialNarrationCue> pending = new HashSet<TrialNarrationCue>();
    private bool leftStart;
    private bool progressed;
    private int observedDeaths;
    private float activeTime, silence;
    private bool replacementScheduled;
    private const float IdleHoldDuration = 8f;

    public TrialNarrationCue? CurrentCue { get; private set; }
    public string CurrentText => CurrentCue.HasValue ? Profile.TextFor(CurrentCue.Value) : null;
    public float Age { get; private set; }
    public float Duration { get; private set; }
    public float MinimumReadTime { get; private set; }
    public int PlayedCount => played.Count;
    public bool IsDeathResponse => CurrentCue == TrialNarrationCue.FirstDeath || CurrentCue == TrialNarrationCue.RepeatedDeath;
    public bool HasPlayed(TrialNarrationCue cue) => played.Contains(cue);

    public TrialNarrationSequence(TrialNarrationProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        pending.Add(TrialNarrationCue.Entry);
    }

    public void Tick(float deltaTime, bool alive, bool canStartLine, int levelDeaths, bool nearStart, bool madeProgress)
    {
        if (levelDeaths > observedDeaths && Profile.HasDeathResponses)
        {
            observedDeaths = levelDeaths;
            // Keep a death response readable across another death/reload.
            // Only stale entry/progress/hesitation lines are interrupted.
            if (!IsDeathResponse)
            {
                CurrentCue = null;
                Age = Duration = 0f;
                silence = 0f;
                replacementScheduled = false;
            }
            pending.Remove(TrialNarrationCue.Entry);
            pending.Remove(TrialNarrationCue.Hesitation);
            pending.Remove(TrialNarrationCue.Progress);
            pending.Remove(TrialNarrationCue.ColorDiscovery);
            Enqueue(TrialNarrationCue.FirstDeath);
            if (levelDeaths >= 3)
                Enqueue(TrialNarrationCue.RepeatedDeath);
        }
        if (deltaTime <= 0f)
            return;

        float dt = Math.Min(deltaTime, .25f);
        if (alive)
            activeTime += dt;
        if (alive && !nearStart)
        {
            leftStart = true;
            pending.Remove(TrialNarrationCue.Hesitation);
        }
        if (alive && madeProgress)
        {
            progressed = true;
            pending.Remove(TrialNarrationCue.Hesitation);
            pending.Remove(TrialNarrationCue.ColorDiscovery);
            if (CurrentCue == TrialNarrationCue.ColorDiscovery)
            {
                CurrentCue = null;
                Age = Duration = 0f;
                silence = .15f;
                replacementScheduled = false;
            }
            Enqueue(TrialNarrationCue.Progress);
        }
        if (activeTime >= 2f && nearStart && !leftStart && !progressed && observedDeaths == 0)
            Enqueue(TrialNarrationCue.Hesitation);
        // Tutorial discovery follows elapsed play time, not spawn proximity.
        // Walking to a color wall must not dismiss the color-change reminder.
        if (activeTime >= 2f && !progressed)
            Enqueue(TrialNarrationCue.ColorDiscovery);

        if (CurrentCue.HasValue)
        {
            if (!alive && !IsDeathResponse)
                return;
            Age += dt;
            var next = NextEligible(alive, canStartLine);
            if (Age >= MinimumReadTime && next.HasValue)
            {
                if (!replacementScheduled)
                {
                    Duration = Math.Min(IdleHoldDuration, Age + .10f);
                    replacementScheduled = true;
                }
            }
            else if (replacementScheduled && Age < IdleHoldDuration)
            {
                // A reminder can become stale or the player can jump during
                // the outgoing fade. Keep readable text if nothing can follow.
                Duration = IdleHoldDuration;
                replacementScheduled = false;
            }
            if (Age >= Duration)
            {
                CurrentCue = null;
                if (next.HasValue)
                    Begin(next.Value);
                else
                    silence = .15f;
            }
            return;
        }

        silence = Math.Max(0f, silence - dt);
        if (silence > .0001f)
            return;
        var following = NextEligible(alive, canStartLine);
        if (following.HasValue)
            Begin(following.Value);
    }

    private TrialNarrationCue? NextEligible(bool alive, bool canStartLine)
    {
        TrialNarrationCue? next = null;
        int priority = -1;
        foreach (var cue in pending)
        {
            bool death = cue == TrialNarrationCue.FirstDeath || cue == TrialNarrationCue.RepeatedDeath;
            if (!death && (!alive || activeTime < .3f || !canStartLine))
                continue;
            int candidate = Priority(cue);
            if (candidate > priority)
            {
                priority = candidate;
                next = cue;
            }
        }
        return next;
    }

    private void Begin(TrialNarrationCue cue)
    {
        pending.Remove(cue);
        played.Add(cue);
        CurrentCue = cue;
        Age = 0f;
        replacementScheduled = false;
        int words = CurrentText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        MinimumReadTime = Math.Max(2.6f, Math.Min(3.4f, 1.8f + words * .16f));
        Duration = IdleHoldDuration;
    }

    private void Enqueue(TrialNarrationCue cue)
    {
        if (Profile.HasLine(cue) && !played.Contains(cue) && CurrentCue != cue)
            pending.Add(cue);
    }

    private static int Priority(TrialNarrationCue cue)
    {
        switch (cue)
        {
            case TrialNarrationCue.Entry: return 5;
            case TrialNarrationCue.FirstDeath: return 4;
            case TrialNarrationCue.RepeatedDeath: return 3;
            case TrialNarrationCue.Progress: return 2;
            case TrialNarrationCue.ColorDiscovery: return 1;
            case TrialNarrationCue.Hesitation: return 1;
            default: return 0;
        }
    }
}
