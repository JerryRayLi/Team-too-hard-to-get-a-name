using UnityEngine;
using UnityEngine.SceneManagement;

public class HiddenPlatformHint : MonoBehaviour
{
    private const string TargetScene = "Level_Yulin_hidden1";

    public static HiddenPlatformHint Instance { get; private set; }

    [Header("Target")]
    public HiddenPlatform firstPlatform;

    [Header("Hint Settings")]
    [Min(1)] public int deathThreshold = 4;
    [Min(0.1f)] public float successDuration = 3f;

    private static bool firstPlatformFound;
    private static bool hintWasShown;
    private float successUntil;
    private GUIStyle textStyle;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        // Reset static state when a new Play session starts.
        Instance = null;
        firstPlatformFound = false;
        hintWasShown = false;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(
        Scene scene, LoadSceneMode mode)
    {
        // Keep discovery across deaths, but clear it on leaving the level.
        if (mode == LoadSceneMode.Single && scene.name != TargetScene)
        {
            firstPlatformFound = false;
            hintWasShown = false;
        }
    }

    private void Awake()
    {
        if (gameObject.scene.name != TargetScene)
        {
            enabled = false;
            return;
        }

        Instance = this;
    }

    public void NotifyPlatformFound(HiddenPlatform platform)
    {
        if (firstPlatformFound ||
            firstPlatform == null ||
            platform != firstPlatform)
        {
            return;
        }

        firstPlatformFound = true;

        // Show success only if the guidance appeared first.
        successUntil = hintWasShown
        ? Time.unscaledTime + successDuration
        : 0f;
    }

    private void OnGUI()
    {
        string message;

        if (firstPlatformFound)
        {
            if (Time.unscaledTime >= successUntil)
            {
                return;
            }

            message = "You got it!";
        }
        else
        {
            if (PlaySessionStats.GetCurrentLevelDeaths() < deathThreshold)
            {
                return;
            }

            message = "Try a leap of faith.";
        }

        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
        }

        // Scale the message to fit the current Game window.
        float scale = Mathf.Min(
            Screen.width / 1280f,
            Screen.height / 720f
        );

        textStyle.fontSize = Mathf.Max(
            16, Mathf.RoundToInt(30f * scale)
        );
        textStyle.normal.textColor = Color.white;

        float width = Mathf.Min(560f * scale, Screen.width - 32f);
        float height = Mathf.Max(48f, 64f * scale);

        Rect area = new Rect(
            (Screen.width - width) / 2f,
            Screen.height * 0.28f,
            width,
            height
        );

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUI.Label(area, message, textStyle);

        // Remember that the guidance has been displayed.
        if (!firstPlatformFound)
        {
            hintWasShown = true;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}