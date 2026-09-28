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

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartLevel();
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
        titleStyle.fontSize = 24;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = 18;
        bodyStyle.normal.textColor = Color.white;

        GUIStyle deathStyle = new GUIStyle(bodyStyle);
        deathStyle.alignment = TextAnchor.MiddleRight;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 16;

        string levelName =
            SceneManager.GetActiveScene().name;

        GUI.Label(
            new Rect(20, 15, 400, 35),
            levelName,
            titleStyle
        );

        GUI.Label(
            new Rect(20, 50, 600, 30),
            "Move: A / D     Jump: Space",
            bodyStyle
        );

        if (levelName != "Level01")
        {
            GUI.Label(
                new Rect(20, 78, 600, 30),
                "Switch color: Q / E",
                bodyStyle
            );
        }

        GUI.Label(
            new Rect(Screen.width - 220, 15, 200, 30),
            "Deaths: " + PlayerRespawn.DeathCount,
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
            new Rect(20, Screen.height - 70, 300, 30),
            "R: Restart Level",
            bodyStyle
        );

        GUI.Label(
            new Rect(20, Screen.height - 42, 300, 30),
            "Esc: Main Menu",
            bodyStyle
        );
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;

        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(currentScene.name);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerRespawn.ResetDeathCount();
        SceneManager.LoadScene("MainMenu");
    }
}