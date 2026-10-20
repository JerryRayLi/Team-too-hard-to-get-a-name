using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelExit : MonoBehaviour
{
    private bool completed;
    private float completionTime;
    private int nextScene;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (completed)
            return;

        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        completed = true;
        completionTime = Time.timeSinceLevelLoad;

        nextScene = SceneManager.GetActiveScene().buildIndex + 1;

        player.enabled = false;

        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null)
            respawn.enabled = false;

        ColorLayerManager colors =
            FindFirstObjectByType<ColorLayerManager>();

        if (colors != null)
            colors.enabled = false;


        GameHUD hud = FindFirstObjectByType<GameHUD>();
        if (hud != null)
            hud.enabled = false;

        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (!completed || Keyboard.current == null)
            return;

        if (Keyboard.current.enterKey.wasPressedThisFrame)
            Continue();
    }

    private void OnGUI()
    {
        if (!completed)
            return;

        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 960f,
                                Screen.height / 540f);

        GUI.matrix = Matrix4x4.TRS(
            new Vector3(
                (Screen.width - 960f * scale) / 2f,
                (Screen.height - 540f * scale) / 2f,
                0f),
            Quaternion.identity,
            Vector3.one * scale
        );

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);

        GUI.DrawTexture(
            new Rect(
                -(Screen.width / scale - 960f) / 2f,
                -(Screen.height / scale - 540f) / 2f,
                Screen.width / scale,
                Screen.height / scale),
            Texture2D.whiteTexture
        );

        GUI.color = previousColor;

        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        title.normal.textColor = new Color(1f, 0.85f, 0.3f);

        GUIStyle text = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter
        };
        text.normal.textColor = Color.white;

        GUIStyle button = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22
        };

        GUI.Label(new Rect(80, 105, 800, 75),
                  "LEVEL COMPLETE!", title);

        GUI.Label(new Rect(80, 180, 800, 40),
                  "You made it through the exit.", text);

        int minutes = Mathf.FloorToInt(completionTime / 60f);
        int seconds = Mathf.FloorToInt(completionTime % 60f);

        GUI.Label(new Rect(80, 230, 800, 40),
                  $"Time: {minutes:00}:{seconds:00}    " +
                  $"Total deaths: {PlayerRespawn.DeathCount}",
                  text);

        if (GUI.Button(new Rect(350, 295, 260, 55),
                       "CONTINUE", button))
            Continue();

        if (GUI.Button(new Rect(350, 365, 125, 50),
                       "RETRY", button))
            LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (GUI.Button(new Rect(485, 365, 125, 50),
                       "MENU", button))
        {
            PlayerRespawn.ResetDeathCount();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        GUI.Label(new Rect(80, 440, 800, 35),
                  "Press Enter to continue", text);

        GUI.matrix = previousMatrix;
    }

    private void Continue()
    {
        if (nextScene < SceneManager.sceneCountInBuildSettings)
        {
            LoadScene(nextScene);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void LoadScene(int index)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(index);
    }

    private void OnDestroy()
    {
        if (completed)
            Time.timeScale = 1f;
    }
}