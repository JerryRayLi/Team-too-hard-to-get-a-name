using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameHUD : MonoBehaviour
{
    private const int Level03HintDeathThreshold = 5;

    private Vector3 level03RespawnPosition;
    private bool hasLevel03RespawnPosition;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "Level03")
        {
            return;
        }

        PlayerRespawn player = FindFirstObjectByType<PlayerRespawn>();
        if (player != null)
        {
            level03RespawnPosition = player.transform.position;
            hasLevel03RespawnPosition = true;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToMainMenu();
        }
    }

    private void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 36;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = 40;
        bodyStyle.richText = true;
        bodyStyle.normal.textColor = Color.white;

        GUIStyle deathStyle = new GUIStyle(bodyStyle);
        deathStyle.alignment = TextAnchor.MiddleRight;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 26;

        string levelName = SceneManager.GetActiveScene().name;

        GUI.Label(
            new Rect(20, 15, 500, 48),
            LevelBackgroundTitle.GetDisplayTitle(levelName),
            titleStyle
        );

        GUI.Label(
            new Rect(20, 60, 940, 52),
            "Move: <b>A / D</b>     Jump: <b>Space</b>     Restart: <b>R</b>",
            bodyStyle
        );

        if (levelName != "Level01")
        {
            GUI.Label(
                new Rect(20, 108, 760, 52),
                "Switch color: <b>Shift</b>",
                bodyStyle
            );
        }

        GUI.Label(
            new Rect(Screen.width - 300, 15, 280, 48),
            "Deaths: " + PlaySessionStats.TotalDeaths,
            deathStyle
        );

        if (GUI.Button(
            new Rect(Screen.width - 200, 68, 180, 48),
            "MAIN MENU",
            buttonStyle))
        {
            ReturnToMainMenu();
        }

        GUI.Label(
            new Rect(20, Screen.height - 112, 520, 52),
            "<b>R</b>: Restart Level",
            bodyStyle
        );

        GUI.Label(
            new Rect(20, Screen.height - 60, 520, 52),
            "<b>Esc</b>: Main Menu",
            bodyStyle
        );

        if (levelName == "Level03" &&
            PlaySessionStats.GetCurrentLevelDeaths() >= Level03HintDeathThreshold)
        {
            DrawLevel03RespawnHint();
        }
    }

    private void DrawLevel03RespawnHint()
    {
        if (!hasLevel03RespawnPosition || Camera.main == null)
        {
            return;
        }

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(level03RespawnPosition);
        if (screenPoint.z <= 0f)
        {
            return;
        }

        float guiY = Screen.height - screenPoint.y;

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 40;
        hintStyle.fontStyle = FontStyle.Bold;
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);

        GUIStyle arrowStyle = new GUIStyle(hintStyle);
        arrowStyle.fontSize = 60;

        GUI.Label(
            new Rect(screenPoint.x - 320f, guiY - 160f, 640f, 52f),
            "Maybe try observe here first",
            hintStyle
        );

        GUI.Label(
            new Rect(screenPoint.x - 45f, guiY - 112f, 90f, 68f),
            "▼",
            arrowStyle
        );
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerRespawn.ResetDeathCount();
        SceneManager.LoadScene("MainMenu");
    }
}
