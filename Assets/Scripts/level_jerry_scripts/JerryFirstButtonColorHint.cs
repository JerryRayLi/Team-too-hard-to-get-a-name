using UnityEngine;

// This tutorial observes real player contact; it does not drive the button.
[DefaultExecutionOrder(1500)]
public sealed class JerryFirstButtonColorHint : MonoBehaviour
{
    public Button source;
    public JerryPressurePlate plate;
    [Min(0f)] public float delay = 3f;
    public float heightAboveButton = 1.9f;
    public string hintText = "Try switching color!\nShift / K / Right Click";
    public bool IsVisible => armed && !dismissed && elapsed >= delay;

    private readonly Collider2D[] contacts = new Collider2D[32];
    private bool armed, dismissed, initialBlue;
    private float elapsed;
    private GUIStyle style;

    private void Update()
    {
        if (Time.timeScale > 0f)
            Refresh(Time.deltaTime);
    }

    public void Refresh(float deltaTime)
    {
        if (dismissed || source == null || source.colorManager == null || plate == null)
            return;
        bool blue = source.colorManager.IsBlueBackground;
        if (!armed)
        {
            if (!HasPlayerOnTop())
                return;
            armed = true;
            initialBlue = blue;
        }
        if (blue != initialBlue)
        {
            dismissed = true;
            return;
        }
        elapsed += Mathf.Max(0f, deltaTime);
    }

    private bool HasPlayerOnTop()
    {
        if (!plate.sensor.enabled || !plate.surface.enabled)
            return false;
        var bounds = plate.sensor.bounds;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        int count = gameObject.scene.GetPhysicsScene2D().OverlapBox(bounds.center,
            (Vector2)bounds.size + Vector2.one * .14f, 0f, filter, contacts);
        float top = plate.SurfaceTop;
        for (int i = 0; i < count; i++)
        {
            var other = contacts[i];
            var player = other.GetComponentInParent<PlayerRespawnJ>();
            if (player == null || player.IsRespawning || other.bounds.center.y <= top || other.bounds.min.y < top - .07f)
                continue;
            var distance = plate.surface.Distance(other);
            if (distance.isValid && distance.distance <= .07f)
                return true;
        }
        return false;
    }

    private void OnGUI()
    {
        if (JerryLevelHelp.IsShowingIn(gameObject.scene) || JerryCorpseReleaseHint.IsShowingIn(gameObject.scene)) return;
        if (!IsVisible || Time.timeScale <= 0f || Camera.main == null)
            return;
        Vector3 world = plate.sensor.bounds.center;
        world.y = plate.sensor.bounds.max.y + heightAboveButton;
        var point = Camera.main.WorldToScreenPoint(world);
        if (point.z <= 0f)
            return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            style.normal.textColor = new Color(1f, .93f, .73f);
        }
        style.fontSize = GameHUD.ScaleFont(24);
        float width = Mathf.Min(460f * GameHUD.UiScale, Screen.width - 32f);
        float height = Mathf.Max(48f, 72f * GameHUD.UiScale);
        var rect = new Rect(Mathf.Clamp(point.x - width * .5f, 16f, Screen.width - width - 16f),
            Mathf.Clamp(Screen.height - point.y - height * .5f, 16f, Screen.height - height - 16f), width, height);
        var previous = GUI.color;
        GUI.color = new Color(.04f, .07f, .09f, .86f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(rect, hintText, style);
    }
}
