using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private bool showLevelSelect;
    private Vector2 levelSelectScroll;

    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle categoryStyle;

    private void Start()
    {
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.08f, 0.10f, 0.16f);
        }
    }

    private void OnGUI()
    {
        CreateStyles();

        GUI.Label(
            new Rect(0, Screen.height * 0.05f, Screen.width, 70),
            "Color Blind Troll",
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
        titleStyle.fontSize = Mathf.Clamp(Screen.width / 26, 34, 48);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        subtitleStyle = new GUIStyle(GUI.skin.label);
        subtitleStyle.fontSize = 28;
        subtitleStyle.fontStyle = FontStyle.Bold;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.normal.textColor = Color.white;

        categoryStyle = new GUIStyle(GUI.skin.label);
        categoryStyle.fontSize = 20;
        categoryStyle.fontStyle = FontStyle.Bold;
        categoryStyle.alignment = TextAnchor.MiddleCenter;
        categoryStyle.normal.textColor = new Color(0.82f, 0.84f, 0.90f);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;
    }

    private void DrawMainButtons()
    {
        float x = Screen.width / 2f - 110;
        float y = Screen.height / 2f - 80;

        if (GUI.Button(new Rect(x, y, 220, 55), "START", buttonStyle))
        {
            LoadLevel("Level01");
        }

        if (GUI.Button(new Rect(x, y + 75, 220, 55), "LEVEL SELECT", buttonStyle))
        {
            showLevelSelect = true;
        }

        if (GUI.Button(new Rect(x, y + 150, 220, 55), "QUIT", buttonStyle))
        {
            QuitGame();
        }
    }

    private void DrawLevelSelect()
    {
        const float margin = 24f;
        float viewTop = Mathf.Max(115f, Screen.height * 0.17f);
        float viewHeight = Screen.height - viewTop - 75f;
        float contentWidth = Mathf.Min(940f, Screen.width - margin * 2f);
        float contentX = (Screen.width - contentWidth) / 2f;
        float contentHeight = 475f;

        Rect viewport = new Rect(contentX, viewTop, contentWidth, viewHeight);
        Rect content = new Rect(0, 0, contentWidth - 18f, contentHeight);
        levelSelectScroll = GUI.BeginScrollView(viewport, levelSelectScroll, content);

        GUI.Label(new Rect(0, 0, content.width, 42), "SELECT LEVEL", subtitleStyle);

        float tutorialGap = 14f;
        float tutorialButtonWidth = (content.width - tutorialGap * 3f) / 4f;
        float tutorialY = 58f;

        DrawLevelButton(new Rect(0, tutorialY, tutorialButtonWidth, 52), "LEVEL 1", "Level01");
        DrawLevelButton(new Rect((tutorialButtonWidth + tutorialGap), tutorialY, tutorialButtonWidth, 52), "LEVEL 2", "Level02");
        DrawLevelButton(new Rect((tutorialButtonWidth + tutorialGap) * 2f, tutorialY, tutorialButtonWidth, 52), "LEVEL 3", "Level03");
        DrawLevelButton(new Rect((tutorialButtonWidth + tutorialGap) * 3f, tutorialY, tutorialButtonWidth, 52), "LEVEL 4", "Level04");

        float categoryY = 145f;
        float categoryGap = 14f;
        float categoryWidth = (content.width - categoryGap * 3f) / 4f;
        float categoryHeight = 210f;

        DrawCategory(new Rect(0, categoryY, categoryWidth, categoryHeight), "SPIKES",
            new[] { "LEVEL 1" }, new[] { "Level05" });

        DrawCategory(new Rect(categoryWidth + categoryGap, categoryY, categoryWidth, categoryHeight), "CORPSE",
            new[] { "LEVEL 1", "LEVEL 2" }, new[] { "level_j1", "level_j2" });

        DrawCategory(new Rect((categoryWidth + categoryGap) * 2f, categoryY, categoryWidth, categoryHeight), "SLOW",
            new[] { "LEVEL 1" }, new[] { "Level_Haoyu" });

        DrawCategory(new Rect((categoryWidth + categoryGap) * 3f, categoryY, categoryWidth, categoryHeight), "DOOR",
            new[] { "LEVEL 1" }, new[] { "Level_Running_Door_1" });

        GUI.EndScrollView();

        if (GUI.Button(
            new Rect(Screen.width / 2f - 80f, Screen.height - 60f, 160f, 45f),
            "BACK",
            buttonStyle))
        {
            showLevelSelect = false;
        }
    }

    private void DrawCategory(Rect rect, string title, string[] labels, string[] sceneNames)
    {
        GUI.Box(rect, GUIContent.none);
        GUI.Label(new Rect(rect.x, rect.y + 10f, rect.width, 35f), title, categoryStyle);

        for (int i = 0; i < labels.Length; i++)
        {
            DrawLevelButton(
                new Rect(rect.x + 12f, rect.y + 55f + i * 62f, rect.width - 24f, 48f),
                labels[i],
                sceneNames[i]
            );
        }
    }

    private void DrawLevelButton(Rect rect, string label, string sceneName)
    {
        if (GUI.Button(rect, label, buttonStyle))
        {
            LoadLevel(sceneName);
        }
    }

    private void LoadLevel(string levelName)
    {
        PlayerRespawn.ResetDeathCount();
        PlayerRespawnJ.ResetDeathCount();
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
