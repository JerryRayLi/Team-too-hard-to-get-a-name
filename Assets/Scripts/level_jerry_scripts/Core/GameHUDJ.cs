using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameHUDJ : MonoBehaviour
{
    private void Start()
    {
        string extra = gameObject.scene.name == "level_j2"
            ? GameHUD.CorpseControls + "\nSwitch Color: <b>Shift</b>"
            : null;
        GameHUD.CreateControls(transform, extra);
    }

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

        // Level name
        GUI.Label(
            new Rect(20, 15, 500, 48),
            LevelBackgroundTitle.GetDisplayTitle(levelName),
            titleStyle
        );

        // Death count
        GUI.Label(
            new Rect(Screen.width - 300, 15, 280, 48),
            "Deaths: " + PlaySessionStats.TotalDeaths,
            deathStyle
        );

        // Main menu button
        if (GUI.Button(
            new Rect(Screen.width - 200, 68, 180, 48),
            "MAIN MENU",
            buttonStyle))
        {
            ReturnToMainMenu();
        }

    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerRespawnJ.ResetDeathCount();
        SceneManager.LoadScene("MainMenu");
    }
}
