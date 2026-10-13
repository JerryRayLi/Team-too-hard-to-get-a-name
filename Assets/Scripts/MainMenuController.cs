
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class LevelData
{
    public string levelName = "Level 1";
    public string sceneName = "";
}

[System.Serializable]
public class WorldData
{
    public string worldName = "New World";
    public LevelData[] levels = new LevelData[0];
}

public class MainMenuController : MonoBehaviour
{
    public string gameTitle = "Platformer Prototype";
    public WorldData[] worlds = new WorldData[6];

    private int menuPage = 0;
    private int selectedWorld = 0;

    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;

    private void OnGUI()
    {
        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 42;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;

        GUI.Label(
            new Rect(0, 40, Screen.width, 70),
            gameTitle,
            titleStyle
        );

        if (menuPage == 0)
            DrawMainMenu();
        else if (menuPage == 1)
            DrawWorldSelect();
        else
            DrawLevelSelect();
    }

    private void DrawMainMenu()
    {
        float x = Screen.width / 2f - 110;
        float y = Screen.height / 2f - 70;

        if (GUI.Button(
            new Rect(x, y, 220, 55),
            "START", buttonStyle))
        {
            if (worlds.Length > 0 &&
                worlds[0] != null &&
                worlds[0].levels != null &&
                worlds[0].levels.Length > 0 &&
                worlds[0].levels[0] != null)
            {
                LoadLevel(worlds[0].levels[0].sceneName);
            }
        }

        if (GUI.Button(
            new Rect(x, y + 75, 220, 55),
            "LEVEL SELECT", buttonStyle))
        {
            menuPage = 1;
        }

        if (GUI.Button(
            new Rect(x, y + 150, 220, 55),
            "QUIT", buttonStyle))
        {
            QuitGame();
        }
    }

    private void DrawWorldSelect()
    {
        float width = 200;
        float height = 60;
        float gap = 20;

        float startX = Screen.width / 2f - 320;
        float startY = Screen.height * 0.35f;

        for (int i = 0; i < worlds.Length; i++)
        {
            WorldData world = worlds[i];

            int col = i % 3;
            int row = i / 3;

            float x = startX + col * (width + gap);
            float y = startY + row * (height + gap);

            bool available = world != null &&
                world.levels != null &&
                world.levels.Length > 0;

            string name = world != null
                ? world.worldName
                : "World " + (i + 1);

            GUI.enabled = available;

            if (GUI.Button(
                new Rect(x, y, width, height),
                available ? name : "COMING SOON",
                buttonStyle))
            {
                selectedWorld = i;
                menuPage = 2;
            }

            GUI.enabled = true;
        }

        if (GUI.Button(
            new Rect(Screen.width / 2f - 80,
                startY + 190, 160, 50),
            "BACK", buttonStyle))
        {
            menuPage = 0;
        }
    }

    private void DrawLevelSelect()
    {
        if (selectedWorld < 0 ||
            selectedWorld >= worlds.Length ||
            worlds[selectedWorld] == null)
        {
            menuPage = 1;
            return;
        }

        WorldData world = worlds[selectedWorld];

        GUI.Label(
            new Rect(0, 130, Screen.width, 50),
            world.worldName,
            titleStyle
        );

        float width = 180;
        float height = 55;
        float gap = 20;

        float startX = Screen.width / 2f - 290;
        float startY = Screen.height * 0.35f;

        int count = world.levels != null
            ? world.levels.Length : 0;

        for (int i = 0; i < count; i++)
        {
            LevelData level = world.levels[i];

            int col = i % 3;
            int row = i / 3;

            float x = startX + col * (width + gap);
            float y = startY + row * (height + gap);

            if (level == null)
                continue;

            bool available =
                !string.IsNullOrWhiteSpace(level.sceneName);

            GUI.enabled = available;

            if (GUI.Button(
                new Rect(x, y, width, height),
                level.levelName,
                buttonStyle))
            {
                LoadLevel(level.sceneName);
            }

            GUI.enabled = true;
        }

        int rows = Mathf.CeilToInt(count / 3f);

        if (GUI.Button(
            new Rect(Screen.width / 2f - 80,
                startY + rows * 75 + 20, 160, 50),
            "BACK", buttonStyle))
        {
            menuPage = 1;
        }
    }

    private void LoadLevel(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        PlayerRespawn.ResetDeathCount();
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
