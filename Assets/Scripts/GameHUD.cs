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
            string extra = level == "Level01" ? null : ColorControls;
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

    internal static GameObject CreateControls(Transform owner, string extra = null, bool showMenu = true)
    {
        GameObject hints = new GameObject("Gameplay Hints", typeof(Canvas), typeof(CanvasScaler));
        hints.transform.SetParent(owner, false);
        hints.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = hints.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        string controls = "Move: <b>A / D</b>    Jump: <b>Space</b>\nRestart: <b>R</b>";
        if (showMenu)
        {
            controls += "    Menu: <b>Esc</b>";
        }

        if (extra != null)
        {
            controls += "\n" + extra;
        }

        float height = extra == null ? 156 : 156 + 62 * (extra.Split('\n').Length);
        AddHint(hints.transform, "Controls", new Vector2(0.5f, 1), new Vector2(0, -112),
            new Vector2(900, height), 54, TextAnchor.MiddleLeft, controls);
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
        switch (scene.name)
        {
            case "Level_Yulin_v1.1":
                SceneManager.MoveGameObjectToScene(CreateControls(null), scene);
                break;
            case "Level_Running_Door_1":
            case "Level_Running_Door_2":
                SceneManager.MoveGameObjectToScene(CreateControls(null, ColorControls, showMenu: false), scene);
                break;
            case "level_j3":
                SceneManager.MoveGameObjectToScene(CreateControls(null, CorpseControls + "\n" + ColorControls, showMenu: false), scene);
                break;
        }
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
        background.color = new Color(0.05f, 0.09f, 0.11f, 0.95f);
        background.raycastTarget = false;

        GameObject label = new GameObject("Text", typeof(Text));
        RectTransform labelRect = (RectTransform)label.transform;
        labelRect.SetParent(panelRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20, 8);
        labelRect.offsetMax = new Vector2(-20, -8);

        Text text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
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
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 36;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = 40;
        bodyStyle.richText = true;
        bodyStyle.normal.textColor = Color.white;

        GUIStyle deathStyle = new GUIStyle(bodyStyle);
        deathStyle.alignment = TextAnchor.MiddleRight;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 26;

        string levelName = SceneManager.GetActiveScene().name;

        GUI.Label(
            new Rect(20, 15, 500, 48),
            LevelBackgroundTitle.GetDisplayTitle(levelName),
            titleStyle
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

    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerRespawn.ResetDeathCount();
        SceneManager.LoadScene("MainMenu");
    }
}
