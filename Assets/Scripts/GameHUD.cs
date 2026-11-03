using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameHUD : MonoBehaviour
{
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
                "Switch color: <b>Shift / K / Right Click</b>",
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

        if (levelName == "Level01")
        {
            GUIStyle initiationStyle = new GUIStyle(bodyStyle);
            initiationStyle.alignment = TextAnchor.MiddleCenter;
            initiationStyle.fontSize = 32;
            initiationStyle.normal.textColor = new Color(1f, 1f, 1f, 0.82f);
            GUI.Label(
                new Rect(Screen.width * 0.2f, Screen.height - 125f,
                    Screen.width * 0.6f, 54f),
                "This game teaches through failure. Observe, die, adapt.",
                initiationStyle
            );
        }
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerRespawn.ResetDeathCount();
        SceneManager.LoadScene("MainMenu");
    }
}
