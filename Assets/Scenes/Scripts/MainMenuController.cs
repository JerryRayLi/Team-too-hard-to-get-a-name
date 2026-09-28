using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.Experimental.GraphView.GraphView;

public class MainMenuController : MonoBehaviour
{
    private bool showLevelSelect;

    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle subtitleStyle;

    private void Start()
    {
        if (Camera.main != null)
        {
            Camera.main.backgroundColor =
                new Color(0.08f, 0.10f, 0.16f);
        }
    }

    private void OnGUI()
    {
        CreateStyles();

        GUI.Label(
        new Rect(0, Screen.height * 0.06f, Screen.width, 80),
            "LAYERED LIES",
            titleStyle
        );

        if (showLevelSelect)
        {
            DrawLevelSelect();
        }
        else
        {
            DrawMainButtons();
        }
    }

    private void CreateStyles()
    {
        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 48;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        subtitleStyle = new GUIStyle(GUI.skin.label);
        subtitleStyle.fontSize = 28;
        subtitleStyle.fontStyle = FontStyle.Bold;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 22;
    }

    private void DrawMainButtons()
    {
        float x = Screen.width / 2f - 110;
        float y = Screen.height / 2f - 80;

        if (GUI.Button(
            new Rect(x, y, 220, 55),
            "START",
            buttonStyle))
        {
            LoadLevel("Level01");
        }

        if (GUI.Button(
            new Rect(x, y + 75, 220, 55),
            "LEVEL SELECT",
            buttonStyle))
        {
            showLevelSelect = true;
        }

        if (GUI.Button(
            new Rect(x, y + 150, 220, 55),
            "QUIT",
            buttonStyle))
        {
            QuitGame();
        }
    }

    private void DrawLevelSelect()
    {
        GUI.Label(
            new Rect(
            0,
            Screen.height * 0.24f,
            Screen.width,
            50
        ),
            "SELECT LEVEL",
            subtitleStyle
        );

        float buttonWidth = 150;
        float buttonHeight = 55;
        float gap = 20;

        float totalWidth =
            buttonWidth * 2 + gap;

        float startX =
            Screen.width / 2f - totalWidth / 2f;

        float startY =Screen.height * 0.38f;

        if (GUI.Button(
            new Rect(startX, startY, buttonWidth, buttonHeight),
            "LEVEL 1",
            buttonStyle))
        {
            LoadLevel("Level01");
        }

        if (GUI.Button(
            new Rect(
                startX + buttonWidth + gap,
                startY,
                buttonWidth,
                buttonHeight),
            "LEVEL 2",
            buttonStyle))
        {
            LoadLevel("Level02");
        }

        if (GUI.Button(
            new Rect(
                startX,
                startY + buttonHeight + gap,
                buttonWidth,
                buttonHeight),
            "LEVEL 3",
            buttonStyle))
        {
            LoadLevel("Level03");
        }

        if (GUI.Button(
            new Rect(
                startX + buttonWidth + gap,
                startY + buttonHeight + gap,
                buttonWidth,
                buttonHeight),
            "LEVEL 4",
            buttonStyle))
        {
            LoadLevel("Level04");
        }

        if (GUI.Button(
            new Rect(
                Screen.width / 2f - 80,
                startY + 2 * (buttonHeight + gap),
                160,
                50),
            "BACK",
            buttonStyle))
        {
            showLevelSelect = false;
        }
    }

    private void LoadLevel(string levelName)
    {
        PlayerRespawn.ResetDeathCount();
        SceneManager.LoadScene(levelName);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}   