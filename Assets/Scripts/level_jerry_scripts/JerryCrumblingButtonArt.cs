using UnityEngine;

// One readable switch housing observes the original receiver and fragile floor.
// All collision, activation and collapse timing remain on those original objects.
[ExecuteAlways, DefaultExecutionOrder(1260)]
public sealed class JerryCrumblingButtonArt : MonoBehaviour
{
    public Button source;
    public JerryPressurePlate plate;
    public SpriteRenderer floorRenderer;
    public Transform movingCap;
    public Vector3 capRestPosition;
    public SpriteRenderer[] pieces = new SpriteRenderer[0];
    public SpriteRenderer[] colorPanels = new SpriteRenderer[0];
    public SpriteRenderer[] signalStrips = new SpriteRenderer[0];
    public SpriteRenderer activationGlow;
    [HideInInspector] public SpriteRenderer[] legacyRenderers = new SpriteRenderer[0];
    [Min(0f)] public float visualTravel = .08f;
    private bool previouslyActive;
    private float activationTime;

    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();
    public void Refresh()
    {
        if (source == null || plate == null || floorRenderer == null || movingCap == null) return;
        // Keep the original renderers as state sources, without drawing a second
        // platform or small cap underneath the unified switch housing.
        floorRenderer.forceRenderingOff = true;
        plate.colorSource.forceRenderingOff = true;
        plate.cap.forceRenderingOff = true;
        foreach (var r in legacyRenderers) if (r != null) r.forceRenderingOff = true;
        bool visible = floorRenderer.enabled && floorRenderer.gameObject.activeInHierarchy && plate.sensor.enabled;
        foreach(var r in pieces) if(r != null) r.enabled = visible;
        Color tint = JerryGateButtonLight.SignalColor(source);
        bool active = plate.sensor.enabled && source.IsPressed;
        if (active && !previouslyActive) activationTime = Time.time;
        previouslyActive = active;
        float flash = active ? Mathf.Clamp01(1f - (Time.time - activationTime) / .4f) : 0f;
        float amount = Mathf.Clamp01(plate.PressAmount);
        Color face = Color.Lerp(new Color(.22f,.27f,.29f,1), tint, active ? .65f + .12f * flash : .23f);
        foreach(var r in colorPanels) if(r != null) r.color = face;
        foreach(var r in signalStrips) if(r != null) r.color = Color.Lerp(tint, Color.white, active ? .75f + .18f * flash : .18f + amount * .10f);
        float travel = plate.sensor.size.y * Mathf.Abs(plate.transform.lossyScale.y) * plate.travelFraction + visualTravel;
        movingCap.localPosition = capRestPosition + movingCap.parent.InverseTransformVector(Vector3.down * (travel * amount));
        if (activationGlow != null)
        {
            Color glow = Color.Lerp(tint, Color.white, .25f);
            glow.a = active ? .18f + .12f * flash : .025f * amount;
            activationGlow.color = glow;
            activationGlow.enabled = visible && glow.a > .001f;
        }
    }
}
