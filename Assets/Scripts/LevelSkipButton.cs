using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelSkipButton : MonoBehaviour
{
    private const string MainMenuScene = "MainMenu";
    private const string WinScene = "WinScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateButton()
    {
        if (FindFirstObjectByType<LevelSkipButton>() != null)
        {
            return;
        }

        GameObject buttonObject = new GameObject(nameof(LevelSkipButton));
        DontDestroyOnLoad(buttonObject);
        buttonObject.AddComponent<LevelSkipButton>();
    }

    private void OnGUI()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == MainMenuScene || activeScene.name == WinScene)
        {
            return;
        }

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = GameHUD.ScaleFont(20);

        float scale = GameHUD.UiScale;
        // Align below the scaled Main Menu button. Older scene-specific HUDs
        // still end their fixed-pixel menu at y=116, so also leave them a gap.
        Rect buttonRect = new Rect(Screen.width - 168f * scale,
            Mathf.Max(98f * scale, 124f), 152f * scale, 36f * scale);
        if (GUI.Button(buttonRect, "SKIP LEVEL", buttonStyle))
        {
            LoadNextLevel(activeScene.buildIndex);
        }
    }

    private static void LoadNextLevel(int currentBuildIndex)
    {
        PlaySessionStats.MarkCurrentLevelSkipped();

        int nextBuildIndex = currentBuildIndex + 1;
        if (nextBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            nextBuildIndex = 0;
        }

        SceneManager.LoadScene(nextBuildIndex);
    }
}
