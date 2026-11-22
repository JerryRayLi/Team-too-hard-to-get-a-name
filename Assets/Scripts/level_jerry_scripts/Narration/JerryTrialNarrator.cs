using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Read-only presentation for BabyTutorial and Level01-04. No triggers, player
// input handlers, gameplay callbacks, scene edits, or physics writes are used.
[DefaultExecutionOrder(1800)]
public sealed class JerryTrialNarrator : MonoBehaviour
{
    private static JerryTrialNarrator instance;
    private string boundName;
    private int boundHandle = -1;
    private TrialNarrationSequence sequence;
    private PlayerController player;
    private Rigidbody2D body;
    private Collider2D playerCollider, exitCollider;
    private ColorLayerManager colors;
    private Vector2 start;
    private bool initialBlue;
    private int lastGlobalDeaths, levelDeaths;
    private float stableTime, rebindTime;
    private Canvas canvas;
    private CanvasGroup group;
    private RectTransform panel;
    private Text caption;
    private RectTransform introduction;
    private readonly Vector3[] corners = new Vector3[4];

    public static bool IsTrialScene(string sceneName) => TrialNarrationProfile.TryGet(sceneName, out _);
    public static bool OwnsTauntText(string sceneName) => instance != null && instance.isActiveAndEnabled && instance.boundName == sceneName && IsTrialScene(sceneName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitialScene() => SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive && scene != SceneManager.GetActiveScene())
            return;
        if (!IsTrialScene(scene.name))
        {
            if (instance != null)
            {
                var old = instance;
                instance = null;
                if (old.canvas != null)
                    old.canvas.enabled = false;
                Destroy(old.gameObject);
            }
            return;
        }
        if (instance == null)
        {
            var owner = new GameObject("Trial Subtitle Narrator");
            instance = owner.AddComponent<JerryTrialNarrator>();
            DontDestroyOnLoad(owner);
        }
        instance.BindScene(scene);
    }

    private void BindScene(Scene scene)
    {
        if (boundHandle == scene.handle && boundName == scene.name && player != null)
            return;
        bool retry = boundName == scene.name && sequence != null;
        boundName = scene.name;
        boundHandle = scene.handle;
        if (!retry)
        {
            if (!TrialNarrationProfile.TryGet(boundName, out var profile))
                return;
            sequence = new TrialNarrationSequence(profile);
            levelDeaths = 0;
            lastGlobalDeaths = PlayerRespawn.DeathCount;
        }
        ObserveDeaths();
        sequence.Tick(0f, false, false, levelDeaths, true, false);
        FindSceneObjects(scene);
        stableTime = rebindTime = 0f;
        EnsureUI();
        if (!retry || !sequence.IsDeathResponse)
        {
            canvas.enabled = false;
            group.alpha = 0f;
        }
    }

    private void FindSceneObjects(Scene scene)
    {
        player = null;
        colors = null;
        exitCollider = null;
        introduction = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (player == null)
                player = root.GetComponentInChildren<PlayerController>(true);
            if (colors == null)
                colors = root.GetComponentInChildren<ColorLayerManager>(true);
            if (exitCollider == null)
            {
                var exit = root.GetComponentInChildren<LevelExit>(true);
                if (exit != null)
                    exitCollider = exit.GetComponent<Collider2D>();
            }
            foreach (var rectangle in root.GetComponentsInChildren<RectTransform>(true))
                if (rectangle.name == "Introduction")
                    introduction = rectangle;
        }
        body = player != null ? player.GetComponent<Rigidbody2D>() : null;
        playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        start = player != null ? (Vector2)player.transform.position : Vector2.zero;
        initialBlue = colors == null || colors.IsBlueBackground;
    }

    private void ObserveDeaths()
    {
        int total = PlayerRespawn.DeathCount;
        if (total >= lastGlobalDeaths)
            levelDeaths += total - lastGlobalDeaths;
        lastGlobalDeaths = total;
    }

    private void Update()
    {
        if (sequence == null || SceneManager.GetActiveScene().name != boundName)
            return;
        if (player == null)
        {
            rebindTime += Time.unscaledDeltaTime;
            if (rebindTime >= 1f)
            {
                rebindTime = 0f;
                FindSceneObjects(SceneManager.GetActiveScene());
            }
        }
        ObserveDeaths();
        bool alive = player != null && player.isActiveAndEnabled && player.gameObject.activeInHierarchy && Time.timeScale > 0f;
        float dt = Mathf.Min(Time.deltaTime, .25f);
        bool steady = body == null || Mathf.Abs(body.linearVelocity.y) < .25f;
        stableTime = alive && steady ? stableTime + dt : 0f;
        bool nearStart = player != null && Vector2.Distance(player.transform.position, start) < 1.35f;
        bool progress = false;
        if (player != null)
        {
            if (sequence.Profile.ProgressOnColorSwitch)
                progress = colors != null && colors.IsBlueBackground != initialBlue;
            else if (exitCollider != null)
            {
                var position = player.transform.position;
                float span = exitCollider.bounds.center.x - start.x;
                progress = Mathf.Abs(span) > 2f && (position.x - start.x) / span >= .5f;
            }
        }
        sequence.Tick(dt, alive, stableTime >= .20f, levelDeaths, nearStart, progress);
        bool visible = Time.timeScale > 0f && sequence.CurrentCue.HasValue && (alive || sequence.IsDeathResponse);
        canvas.enabled = visible;
        if (!visible)
            return;
        caption.text = sequence.CurrentText;
        float fadeIn = sequence.IsDeathResponse ? 1f : Mathf.Clamp01(sequence.Age / .05f);
        group.alpha = Mathf.Min(fadeIn, Mathf.Clamp01((sequence.Duration - sequence.Age) / .08f));
        UpdateLayout();
    }

    private void EnsureUI()
    {
        if (canvas != null)
            return;
        var owner = new GameObject("Trial Narration Subtitles", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        owner.transform.SetParent(transform, false);
        canvas = owner.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        var scaler = owner.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        group = owner.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var box = new GameObject("Narrator Caption", typeof(Image));
        panel = (RectTransform)box.transform;
        panel.SetParent(owner.transform, false);
        panel.anchorMin = panel.anchorMax = new Vector2(.5f, 0f);
        panel.pivot = new Vector2(.5f, 0f);
        var backdrop = box.GetComponent<Image>();
        backdrop.color = new Color(.025f, .04f, .055f, .80f);
        backdrop.raycastTarget = false;

        var textObject = new GameObject("Subtitle", typeof(Text));
        var rectangle = (RectTransform)textObject.transform;
        rectangle.SetParent(panel, false);
        rectangle.anchorMin = Vector2.zero;
        rectangle.anchorMax = Vector2.one;
        rectangle.offsetMin = new Vector2(18, 6);
        rectangle.offsetMax = new Vector2(-18, -6);
        caption = textObject.GetComponent<Text>();
        caption.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        caption.fontSize = 26;
        caption.alignment = TextAnchor.MiddleCenter;
        caption.color = new Color(1f, .95f, .84f, 1f);
        caption.horizontalOverflow = HorizontalWrapMode.Wrap;
        caption.verticalOverflow = VerticalWrapMode.Truncate;
        caption.raycastTarget = false;
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        float scale = Mathf.Max(.1f, canvas.scaleFactor);
        float pixelWidth = canvas.pixelRect.width > 0f ? canvas.pixelRect.width : Screen.width;
        float pixelHeight = canvas.pixelRect.height > 0f ? canvas.pixelRect.height : Screen.height;
        float width = Mathf.Min(1000f, pixelWidth / scale - 40f);
        float bottom = Mathf.Max(18f, Screen.safeArea.yMin / scale + 12f);
        if (introduction == null)
        {
            var hints = GameObject.Find("Gameplay Hints");
            if (hints != null)
                introduction = hints.transform.Find("Introduction") as RectTransform;
        }
        if (introduction != null && introduction.gameObject.activeInHierarchy)
        {
            introduction.GetWorldCorners(corners);
            var parentCanvas = introduction.GetComponentInParent<Canvas>();
            var uiCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;
            float top = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]).y;
            if (top < pixelHeight * .45f)
                bottom = Mathf.Max(bottom, top / scale + 12f);
        }
        // Use a second low subtitle row when the actor occupies the bottom row.
        if (playerCollider != null && Camera.main != null)
        {
            var bounds = playerCollider.bounds;
            var low = Camera.main.WorldToScreenPoint(bounds.min);
            var high = Camera.main.WorldToScreenPoint(bounds.max);
            float panelTop = (bottom + 58f) * scale;
            bool overlapsX = high.x >= pixelWidth * .5f - width * scale * .5f && low.x <= pixelWidth * .5f + width * scale * .5f;
            if (overlapsX && low.y <= panelTop + 8f * scale && high.y >= bottom * scale - 8f * scale)
                bottom = Mathf.Max(bottom, Mathf.Min(high.y / scale + 12f, bottom + 96f));
        }
        panel.sizeDelta = new Vector2(Mathf.Max(160f, width), 58f);
        panel.anchoredPosition = new Vector2(0, bottom);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void OnDisable()
    {
        if (canvas != null)
            canvas.enabled = false;
    }
}
