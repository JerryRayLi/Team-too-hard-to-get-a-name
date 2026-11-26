using UnityEngine;

// A gate's color identifies its controlling button, independently of room color.
[ExecuteAlways, DefaultExecutionOrder(1350)]
public sealed class JerryGateButtonLight : MonoBehaviour
{
    public Button source;
    public SpriteRenderer gateRenderer;
    public SpriteRenderer[] tubes = new SpriteRenderer[0];
    public SpriteRenderer[] cores = new SpriteRenderer[0];
    public SpriteRenderer[] housings = new SpriteRenderer[0];
    public SpriteRenderer[] halos = new SpriteRenderer[0];

    public static Color SignalColor(Button button)
    {
        Color c = button.requiredBackground == RequiredButtonBackground.Blue ? button.blueColor : button.redColor;
        Color.RGBToHSV(c, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp(s + .18f, .45f, .78f), .86f);
    }
    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();
    public void Refresh()
    {
        if (source == null || gateRenderer == null) return;
        bool visible = gateRenderer.enabled && gateRenderer.gameObject.activeInHierarchy;
        Color tint = SignalColor(source);
        bool active = source.IsPressed;
        Paint(tubes, Color.Lerp(tint, Color.white, active ? .42f : .08f), visible);
        Paint(cores, Color.Lerp(tint, Color.white, active ? .8f : .38f), visible);
        foreach (var r in housings) if(r != null) r.enabled = visible;
        tint.a = active ? .17f : .10f;
        Paint(halos, tint, visible);
    }
    private static void Paint(SpriteRenderer[] renderers, Color color, bool visible)
    {
        foreach (var r in renderers) if (r != null) { r.color = color; r.enabled = visible; }
    }
}
