using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Scene-specific guidance and checkpoints; the corpse and button rules remain
// the existing Jerry implementations.
public sealed class LevelJaGuide : MonoBehaviour
{
    public PlayerRespawnJ player;
    public Transform firstLanding;
    public Transform finalLanding;
    public Door firstGate;
    public Door finalGate;
    public Transform exit;

    private int checkpoint;
    private bool completed;
    private float completedAt;
    private Camera view;
    private GUIStyle title;
    private GUIStyle text;
    private GUIStyle label;

    private void Awake()
    {
        Time.timeScale = 1f;
        view = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Menu();
        if (completed || player == null || player.IsRespawning)
            return;

        Vector2 position = player.transform.position;
        if (checkpoint < 1 && IsAtLanding(position, firstLanding))
            ActivateCheckpoint(1, firstLanding);
        if (checkpoint < 2 && IsAtLanding(position, finalLanding))
            ActivateCheckpoint(2, finalLanding);

        if (exit != null && position.x > exit.position.x - 0.45f &&
            Mathf.Abs(position.y - exit.position.y) < 1.5f && IsOpen(finalGate))
        {
            completed = true;
            completedAt = Time.timeSinceLevelLoad;
            PlaySessionStats.MarkCurrentLevelCompleted();
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<CorpseReleaseAbility>().enabled = false;
            Time.timeScale = 0f;
        }
    }

    private void LateUpdate()
    {
        // Fit the complete puzzle on narrow displays as well as 16:9.
        if (view != null)
            view.orthographicSize = Mathf.Max(8.8f, 16f / Mathf.Max(0.5f, view.aspect));
    }

    private static bool IsAtLanding(Vector2 position, Transform landing)
    {
        return landing != null && Mathf.Abs(position.x - landing.position.x) < 0.85f &&
            Mathf.Abs(position.y - landing.position.y) < 0.55f;
    }

    private void ActivateCheckpoint(int index, Transform landing)
    {
        checkpoint = index;
        player.respawnPoint = landing;
    }

    private static bool IsOpen(Door gate)
    {
        return gate != null && !gate.GetComponent<Collider2D>().enabled;
    }

    private void OnGUI()
    {
        if (view == null)
            return;
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            label = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            title.normal.textColor = text.normal.textColor = label.normal.textColor = Color.white;
        }

        Panel(new Rect(0, 0, Screen.width, 92));
        GUI.Label(new Rect(22, 12, 650, 38), "LEVEL_JA  /  BORROWED STEPS", title);
        GUI.Label(new Rect(22, 51, Screen.width - 40, 33),
            "A / D  move     Space  jump     E  release nearest corpse     Shift  red / blue     R  reset     Esc  menu", text);
        GUI.Label(new Rect(Screen.width - 275, 15, 260, 32),
            "CHECKPOINT " + checkpoint + "/2   |   DEATHS " + PlayerRespawnJ.DeathCount, label);

        WorldLabel(new Vector3(-10.5f, -1.25f, 0f), "01  BORROW A STEP");
        WorldLabel(new Vector3(-2.8f, 3.5f, 0f), "02  LAND, THEN LET GO");
        WorldLabel(new Vector3(9.1f, 6.7f, 0f), "03  REUSE THE LAST BRIDGE");
        WorldLabel(new Vector3(-5.9f, -6.6f, 0f), "A  /  CORPSE RECEIVER");
        WorldLabel(new Vector3(4.8f, -4.2f, 0f), "B  /  CORPSE RECEIVER");

        string hint = checkpoint == 0
            ? "The pale blocks are frozen corpses. Use both left steps to reach the green landing before pressing E."
            : checkpoint == 1 && !IsOpen(firstGate)
                ? "Checkpoint saved. From the left edge, press E to drop the upper corpse into receiver A. Shift to BLUE."
                : checkpoint < 2
                    ? "Gate A is open. Cross the last frozen corpse and land on the upper green checkpoint. Do not release it yet."
                    : !IsOpen(finalGate)
                        ? "Checkpoint saved. From the green landing, press E to drop the last bridge onto receiver B. Stay BLUE."
                        : "Both corpses now hold the gates open. Walk through the gold exit. R resets all three corpses.";
        Panel(new Rect(0, Screen.height - 75, Screen.width, 75));
        GUI.Label(new Rect(24, Screen.height - 61, Screen.width - 48, 52), hint, label);

        if (completed)
        {
            Panel(new Rect(0, 0, Screen.width, Screen.height), 0.88f);
            Rect center = new Rect(Screen.width / 2f - 270f, Screen.height / 2f - 100f, 540f, 240f);
            GUI.Label(new Rect(center.x, center.y, center.width, 45), "EVERY STEP HAD A SECOND PURPOSE.", label);
            GUI.Label(new Rect(center.x, center.y + 48, center.width, 40),
                "Complete  |  " + completedAt.ToString("F1") + "s  |  " + PlayerRespawnJ.DeathCount + " deaths", label);
            if (GUI.Button(new Rect(center.x + 80, center.y + 110, 180, 48), "PLAY AGAIN"))
                Restart();
            if (GUI.Button(new Rect(center.x + 280, center.y + 110, 180, 48), "MAIN MENU"))
                Menu();
        }
    }

    private void WorldLabel(Vector3 world, string caption)
    {
        Vector3 screen = view.WorldToScreenPoint(world);
        if (screen.z > 0f)
            GUI.Label(new Rect(screen.x - 180, Screen.height - screen.y - 18, 360, 36), caption, label);
    }

    private static void Panel(Rect rectangle, float opacity = 0.75f)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0.035f, 0.06f, 0.075f, opacity);
        GUI.DrawTexture(rectangle, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    private static void Menu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
