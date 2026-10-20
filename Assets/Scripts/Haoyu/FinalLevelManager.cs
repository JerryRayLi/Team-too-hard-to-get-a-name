using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FinalLevelManager : MonoBehaviour
{
    public static FinalLevelManager Instance { get; private set; }
    public Vector3 playerSpawn = new Vector3(-12f, -2.4f, 0);
    public FinalLevelPlayerController player;
    public FinalWeightBrick brick;
    public FinalCollapsePlatform collapse;
    public FinalChaser chaser;
    public HaoyuTianyu.PlayerRespawn effects;
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
        spikeTraps = FindObjectsByType<HaoyuTianyu.SpikeTrap>(FindObjectsSortMode.None);
        spikeTriggers = FindObjectsByType<HaoyuTianyu.SpikeTrapTrigger>(FindObjectsSortMode.None);
        spikeHazards = FindObjectsByType<FinalSpikeHazard>(FindObjectsSortMode.None);
        originalPlayerColor = player.GetComponent<SpriteRenderer>().color;
        originalScale = player.transform.localScale;
    }
    void Start() { ResetRun(); }
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (!Completed) Die("restart");
        }
        if (CanMove) RunSeconds = Time.time - startedAt;
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
        brick.ResetState(); collapse.ResetState(); chaser.ResetState();
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
    void OnGUI()
    {
        // Tianyu GameHUD layout, with this level's controls and completion flow.
        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = Color.white;
        var bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, richText = true };
        bodyStyle.normal.textColor = Color.white;
        var deathStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleRight };
        GUI.Label(new Rect(20, 15, 400, 35), UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, titleStyle);
        GUI.Label(new Rect(20, 50, 760, 40), "Move: <b>A / D</b>     Jump: <b>Space</b>     Restart: <b>R</b>", bodyStyle);
        GUI.Label(new Rect(Screen.width - 220, 15, 200, 30), "Deaths: " + PlaySessionStats.TotalDeaths, deathStyle);
        GUI.Label(new Rect(20, Screen.height - 50, 400, 40), "<b>R</b>: Restart Level", bodyStyle);
    }
    void OnDestroy() { if (Instance == this) Instance = null; }
}

