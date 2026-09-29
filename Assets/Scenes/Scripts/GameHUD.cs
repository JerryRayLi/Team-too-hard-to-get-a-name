using UnityEngine;
using UnityEngine.SceneManagement;

public class GameHUD : MonoBehaviour
{
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

        string levelName =
            SceneManager.GetActiveScene().name;

        GUI.Label(
            new Rect(20, 15, 400, 35),
            levelName,
            titleStyle
        );

        GUI.Label(
            new Rect(20, 50, 600, 30),
            "Move: A / D     Jump: Space     Restart: R",
            bodyStyle
        );

        if (levelName == "Level02" ||
            levelName == "Level03")
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
    }
}