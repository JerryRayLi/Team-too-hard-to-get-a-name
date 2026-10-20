using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameComplete : MonoBehaviour
{
    private static readonly string[] SceneNames =
    {
        "Level01",
        "Level02",
        "Level03",
        "Level04",
        "Level_TIanyu1",
        "level_j1",
        "level_j2",
        "Level_Haoyu",
        "Level_Running_Door_1",
        "Level_Yulin_v1.1"
    };

    private static readonly string[] DisplayNames =
    {
        "LEVEL01",
        "LEVEL02",
        "LEVEL03",
        "LEVEL04",
        "SPIKES",
        "CORPSE01",
        "CORPSE02",
        "SLOW",
        "DOOR",
        "REVERSE"
    };

    private void Start()
    {
        if (Camera.main != null)
        {
            Camera.main.backgroundColor =
                new Color(0.08f, 0.10f, 0.16f);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            PlayerRespawn.ResetDeathCount();
            SceneManager.LoadScene(0);
        }
    }

    private void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.Clamp(Screen.height / 12, 38, 54);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = Mathf.Clamp(Screen.height / 24, 20, 28);
        bodyStyle.alignment = TextAnchor.MiddleCenter;
        bodyStyle.normal.textColor = Color.white;

        GUIStyle headerStyle = new GUIStyle(bodyStyle);
        headerStyle.fontStyle = FontStyle.Bold;

        GUIStyle rowStyle = new GUIStyle(bodyStyle);
        rowStyle.fontSize = Mathf.Clamp(Screen.height / 28, 18, 24);

        float panelWidth = Mathf.Min(760f, Screen.width - 40f);
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float titleY = 12f;
        float summaryY = 67f;
        float tableY = 105f;
        float rowHeight = Mathf.Clamp((Screen.height - 190f) / 11f, 24f, 33f);
        float panelHeight = rowHeight * 11f + 20f;

        GUI.Box(new Rect(panelX, tableY - 10f, panelWidth, panelHeight), "");

        GUI.Label(
            new Rect(0, titleY, Screen.width, 52f),
            "YOU WIN!",
            titleStyle
        );

        GUI.Label(
            new Rect(0, summaryY, Screen.width, 32f),
            "Total deaths: " + PlaySessionStats.TotalDeaths,
            bodyStyle
        );

        float levelColumnX = panelX + 24f;
        float deathsColumnX = panelX + panelWidth * 0.48f;
        float timeColumnX = panelX + panelWidth * 0.72f;
        float levelColumnWidth = panelWidth * 0.42f;
        float valueColumnWidth = panelWidth * 0.23f;

        GUI.Label(new Rect(levelColumnX, tableY, levelColumnWidth, rowHeight), "LEVEL", headerStyle);
        GUI.Label(new Rect(deathsColumnX, tableY, valueColumnWidth, rowHeight), "DEATHS", headerStyle);
        GUI.Label(new Rect(timeColumnX, tableY, valueColumnWidth, rowHeight), "BEST TIME", headerStyle);

        var allStats = PlaySessionStats.GetAllLevelStats();
        for (int i = 0; i < SceneNames.Length; i++)
        {
            float rowY = tableY + rowHeight * (i + 1);
            string deaths = "N/A";
            string duration = "N/A";

            if (allStats.TryGetValue(SceneNames[i], out PlaySessionStats.LevelStats stats))
            {
                deaths = stats.DeathsDisplay;
                duration = stats.DurationDisplay;
            }

            GUI.Label(new Rect(levelColumnX, rowY, levelColumnWidth, rowHeight), DisplayNames[i], rowStyle);
            GUI.Label(new Rect(deathsColumnX, rowY, valueColumnWidth, rowHeight), deaths, rowStyle);
            GUI.Label(new Rect(timeColumnX, rowY, valueColumnWidth, rowHeight), duration, rowStyle);
        }

        GUI.Label(
            new Rect(0, Screen.height - 48f, Screen.width, 36f),
            "Press R to return to the main menu",
            bodyStyle
        );
    }
}
