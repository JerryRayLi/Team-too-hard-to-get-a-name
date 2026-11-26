// Per-visit help state. The controller retains this across same-level R reloads.
public sealed class JerryHelpSession
{
    public bool Unlocked { get; private set; }
    public bool GuideOpen;
    public int Step;
    public float StalledSeconds { get; private set; }
    public int ProgressFlags { get; private set; }
    private int deathsAtProgress;

    public JerryHelpSession(int deaths) { deathsAtProgress = deaths; }
    public void Observe(float seconds, int deaths, float waitSeconds, int deathLimit)
    {
        StalledSeconds += UnityEngine.Mathf.Max(0f, seconds);
        if (StalledSeconds >= waitSeconds || deaths - deathsAtProgress >= deathLimit)
            Unlocked = true;
    }
    public void MarkProgress(int flag, int deaths)
    {
        if ((ProgressFlags & flag) != 0) return;
        ProgressFlags |= flag;
        StalledSeconds = 0f;
        deathsAtProgress = deaths;
    }
}
