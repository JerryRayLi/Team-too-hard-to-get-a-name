
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class MenuLevelData
{
    public string displayName = "";
    public string sceneName = "";

    // Only used by Unity Editor.
    // WebGL builds will not reference UnityEditor.
#if UNITY_EDITOR
    public UnityEditor.SceneAsset sceneAsset;
#endif
}

[System.Serializable]
public class MenuWorldData
{
    public string worldName = "New World";
    public MenuLevelData[] levels = new MenuLevelData[0];
}

public class MainMenuController : MonoBehaviour
{
    [Header("Game Settings")]
    public string gameTitle = "Platformer Prototype";

    [Header("World Settings")]
    public MenuWorldData[] worlds = new MenuWorldData[6];

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
        subtitleStyle.fontSize = 28;
        subtitleStyle.fontStyle = FontStyle.Bold;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;
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
            StartGame();
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

    private void StartGame()
    {
        if (worlds == null || worlds.Length == 0)
            return;

        MenuWorldData firstWorld = worlds[0];

        if (firstWorld == null ||
            firstWorld.levels == null ||
            firstWorld.levels.Length == 0)
            return;

        MenuLevelData firstLevel = firstWorld.levels[0];

        if (firstLevel != null &&
            !string.IsNullOrWhiteSpace(firstLevel.sceneName))
        {
<<<<<<< Updated upstream
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
        Screen.width / 2f - buttonWidth / 2f,
        startY + 2 * (buttonHeight + gap),
        buttonWidth,
        buttonHeight),
    "LEVEL 5",
    buttonStyle))
        {
            LoadLevel("Level05");
        }

        if (GUI.Button(
            new Rect(
                Screen.width / 2f - 80,
                startY + 3 * (buttonHeight + gap),
                160,
                50),
            "BACK",
            buttonStyle))
        {
            showLevelSelect = false;
=======
            LoadLevel(firstLevel.sceneName);
>>>>>>> Stashed changes
        }
    }

    private void DrawWorldSelect()
    {
        GUI.Label(
            new Rect(0, Screen.height * 0.22f,
                Screen.width, 50),
            "SELECT WORLD",
            subtitleStyle
        );

        float width = 200;
        float height = 65;
        float gapX = 25;
        float gapY = 25;

        int columns = 3;
        float totalWidth = columns * width +
            (columns - 1) * gapX;

        float startX = Screen.width / 2f -
            totalWidth / 2f;

        float startY = Screen.height * 0.38f;

        int count = worlds != null ? worlds.Length : 0;

        for (int i = 0; i < count; i++)
        {
            MenuWorldData world = worlds[i];

            int row = i / columns;
            int col = i % columns;

            float x = startX + col * (width + gapX);
            float y = startY + row * (height + gapY);

            bool available = HasAvailableLevels(world);

            string name = world != null &&
                !string.IsNullOrWhiteSpace(world.worldName)
                ? world.worldName
                : "World " + (i + 1);

            GUI.enabled = available;

            if (GUI.Button(
                new Rect(x, y, width, height),
                available ? name : name + "\nCOMING SOON",
                buttonStyle))
            {
                selectedWorld = i;
                currentPage = MenuPage.LevelSelect;
            }

            GUI.enabled = true;
        }

        int rows = Mathf.CeilToInt(count / (float)columns);
        float backY = startY +
            rows * (height + gapY) + 15;

        if (GUI.Button(
            new Rect(Screen.width / 2f - 80,
                backY, 160, 50),
            "BACK",
            buttonStyle))
        {
            currentPage = MenuPage.Main;
        }
    }

    private bool HasAvailableLevels(MenuWorldData world)
    {
        if (world == null || world.levels == null)
            return false;

        foreach (MenuLevelData level in world.levels)
        {
            if (level != null &&
                !string.IsNullOrWhiteSpace(level.sceneName))
                return true;
        }

        return false;
    }

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

        MenuWorldData world = worlds[selectedWorld];

        GUI.Label(
            new Rect(0, Screen.height * 0.22f,
                Screen.width, 50),
            world.worldName,
            subtitleStyle
        );

        float width = 180;
        float height = 55;
        float gapX = 20;
        float gapY = 20;

        int columns = 3;

        float totalWidth = columns * width +
            (columns - 1) * gapX;

        float startX = Screen.width / 2f -
            totalWidth / 2f;

        float startY = Screen.height * 0.38f;

        int count = world.levels != null
            ? world.levels.Length : 0;

        for (int i = 0; i < count; i++)
        {
            MenuLevelData level = world.levels[i];

            int row = i / columns;
            int col = i % columns;

            float x = startX + col * (width + gapX);
            float y = startY + row * (height + gapY);

            bool available = level != null &&
                !string.IsNullOrWhiteSpace(level.sceneName);

            string name = level != null &&
                !string.IsNullOrWhiteSpace(level.displayName)
                ? level.displayName
                : "Level " + (i + 1);

            GUI.enabled = available;

            if (GUI.Button(
                new Rect(x, y, width, height),
                available ? name : "COMING SOON",
                buttonStyle))
            {
                LoadLevel(level.sceneName);
            }

            GUI.enabled = true;
        }

        int rows = Mathf.CeilToInt(count / (float)columns);
        float backY = startY +
            rows * (height + gapY) + 15;

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
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        // Reset the original game's death counter.
        PlayerRespawn.ResetDeathCount();

        // Jerry's counter is reset when the level starts.
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
