using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Read-only, contextual subtitles for the three Running Door rooms.
// No input, collision, color-layer or scene-progression writes.
[DefaultExecutionOrder(1800)]
public sealed class DoorSubtitleNarrator : MonoBehaviour
{
    private sealed class Cue
    {
        public string key, text, label;
        public Transform target;
        public int priority;
        public float duration;
    }
    private static DoorSubtitleNarrator instance;
    private static readonly Dictionary<string, HashSet<string>> Seen = new();
    private readonly Dictionary<string, Cue> pending = new();
    private HashSet<string> seen;
    private string sceneName;
    private int room;
    private PlayerController player;
    private Collider2D playerCollider;
    private Transform door;
    private BoxCollider2D exitTrigger;
    private ColorLayerManager colors;
    private Transform wall, pad;
    private Vector3 doorStart;
    private float elapsed, age, silence, steadyTime;
    private Cue current;
    private bool wasCaptured;
    private Canvas canvas;
    private CanvasGroup group;
    private RectTransform panel, marker;
    private Text caption, markerText;

    public static bool OwnsTauntText(string name) =>
        instance != null && instance.isActiveAndEnabled &&
        instance.sceneName == name;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        instance = null;
        Seen.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initial() =>
        SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu") Seen.Clear();
        if (mode == LoadSceneMode.Additive && scene != SceneManager.GetActiveScene())
            return;
        int number = scene.name == "Level_Running_Door_1" ? 1 :
            scene.name == "Level_Running_Door_2" ? 2 :
            scene.name == "Level_Running_Door_3" ? 3 : 0;
        if (number == 0) return;
        if (instance != null && instance.gameObject.scene == scene) return;

