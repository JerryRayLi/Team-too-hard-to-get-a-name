using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class FinalLevelManager : MonoBehaviour
{
    public static FinalLevelManager Instance { get; private set; }
    public Vector3 playerSpawn = new Vector3(-12f, -2.4f, 0);
    public FinalLevelPlayerController player;
    public FinalWeightBrick brick;
    public FinalCollapsePlatform collapse;
    public FinalChaser chaser;
    public HaoyuTianyu.PlayerRespawn effects;
    FinalWeightBrick[] bricks;
    FinalCollapsePlatform[] collapses;
    FinalChaser[] chasers;
    FinalLandingCrack[] cracks;
    FinalTimedSpikeTrigger[] timedTriggers;
    HaoyuTianyu.SpikeTrap[] spikeTraps;
    HaoyuTianyu.SpikeTrapTrigger[] spikeTriggers;
    FinalSpikeHazard[] spikeHazards;
    public bool Completed { get; private set; }
    public int Deaths { get; private set; }
    public int RunNumber { get; private set; }
    public float RunSeconds { get; private set; }
    public string LastDeathReason { get; private set; }
    public float LastWeightedCatchSeconds { get; private set; } = -1;
    public bool CanMove => !Completed && Time.time >= contactReadyAt && (!effects || !effects.IsRespawning);
    public bool AcceptContact => CanMove;
    float contactReadyAt, startedAt, weightedAt = -1;
    // A quiet nudge toward the colour switch for players who keep dying without ever using it.
    [TextArea] public string colorHint = "Isn't there a key you haven't pressed in a while?";
    [Min(1)] public int colorHintDeaths = 3;
    public float fallKillY = -7.5f;
    ColorLayerManager colors;
    bool usedColorSwitch;
    public bool ShowingColorHint => colors && !usedColorSwitch && Deaths >= colorHintDeaths && !Completed;
    Color originalPlayerColor;
    Vector3 originalScale;
    void Awake()
    {
        Instance = this;
        player = FindFirstObjectByType<FinalLevelPlayerController>();
        brick = FindFirstObjectByType<FinalWeightBrick>();
        collapse = FindFirstObjectByType<FinalCollapsePlatform>();
        chaser = FindFirstObjectByType<FinalChaser>();
        effects = player.GetComponent<HaoyuTianyu.PlayerRespawn>();
        bricks = FindObjectsByType<FinalWeightBrick>(FindObjectsSortMode.None);
        collapses = FindObjectsByType<FinalCollapsePlatform>(FindObjectsSortMode.None);
        chasers = FindObjectsByType<FinalChaser>(FindObjectsSortMode.None);
        cracks = FindObjectsByType<FinalLandingCrack>(FindObjectsSortMode.None);
        timedTriggers = FindObjectsByType<FinalTimedSpikeTrigger>(FindObjectsSortMode.None);
        spikeTraps = FindObjectsByType<HaoyuTianyu.SpikeTrap>(FindObjectsSortMode.None);
        spikeTriggers = FindObjectsByType<HaoyuTianyu.SpikeTrapTrigger>(FindObjectsSortMode.None);
        spikeHazards = FindObjectsByType<FinalSpikeHazard>(FindObjectsSortMode.None);
        originalPlayerColor = player.GetComponent<SpriteRenderer>().color;
        originalScale = player.transform.localScale;
    }
    void Start()
    {
        ResetRun();
        colors = FindFirstObjectByType<ColorLayerManager>();
        GameHUD.CreateControls(transform, colors ? GameHUD.ColorControls : null);
    }
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToMainMenu();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (!Completed) Die("restart");
        }
        if (CanMove) RunSeconds = Time.time - startedAt;
        // Backstop for falls that miss the death zone (e.g. running off the far left edge).
        if (CanMove && player.transform.position.y < fallKillY) Die("fall");
        if (colors && colors.IsBlueActive) usedColorSwitch = true;
    }
    public void Weighted() { if (weightedAt < 0) weightedAt = Time.time; }
    public void RequestRespawn() { Die("hazard"); }
    public void Die(string reason)
    {
        if (!AcceptContact) return;
        Deaths++; PlaySessionStats.RecordDeath(); LastDeathReason = reason;
        LastWeightedCatchSeconds = reason == "chaser" && weightedAt >= 0 ? Time.time - weightedAt : -1;
        Debug.Log($"FINAL_EVENT death reason={reason} run={RunNumber} seconds={RunSeconds:F3} weightedCatch={LastWeightedCatchSeconds:F3} x={player.transform.position.x:F3}");
        if (effects) effects.Respawn(); else ResetRun();
    }
    public void ResetRun()
    {
        Completed = false; RunNumber++; RunSeconds = 0; weightedAt = -1;
        contactReadyAt = Time.time + Mathf.Max(0.12f, effects ? effects.RespawnDuration : 0); startedAt = contactReadyAt;
        Time.timeScale = 1;
        player.Body.simulated = false;
        player.transform.position = playerSpawn;
        player.transform.localScale = originalScale;
        player.GetComponent<SpriteRenderer>().color = originalPlayerColor;
        player.ResetMovementState();
        foreach (var item in bricks) item.ResetState();
        foreach (var item in collapses) item.ResetState();
        foreach (var item in chasers) item.ResetState();
        foreach (var item in cracks) item.ResetState();
        foreach (var item in timedTriggers) item.ResetState();
        foreach (var trap in spikeTraps) trap.ResetState();
        foreach (var trigger in spikeTriggers) trigger.ResetState();
        foreach (var hazard in spikeHazards) hazard.ResetState();
        Physics2D.SyncTransforms();
        player.Body.simulated = true;
        if (effects) effects.ResetAndPlayRespawn();
        Debug.Log($"FINAL_EVENT reset run={RunNumber}");
    }
    public void CompleteLevel()
    {
        if (!AcceptContact) return;
        RunSeconds = Time.time - startedAt; Completed = true;
        PlaySessionStats.MarkCurrentLevelCompleted();
        player.Body.linearVelocity = Vector2.zero;
        Debug.Log($"FINAL_EVENT complete run={RunNumber} seconds={RunSeconds:F3}");
    }
    // Shared header so the title, death count and menu button scale with the other levels.
    void OnGUI()
    {
        GameHUD.DrawHeader(ReturnToMainMenu);
        if (!ShowingColorHint) return;
        // Small, faint and above the taunt line, so it reads as a passing thought rather than a tutorial.
        var style = new GUIStyle(GUI.skin.label) { fontSize = GameHUD.ScaleFont(22), fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        style.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
        GUI.Label(new Rect(Screen.width * 0.2f, Screen.height * 0.68f, Screen.width * 0.6f, 48f), colorHint, style);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}

