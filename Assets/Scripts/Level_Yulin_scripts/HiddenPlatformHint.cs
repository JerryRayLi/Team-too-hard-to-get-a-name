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

        if (!firstPlatformFound && Event.current.type == EventType.Repaint)
        {
            DrawPlatformOutline(scale);
        }

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

    private void DrawPlatformOutline(float scale)
    {
        Camera camera = Camera.main;
        if (firstPlatform == null || camera == null)
        {
            return;
        }

        Bounds bounds = firstPlatform.GetComponent<BoxCollider2D>().bounds;
        Vector3 bottomLeft = camera.WorldToScreenPoint(bounds.min);
        Vector3 topRight = camera.WorldToScreenPoint(bounds.max);
        if (bottomLeft.z <= 0f || topRight.z <= 0f)
        {
            return;
        }

        float padding = Mathf.Max(2f, 5f * scale);
        float left = bottomLeft.x - padding;
        float right = topRight.x + padding;
        float top = Screen.height - topRight.y - padding;
        float bottom = Screen.height - bottomLeft.y + padding;
        float thickness = Mathf.Max(2f, 3f * scale);
        float dash = Mathf.Max(6f, 12f * scale);
        float gap = Mathf.Max(3f, 7f * scale);

        Color previousColor = GUI.color;
        GUI.color = Color.white;
        for (float x = left; x < right; x += dash + gap)
        {
            float length = Mathf.Min(dash, right - x);
            GUI.DrawTexture(new Rect(x, top, length, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, bottom - thickness, length, thickness), Texture2D.whiteTexture);
        }

        for (float y = top; y < bottom; y += dash + gap)
        {
            float length = Mathf.Min(dash, bottom - y);
            GUI.DrawTexture(new Rect(left, y, thickness, length), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(right - thickness, y, thickness, length), Texture2D.whiteTexture);
        }

        GUI.color = previousColor;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
