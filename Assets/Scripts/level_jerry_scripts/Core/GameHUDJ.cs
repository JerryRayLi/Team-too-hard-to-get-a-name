using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameHUDJ : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null)
            return;

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

        // Level name
        GUI.Label(
            new Rect(20, 15, 400, 35),
            levelName,
            titleStyle
        );

        // Basic controls
        string controls = "Move: <b>A / D</b>     Jump: <b>Space</b>     Restart: <b>R</b>";

        GUI.Label(
            new Rect(20, 50, 760, 40),
            controls,
            bodyStyle
        );

        // Level 2 has the corpse release ability.
        if (levelName == "level_j2")
        {
            GUI.Label(
                new Rect(20, 90, 600, 40),
                "Release Corpse: <b>E</b>     Switch Color: <b>Q</b>",
                bodyStyle
            );
        }

        // Death count
        GUI.Label(
            new Rect(Screen.width - 220, 15, 200, 30),
            "Deaths: " + PlaySessionStats.TotalDeaths,
            deathStyle
        );

        // Main menu button
        if (GUI.Button(
            new Rect(Screen.width - 170, 55, 150, 40),
            "MAIN MENU",
            buttonStyle))
        {
            ReturnToMainMenu();
        }

        // Bottom instructions
        GUI.Label(
            new Rect(20, Screen.height - 50, 400, 40),
            "<b>Esc</b>: Main Menu",
            bodyStyle
        );
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerRespawnJ.ResetDeathCount();
        SceneManager.LoadScene("MainMenu");
    }
}
