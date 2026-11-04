using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class JerryLevelProgress : MonoBehaviour
{
    public int CheckpointOrder { get; private set; }
    [Min(0f)] public float checkpointGraceSeconds = 1f;
    [Min(0f)] public float exitGraceSeconds = 2f;

    private static string restartScenePath;
    private static int restartCheckpointOrder;
    private JerryCheckpoint savedCheckpoint;
    private JerryCheckpoint pendingCheckpoint;
    private float checkpointDeadline = float.PositiveInfinity;
    private float exitDeadline = float.PositiveInfinity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRestartState()
    {
        restartScenePath = null;
        restartCheckpointOrder = 0;
    }

    private void Start()
    {
        // This token belongs only to the scene reload requested by R. Normal
        // level entry, completion-screen Retry and a new Play session start fresh.
        string scenePath = restartScenePath;
        int order = restartCheckpointOrder;
        ResetRestartState();
        if (scenePath != gameObject.scene.path || order <= 0)
            return;

        PlayerRespawnJ player = FindFirstObjectByType<PlayerRespawnJ>();
        if (player == null)
            return;
        foreach (JerryCheckpoint checkpoint in FindObjectsByType<JerryCheckpoint>(FindObjectsSortMode.None))
        {
            if (checkpoint.progress != this || checkpoint.checkpointOrder != order)
                continue;
            Save(checkpoint, player);
            Vector3 position = checkpoint.transform.position;
            player.transform.position = position;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.WakeUp();
            }
            player.ResetAndPlayRespawn();
            break;
        }
    }

    public void PrepareRestart(PlayerRespawnJ player)
    {
        ResetRestartState();
        if (savedCheckpoint == null || player == null || player.gameObject.scene != gameObject.scene)
            return;
        restartScenePath = gameObject.scene.path;
        restartCheckpointOrder = CheckpointOrder;
    }

    public void Save(JerryCheckpoint checkpoint, PlayerRespawnJ player)
    {
        // Later checkpoints cannot be overwritten by walking back to an earlier one.
        if (checkpoint.checkpointOrder <= CheckpointOrder)
            return;
        CheckpointOrder = checkpoint.checkpointOrder;
        savedCheckpoint = checkpoint;
        player.respawnPoint = checkpoint.transform;
        if (pendingCheckpoint != null && pendingCheckpoint.IsSaved)
        {
            pendingCheckpoint = null;
            checkpointDeadline = float.PositiveInfinity;
        }
    }

    public void NotifyCollapse(JerryCheckpoint checkpoint, bool requiresLevelExit)
    {
        if (requiresLevelExit)
            exitDeadline = Mathf.Min(exitDeadline, Time.time + exitGraceSeconds);
        else if (checkpoint != null && !checkpoint.IsSaved)
        {
            pendingCheckpoint = checkpoint;
            checkpointDeadline = Mathf.Min(checkpointDeadline, Time.time + checkpointGraceSeconds);
        }
    }

    private void OnGUI()
    {
        if (Time.timeScale <= 0f)
            return;
        // Scaled time excludes pauses. Successful exit opens the existing
        // completion screen (which pauses time) or unloads this scene.
        bool missedCheckpoint = pendingCheckpoint != null && !pendingCheckpoint.IsSaved &&
            Time.time >= checkpointDeadline;
        bool missedExit = Time.time >= exitDeadline;
        bool needsRestart = missedCheckpoint || missedExit;
        if (!needsRestart)
            return;
        string text = missedExit
            ? "Exit not reached in time. Press R to retry from checkpoint."
            : "Checkpoint not reached in time. Press R to restart this level.";
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Screen.height / 32, 16, 30),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        style.normal.textColor = new Color(1f, 0.86f, 0.55f);
        float width = Mathf.Min(900f, Screen.width - 32f);
        Rect rect = new Rect((Screen.width - width) / 2f, Screen.height * 0.12f, width, 78f);
        Color previous = GUI.color;
        GUI.color = new Color(0.03f, 0.05f, 0.07f, 0.85f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(rect, text, style);
    }
}
