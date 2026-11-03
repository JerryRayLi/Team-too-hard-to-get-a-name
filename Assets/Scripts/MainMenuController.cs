
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class LevelData
{
    public string levelName = "Level 1";
    public string sceneName;

#if UNITY_EDITOR
    public UnityEditor.SceneAsset sceneAsset;
#endif
}

[System.Serializable]
public class WorldData
{
    public string worldName = "New World";
    public LevelData[] levels;
    public bool isChallenge;
}

public class MainMenuController : MonoBehaviour
{
    [Header("Game Settings")]
    public string gameTitle = "Platformer Prototype";

    [Header("World Settings")]
    public WorldData[] worlds = new WorldData[6];

    private enum MenuPage
    {
        Main,
        WorldSelect,
        LevelSelect
    }

    private MenuPage currentPage = MenuPage.Main;
    private int selectedWorld = -1;

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;

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
            new Rect(0, Screen.height * 0.06f,
                Screen.width, 80),
            gameTitle,
            titleStyle
        );

        switch (currentPage)
        {
            case MenuPage.Main:
                DrawMainButtons();
                break;

            case MenuPage.WorldSelect:
                DrawWorldSelect();
                break;

            case MenuPage.LevelSelect:
                DrawLevelSelect();
                break;
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
        subtitleStyle.fontSize = 32;
        subtitleStyle.fontStyle = FontStyle.Bold;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 24;
        buttonStyle.richText = true;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 22;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.gray;
    }

    // Main menu
    private void DrawMainButtons()
    {
        float x = Screen.width / 2f - 110;
        float y = Screen.height / 2f - 80;

        if (GUI.Button(
            new Rect(x, y, 220, 55),
            "START",
            buttonStyle))
        {
            currentPage = MenuPage.WorldSelect;
        }

        if (GUI.Button(
            new Rect(x, y + 75, 220, 55),
            "LEVEL SELECT",
            buttonStyle))
        {
            currentPage = MenuPage.WorldSelect;
        }

        if (GUI.Button(
            new Rect(x, y + 150, 220, 55),
            "QUIT",
            buttonStyle))
        {
            QuitGame();
        }
    }

    // World selection: 3 columns x 2 rows
    private void DrawWorldSelect()
    {
        GUI.Label(
            new Rect(0, Screen.height * 0.22f,
                Screen.width, 50),
            "SELECT WORLD",
            subtitleStyle
        );

        float buttonWidth = 200;
        float buttonHeight = 65;
        float gapX = 25;
        float gapY = 25;

        int columns = 3;

        float totalWidth =
            columns * buttonWidth + (columns - 1) * gapX;

        float startX =
            Screen.width / 2f - totalWidth / 2f;

        float startY = Screen.height * 0.38f;

        if (worlds != null)
        {
            for (int i = 0; i < worlds.Length; i++)
            {
                WorldData world = worlds[i];

                int row = i / columns;
                int col = i % columns;

                float x = startX + col * (buttonWidth + gapX);
                float y = startY + row * (buttonHeight + gapY);

                bool available =
                    world != null &&
                    world.levels != null &&
                    world.levels.Length > 0;

                string worldName =
                    world != null &&
                    !string.IsNullOrWhiteSpace(world.worldName)
                    ? world.worldName
                    : "World " + (i + 1);

                string displayName = available
                    ? worldName
                    : worldName + "\nCOMING SOON";

                if (available && world.isChallenge)
                {
                    displayName =
                        worldName +
                        "\n<color=#E85D5D><b>CHALLENGE</b></color>";
                }

                GUI.enabled = available;

                if (GUI.Button(
                    new Rect(x, y, buttonWidth, buttonHeight),
                    displayName,
                    buttonStyle))
                {
                    selectedWorld = i;
                    currentPage = MenuPage.LevelSelect;
                }

                GUI.enabled = true;
            }
        }

        int rows = Mathf.CeilToInt(
            (worlds != null ? worlds.Length : 0) /
            (float)columns
        );

        float backY = startY +
            rows * (buttonHeight + gapY) + 15;

        if (GUI.Button(
            new Rect(Screen.width / 2f - 80,
                backY, 160, 50),
            "BACK",
            buttonStyle))
        {
            currentPage = MenuPage.Main;
        }
    }

    // Level selection inside a world
    private void DrawLevelSelect()
    {
        if (worlds == null ||
            selectedWorld < 0 ||
            selectedWorld >= worlds.Length ||
            worlds[selectedWorld] == null)
        {
            currentPage = MenuPage.WorldSelect;
            return;
        }

        WorldData world = worlds[selectedWorld];

        GUI.Label(
            new Rect(0, Screen.height * 0.22f,
                Screen.width, 50),
            world.worldName,
            subtitleStyle
        );

        if (world.levels == null)
            return;

        float buttonWidth = 180;
        float buttonHeight = 55;
        float gapX = 20;
        float gapY = 20;

        int columns = 3;

        float totalWidth =
            columns * buttonWidth + (columns - 1) * gapX;

        float startX =
            Screen.width / 2f - totalWidth / 2f;

        float startY = Screen.height * 0.38f;

        for (int i = 0; i < world.levels.Length; i++)
        {
            LevelData level = world.levels[i];

            int row = i / columns;
            int col = i % columns;

            float x = startX + col * (buttonWidth + gapX);
            float y = startY + row * (buttonHeight + gapY);

            bool available =
                level != null &&
                !string.IsNullOrWhiteSpace(level.sceneName);

            string levelName =
                level != null &&
                !string.IsNullOrWhiteSpace(level.levelName)
                ? level.levelName
                : "Level " + (i + 1);

            GUI.enabled = available;

            if (GUI.Button(
                new Rect(x, y, buttonWidth, buttonHeight),
                available ? levelName : "COMING SOON",
                buttonStyle))
            {
                LoadLevel(level.sceneName);
            }

            GUI.enabled = true;
        }

        int rows = Mathf.CeilToInt(
            world.levels.Length / (float)columns
        );

        float backY = startY +
            rows * (buttonHeight + gapY) + 15;

        if (GUI.Button(
            new Rect(Screen.width / 2f - 80,
                backY, 160, 50),
            "BACK",
            buttonStyle))
        {
            currentPage = MenuPage.WorldSelect;
        }
    }

    private void LoadLevel(string sceneName)
    {
        PlayerRespawn.ResetDeathCount();
        PlayerRespawnJ.ResetDeathCount();

        SceneManager.LoadScene(sceneName);
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
