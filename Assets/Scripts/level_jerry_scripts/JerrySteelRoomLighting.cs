using UnityEngine;

// A visual observer of the existing color mode. The opaque steel wall covers
// the old color wash; this component never writes to cameras or gameplay state.
[DefaultExecutionOrder(1300)]
public sealed class JerrySteelRoomLighting : MonoBehaviour
{
    public ColorLayerManager colorManager;
    [Tooltip("Fallback for rooms without color switching, such as the first tutorial room.")]
    public bool defaultBlue = true;
    public SpriteRenderer[] lenses = new SpriteRenderer[0];
    public SpriteRenderer[] cores = new SpriteRenderer[0];
    public SpriteRenderer[] reflections = new SpriteRenderer[0];
    public Color blueLight = new Color(.12f, .72f, .94f, 1f);
    public Color redLight = new Color(.96f, .30f, .25f, 1f);

    private bool refreshed;
    private Color lastTint;
    private Color CurrentTint => (colorManager != null ? colorManager.IsBlueBackground : defaultBlue)
        ? blueLight : redLight;

    private void OnEnable() => Refresh();
    private void LateUpdate()
    {
        if (!refreshed || lastTint != CurrentTint)
            Refresh();
    }

    public void Refresh()
    {
        Color tint = CurrentTint;
        Color core = Color.Lerp(tint, Color.white, .58f);
        foreach (var lens in lenses)
            if (lens != null)
                lens.color = tint;
        foreach (var light in cores)
            if (light != null)
                light.color = core;
        foreach (var reflection in reflections)
            if (reflection != null)
                reflection.color = new Color(tint.r, tint.g, tint.b, reflection.color.a);
        lastTint = tint;
        refreshed = true;
    }
}
