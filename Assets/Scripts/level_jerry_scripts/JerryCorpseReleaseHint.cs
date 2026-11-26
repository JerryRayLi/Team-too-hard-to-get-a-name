using UnityEngine;

// A reminder for the first corpse-release lesson; it only observes bodies.
public sealed class JerryCorpseReleaseHint : MonoBehaviour
{
    private static JerryCorpseReleaseHint current;
    public PlayerRespawnJ player;
    public CorpseReleaseAbility ability;
    [Min(0f)] public float hintDelay = 5f;
    public float heightAboveCorpse = .85f;
    public string hintText = "Press E to release ALL marked corpses in the circle.";
    public string distantHintText = "Move closer until a corpse is marked, then press E.";

    private Corpse target;
    private float waiting;
    private bool dismissed;
    private GUIStyle style;
    public bool IsVisible => !dismissed && target != null && waiting >= hintDelay &&
        player != null && !player.IsRespawning && Time.timeScale > 0f;
    public string CurrentText => target != null && ability != null && player != null &&
        Vector2.Distance(player.transform.position, target.transform.position) < ability.releaseRange
            ? hintText : distantHintText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCurrent() => current = null;
    private void OnEnable() => current = this;
    private void OnDisable() { if (current == this) current = null; }
    public static bool IsShowingIn(UnityEngine.SceneManagement.Scene scene) =>
        current != null && current.gameObject.scene == scene && current.IsVisible;

    private void Update() => Refresh(Time.deltaTime);
    public void Refresh(float seconds)
    {
        if (dismissed || player == null || ability == null || Time.timeScale <= 0f) return;
        target = null;
        float nearest = float.PositiveInfinity;
        foreach (var corpse in FindObjectsByType<Corpse>(FindObjectsSortMode.None))
        {
            if (corpse.gameObject.scene != gameObject.scene) continue;
            var body = corpse.GetComponent<Rigidbody2D>();
            // A successful release changes the body to Dynamic. An E press
            // outside the release radius must not dismiss this reminder.
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
            { dismissed = true; return; }
            if (body == null || body.bodyType != RigidbodyType2D.Static) continue;
            float distance = Vector2.Distance(player.transform.position, corpse.transform.position);
            if (distance < nearest) { nearest = distance; target = corpse; }
        }
        if (target == null) { waiting = 0f; return; }
        if (!player.IsRespawning) waiting += Mathf.Max(0f, seconds);
    }
    private void OnGUI()
    {
        if (!IsVisible || Camera.main == null || JerryLevelHelp.IsShowingIn(gameObject.scene)) return;
        if (style == null) style = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
        style.fontSize = GameHUD.ScaleFont(24);
        style.normal.textColor = new Color(1f,.93f,.73f);
        float scale = GameHUD.UiScale;
        float width = Mathf.Min(560f * scale, Screen.width - 32f);
        float height = Mathf.Max(48f * scale, style.CalcHeight(new GUIContent(CurrentText),width-24f)+12f*scale);
        var point = Camera.main.WorldToScreenPoint(target.transform.position + Vector3.up * heightAboveCorpse);
        if (point.z <= 0f) return;
        float x = Mathf.Clamp(point.x-width*.5f,16,Screen.width-width-16);
        float y = Screen.height-point.y-height*.5f;
        // Keep the reminder clear of the existing upper-left control panel.
        if(x<580f*scale) y=Mathf.Max(y,220f*scale);
        y=Mathf.Clamp(y,16,Mathf.Max(16,Screen.height-height-16));
        var rect=new Rect(x,y,width,height);var previous=GUI.color;
        GUI.color=new Color(.035f,.06f,.075f,.9f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
        GUI.color=previous;GUI.Label(new Rect(x+12f*scale,y,width-24f*scale,height),CurrentText,style);
    }
}
