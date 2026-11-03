using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class ReverseLevelHUD : MonoBehaviour
{
    private const string ReverseSceneName = "Level_Yulin_v1.1";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<ReverseLevelHUD>() != null)
        {
            return;
        }

        GameObject hudObject = new GameObject(nameof(ReverseLevelHUD));
        DontDestroyOnLoad(hudObject);
        hudObject.AddComponent<ReverseLevelHUD>();
    }

    private void Update()
    {
        if (!IsReverseScene() || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToMainMenu();
        }
    }

    private void OnGUI()
    {
        if (!IsReverseScene())
        {
            return;
        }

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = Color.white;

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 40,
            richText = true
        };
        bodyStyle.normal.textColor = Color.white;

        GUIStyle deathStyle = new GUIStyle(bodyStyle)
        {
            alignment = TextAnchor.MiddleRight
        };

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 26
        };

        GUI.Label(
            new Rect(20, 15, 500, 48),
            LevelBackgroundTitle.GetDisplayTitle(ReverseSceneName),
            titleStyle
        );

        GUI.Label(
            new Rect(20, 60, 940, 52),
            "Move: <b>A / D</b>     Jump: <b>Space</b>     Restart: <b>R</b>",
            bodyStyle
        );

        GUI.Label(
            new Rect(Screen.width - 300, 15, 280, 48),
            "Deaths: " + PlaySessionStats.TotalDeaths,
            deathStyle
        );

        if (GUI.Button(
            new Rect(Screen.width - 200, 68, 180, 48),
            "MAIN MENU",
            buttonStyle))
        {
            ReturnToMainMenu();
        }

        GUI.Label(
            new Rect(20, Screen.height - 112, 520, 52),
            "<b>R</b>: Restart Level",
            bodyStyle
        );

        GUI.Label(
            new Rect(20, Screen.height - 60, 520, 52),
            "<b>Esc</b>: Main Menu",
            bodyStyle
        );
    }

    private static bool IsReverseScene()
    {
        return SceneManager.GetActiveScene().name == ReverseSceneName;
    }

    private static void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerRespawn.ResetDeathCount();
        SceneManager.LoadScene("MainMenu");
    }
}
