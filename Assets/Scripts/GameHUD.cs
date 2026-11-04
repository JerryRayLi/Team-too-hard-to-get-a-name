using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    private GameObject hints;

    private void OnEnable()
    {
        if (hints == null)
        {
            string level = gameObject.scene.name;
            string extra = GetExtraControls(level);
            hints = CreateControls(transform, extra, showMenu: true);
            if (level == "Level01")
            {
                AddHint(hints.transform, "Introduction", new Vector2(0.5f, 0),
                    new Vector2(0, 28), new Vector2(930, 140), 46, TextAnchor.MiddleCenter,
                    "This game teaches through failure.\nObserve, die, adapt.");
            }
        }

        hints.SetActive(true);
    }

    private void OnDisable()
    {
        if (hints != null)
        {
            hints.SetActive(false);
        }
    }

    internal const string ColorControls = "Switch color: <b>Shift / K / Right Click</b>";
    internal const string CorpseControls = "Release Corpse: <b>E</b>";

    // Use the same reference size for Canvas text and IMGUI headers/labels.
    internal static float UiScale => Mathf.Max(0.1f,
        Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
    internal static int ScaleFont(int size) => Mathf.Max(14, Mathf.RoundToInt(size * UiScale));

    internal static string GetExtraControls(string level)
    {
        if (level == "Level01" || level == "level_j1")
            return null;
        return level == "level_j2" || level == "level_j3"
            ? CorpseControls + "\n" + ColorControls : ColorControls;
    }

    internal static GameObject CreateControls(Transform owner, string extra = null, bool showMenu = true)
    {
        Scene scene = owner != null ? owner.gameObject.scene : SceneManager.GetActiveScene();
        // A scene HUD and the standalone scene hook can both request controls.
        // Reuse the scene's panel regardless of which caller runs first.
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                if (canvas.name == "Gameplay Hints")
                    return canvas.gameObject;

        GameObject hints = new GameObject("Gameplay Hints", typeof(Canvas), typeof(CanvasScaler));
        hints.transform.SetParent(owner, false);
        hints.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = hints.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        string controls = "Move: <b>A / D</b>    Jump: <b>Space</b>\nRestart: <b>R</b>";
        if (showMenu)
        {
            controls += "    Menu: <b>Esc</b>";
        }

        if (extra != null)
        {
            controls += "\n" + extra;
        }

        float height = 16f + 35f * controls.Split('\n').Length;
        AddHint(hints.transform, "Controls", new Vector2(0, 1), new Vector2(16, -56),
            new Vector2(550, height), 28, TextAnchor.UpperLeft, controls);
        return hints;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterStandaloneHints()
    {
        SceneManager.sceneLoaded -= AddStandaloneHints;
        SceneManager.sceneLoaded += AddStandaloneHints;
    }

    private static void AddStandaloneHints(Scene scene, LoadSceneMode mode)
    {
        GameObject hints;
        switch (scene.name)
        {
            case "Level_Yulin_v1.1":
                hints = CreateControls(null);
                break;
            case "Level_Running_Door_1":
            case "Level_Running_Door_2":
                hints = CreateControls(null, ColorControls, showMenu: false);
                break;
            case "level_j3":
                // The HUD prefab already creates this panel. The factory also
                // handles scenes where that prefab is missing.
                hints = CreateControls(null, GetExtraControls(scene.name));
                break;
            default:
                return;
        }
        if (hints.transform.parent == null)
            SceneManager.MoveGameObjectToScene(hints, scene);
    }

    private static void AddHint(Transform parent, string name, Vector2 anchor, Vector2 position,
        Vector2 size, int fontSize, TextAnchor alignment, string message)
    {
        GameObject panel = new GameObject(name, typeof(Image));
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.SetParent(parent, false);
        panelRect.anchorMin = anchor;
        panelRect.anchorMax = anchor;
        panelRect.pivot = anchor;
        panelRect.anchoredPosition = position;
        panelRect.sizeDelta = size;

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.05f, 0.09f, 0.11f, 0.58f);
        background.raycastTarget = false;

        GameObject label = new GameObject("Text", typeof(Text));
        RectTransform labelRect = (RectTransform)label.transform;
        labelRect.SetParent(panelRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12, 8);
        labelRect.offsetMax = new Vector2(-12, -8);

        Text text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.text = message;
    }

    private void Update()
    {
        if (Keyboard.current == null)
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
        DrawHeader(ReturnToMainMenu);
    }

    internal static void DrawHeader(System.Action returnToMainMenu)
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * UiScale);
        float width = Screen.width / UiScale;
        try
        {
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 30;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;

            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.fontSize = 28;
            bodyStyle.richText = true;
            bodyStyle.normal.textColor = Color.white;

            GUIStyle deathStyle = new GUIStyle(bodyStyle);
            deathStyle.alignment = TextAnchor.MiddleRight;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 20;

            string levelName = SceneManager.GetActiveScene().name;

            GUI.Label(
                new Rect(16, 10, 600, 38),
                LevelBackgroundTitle.GetDisplayTitle(levelName),
                titleStyle
            );

            GUI.Label(
                new Rect(width - 240, 10, 224, 38),
                "Deaths: " + PlaySessionStats.TotalDeaths,
                deathStyle
            );

            if (GUI.Button(
                new Rect(width - 168, 54, 152, 36),
                "MAIN MENU",
                buttonStyle))
            {
                returnToMainMenu();
            }
        }
        finally
        {
            GUI.matrix = previousMatrix;
        }
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerRespawn.ResetDeathCount();
        SceneManager.LoadScene("MainMenu");
    }
}
