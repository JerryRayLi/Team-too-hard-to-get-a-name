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
        titleStyle.fontSize = 28;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = 32;
        bodyStyle.richText = true;
        bodyStyle.normal.textColor = Color.white;

        GUIStyle deathStyle = new GUIStyle(bodyStyle);
        deathStyle.alignment = TextAnchor.MiddleRight;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;

        string levelName = SceneManager.GetActiveScene().name;

        GUI.Label(
            new Rect(20, 15, 400, 35),
            levelName,
            titleStyle
        );

        GUI.Label(
            new Rect(20, 50, 760, 40),
            "Move: <b>A / D</b>     Jump: <b>Space</b>     Restart: <b>R</b>",
            bodyStyle
        );

        if (levelName != "Level01")
        {
            GUI.Label(
                new Rect(20, 90, 600, 40),
                "Switch color: <b>Q</b>",
                bodyStyle
            );
        }

        GUI.Label(
            new Rect(Screen.width - 220, 15, 200, 30),
            "Deaths: " + PlaySessionStats.TotalDeaths,
            deathStyle
        );

        if (GUI.Button(
            new Rect(Screen.width - 170, 55, 150, 40),
            "MAIN MENU",
            buttonStyle))
        {
            ReturnToMainMenu();
        }

        GUI.Label(
            new Rect(20, Screen.height - 90, 400, 40),
            "<b>R</b>: Restart Level",
            bodyStyle
        );

        GUI.Label(
            new Rect(20, Screen.height - 50, 400, 40),
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
        hintStyle.fontSize = 32;
        hintStyle.fontStyle = FontStyle.Bold;
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);

        GUIStyle arrowStyle = new GUIStyle(hintStyle);
        arrowStyle.fontSize = 50;

        GUI.Label(
            new Rect(screenPoint.x - 260f, guiY - 142f, 520f, 42f),
            "Maybe try observe here first",
            hintStyle
        );

        GUI.Label(
            new Rect(screenPoint.x - 40f, guiY - 106f, 80f, 58f),
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
