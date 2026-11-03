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
        buttonStyle.fontSize = 26;

        if (GUI.Button(new Rect(20f, 168f, 180f, 48f), "SKIP LEVEL", buttonStyle))
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