        var owner = new GameObject("Door Office Subtitle Narrator");
        SceneManager.MoveGameObjectToScene(owner, scene);
        instance = owner.AddComponent<DoorSubtitleNarrator>();
        instance.Bind(scene, number);
    }

    private void Bind(Scene scene, int number)
    {
        sceneName = scene.name;
        room = number;
        if (!Seen.TryGetValue(sceneName, out seen))
        {
            seen = new HashSet<string>();
            Seen.Add(sceneName, seen);
        }
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (PlayerController candidate in root.GetComponentsInChildren<PlayerController>(true))
                if (candidate.gameObject.activeInHierarchy) player = candidate;
            foreach (LevelExit candidate in root.GetComponentsInChildren<LevelExit>(true))
                if (candidate.gameObject.activeInHierarchy)
                {
                    door = candidate.transform;
                    exitTrigger = candidate.GetComponent<BoxCollider2D>();
                }
            foreach (ColorLayerManager candidate in root.GetComponentsInChildren<ColorLayerManager>(true))
                if (candidate.gameObject.activeInHierarchy) colors = candidate;
        }
        if (player != null) playerCollider = player.GetComponent<Collider2D>();
        if (door != null)
        {
            doorStart = door.position;
            var fleeing = door.GetComponent<FleeingExit>();
            if (fleeing != null && fleeing.blueBarrier != null)
                wall = fleeing.blueBarrier.transform;
            var route = door.GetComponent<Door03LiftRoute>();
            if (room == 3 && route != null && route.capturePad != null)
                pad = route.capturePad.transform;
        }
        if (room == 2)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (DoorCaptureZone zone in root.GetComponentsInChildren<DoorCaptureZone>(true))
                    if (zone.gameObject.activeInHierarchy && zone.indicator != null &&
                        zone.door != null && zone.door.transform == door)
                        pad = zone.indicator.transform;
        }
        CreateUI();
    }

    private void Update()
    {
        if (seen == null || canvas == null) return;
        bool alive = player != null && player.isActiveAndEnabled &&
            player.gameObject.activeInHierarchy && Time.timeScale > 0f;
        if (!alive)
        {
            canvas.enabled = false;
            return;
        }
        float dt = Mathf.Min(Time.deltaTime, .25f);
        elapsed += dt;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        steadyTime = body == null || Mathf.Abs(body.linearVelocity.y) < .3f
            ? steadyTime + dt : 0f;
        bool captured = exitTrigger != null && exitTrigger.enabled;
        bool red = colors != null && colors.IsOrangeBackground;
        int deaths = PlaySessionStats.GetCurrentLevelDeaths();

        if (captured)
        {
            if (!wasCaptured)
            {
                pending.Clear();
                Offer("success", room == 1 ?
                    "The wall did its job. Your turn: walk into the door." :
                    "Approved. Even a door can finish its paperwork. Walk in.",
                    100, null, null, 4.5f);
            }
        }
        else if (room == 1)
        {
            if (elapsed > .8f)
                Offer("entry", "The exit wants to clock out. That wall is still on duty.",
                    10, wall, "STOP THE EXIT", 5f);
            bool fled = door != null && door.position.x > doorStart.x + .45f;
            if (elapsed > 6f || fled || deaths > 0)
                Offer("wall", "The cyan wall can stop the door. Shift to RED makes it solid.",
                    50, wall, "COLOR-SWITCH WALL", 6f);
            if (elapsed > 17f && red)
                Offer("retry", "Missed the wall? R restarts the chase. The paperwork survives.",
                    40, wall, "BLOCK THE DOOR", 5.5f);
        }
        else if (room == 2)
        {
            if (elapsed > .8f)
                Offer("entry", "That floor strip is a CAPTURE PAD. Not another office decoration.",
                    10, pad, "CAPTURE PAD", 5.5f);
            if (elapsed > 6f || deaths > 0)
                Offer("herd", HerdHint(),
                    40, pad, "HERD THE DOOR HERE", 6f);
            if (elapsed > 13f || (deaths >= 2 && elapsed > 3f))
                Offer("red", "RED arms the capture pad. Shift, then herd the door onto it. Approval pending.",
                    50, pad, "RED = PAD ACTIVE", 6f);
        }
        else
        {
            if (elapsed > .8f)
                Offer("entry", "The exit booked a private lift. Humans get the BLUE steps. Naturally.",
                    10, null, null, 5.5f);
            bool upstairs = door != null && door.position.y > doorStart.y + 3f;
            if ((upstairs && elapsed > 6f) || elapsed > 15f || deaths >= 2)
                Offer("pad", "Capture pad: top RIGHT. Land on a permanent ledge, then Shift to RED to arm it.",
                    50, pad, "UPSTAIRS CAPTURE PAD", 6.5f);
            if (upstairs && player.transform.position.y > door.position.y - .9f &&
                elapsed > 16f)
                Offer("herd", "Get LEFT of the door to send it RIGHT onto the pad. No approval by chasing.",
                    40, pad, "HERD THE DOOR HERE", 6f);
        }
        wasCaptured = captured;

        silence = Mathf.Max(0f, silence - dt);
        if (current != null)
        {
            age += dt;
            Cue waiting = BestCue();
            bool replace = age >= 3f && waiting != null &&
                waiting.priority > current.priority;
            if (age >= current.duration || replace)
            {
                current = null;
                silence = replace ? .15f : .8f;
            }
        }
        if (current == null && silence <= 0f && steadyTime >= .15f)
        {
            current = BestCue();
            if (current != null)
            {
                pending.Remove(current.key);
                seen.Add(current.key);
                age = 0f;
            }
        }
        canvas.enabled = current != null;
        if (current == null) return;
        caption.text = current.text;
        group.alpha = Mathf.Min(Mathf.Clamp01(age / .15f),
            Mathf.Clamp01((current.duration - age) / .25f));
        UpdateLayout();
    }

    private string HerdHint()
    {
        bool left = pad == null || door == null || pad.position.x < door.position.x;
        return left ?
            "Chasing is unpaid overtime. Get RIGHT of the door to herd it LEFT onto the pad." :
            "Chasing is unpaid overtime. Get LEFT of the door to herd it RIGHT onto the pad.";
    }

    private void Offer(string key, string text, int priority, Transform target,
        string label, float duration)
    {
        if (seen.Contains(key) || pending.ContainsKey(key)) return;
        pending.Add(key, new Cue { key = key, text = text, priority = priority,
            target = target, label = label, duration = duration });
    }

    private Cue BestCue()
    {
        Cue best = null;
        foreach (Cue cue in pending.Values)
            if (best == null || cue.priority > best.priority ||
                (cue.priority == best.priority &&
                 string.CompareOrdinal(cue.key, best.key) < 0))
                best = cue;
        return best;
    }

    private void CreateUI()
    {
        var owner = new GameObject("Door Office Subtitles", typeof(Canvas),
            typeof(CanvasScaler), typeof(CanvasGroup));
        owner.transform.SetParent(transform, false);
        canvas = owner.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        CanvasScaler scaler = owner.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280,720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        group = owner.GetComponent<CanvasGroup>();
        group.blocksRaycasts = group.interactable = false;
        panel = Panel(owner.transform, "Office broadcast", out caption);
        panel.anchorMin = panel.anchorMax = new Vector2(.5f,0f);
        panel.pivot = new Vector2(.5f,0f);
        caption.fontSize = 27;
        marker = Panel(owner.transform, "Mechanic location", out markerText);
        marker.anchorMin = marker.anchorMax = Vector2.zero;
        marker.pivot = new Vector2(.5f,0f);
        marker.sizeDelta = new Vector2(280f,60f);
        markerText.fontSize = 22;
        markerText.color = new Color(1f,.89f,.42f,1f);
        marker.gameObject.SetActive(false);
        canvas.enabled = false;
    }

    private static RectTransform Panel(Transform parent, string name, out Text text)
    {
        var obj = new GameObject(name, typeof(Image));
        RectTransform rect = (RectTransform)obj.transform;
        rect.SetParent(parent,false);
        Image image = obj.GetComponent<Image>();
        image.color = new Color(.035f,.065f,.055f,.92f);
        image.raycastTarget = false;
        var label = new GameObject("Caption", typeof(Text));
        RectTransform labelRect = (RectTransform)label.transform;
        labelRect.SetParent(rect,false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(18f,7f);
        labelRect.offsetMax = new Vector2(-18f,-7f);
        text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f,.96f,.85f,1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return rect;
    }

    private void UpdateLayout()
    {
        float scale = Mathf.Max(.1f,canvas.scaleFactor);
        float width = canvas.pixelRect.width / scale;
        float height = canvas.pixelRect.height / scale;
        float bottom = Mathf.Max(22f,Screen.safeArea.yMin / scale + 12f);
        Camera view = Camera.main;
        if (playerCollider != null && view != null)
        {
            Vector3 low = view.WorldToScreenPoint(playerCollider.bounds.min);
            Vector3 high = view.WorldToScreenPoint(playerCollider.bounds.max);
            if (low.y / scale < bottom + 88f && high.y / scale > bottom)
                bottom = Mathf.Min(high.y / scale + 14f, bottom + 100f);
        }
        panel.sizeDelta = new Vector2(Mathf.Max(200f,Mathf.Min(1080f,width-40f)),84f);
        panel.anchoredPosition = new Vector2(0f,bottom);
        bool focus = current.target != null && view != null && age < 4.5f &&
            (current.target.gameObject.activeInHierarchy || current.target == wall);
        marker.gameObject.SetActive(focus);
        if (!focus) return;
        Vector3 point = view.WorldToScreenPoint(current.target.position);
        if (point.z <= 0f || point.x < 0f || point.x > canvas.pixelRect.width)
        {
            marker.gameObject.SetActive(false);
            return;
        }
        float markerBottom = point.y / scale + 28f;
        if (Mathf.Abs(point.x / scale - width * .5f) < panel.sizeDelta.x * .5f &&
            markerBottom < bottom + 100f)
            markerBottom = bottom + 102f;
        marker.anchoredPosition = new Vector2(
            Mathf.Clamp(point.x / scale,145f,Mathf.Max(145f,width-145f)),
            Mathf.Clamp(markerBottom,15f,Mathf.Max(15f,height-80f)));
        markerText.text = current.label + "\n v";
    }

    private void OnDisable()
    {
        if (canvas != null) canvas.enabled = false;
    }
    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
