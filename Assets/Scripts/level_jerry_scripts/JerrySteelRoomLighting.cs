using UnityEngine;

// A visual observer of the existing color mode. The opaque steel wall covers
// the old color wash; this component never writes to cameras or gameplay state.
[ExecuteAlways, DefaultExecutionOrder(1300)]
public sealed class JerrySteelRoomLighting : MonoBehaviour
{
    public ColorLayerManager colorManager;
    [Tooltip("Fallback when no color manager is assigned.")]
    public bool defaultBlue = true;
    public SpriteRenderer[] lenses = new SpriteRenderer[0];
    public SpriteRenderer[] cores = new SpriteRenderer[0];
    public SpriteRenderer[] reflections = new SpriteRenderer[0];
    public SpriteRenderer[] ambientWashes = new SpriteRenderer[0];
    [Min(0f)] public float ambientTransitionSeconds = .2f;
    public Color blueLight = new Color(.12f, .72f, .94f, 1f);
    public Color redLight = new Color(.96f, .30f, .25f, 1f);

    private bool refreshed;
    private Color lastTint;
    private Color ambientTint;
    private Color CurrentTint => (colorManager != null ? colorManager.IsBlueBackground : defaultBlue)
        ? blueLight : redLight;
    private Color AmbientTarget => Color.Lerp(CurrentTint, new Color(.43f, .45f, .46f, 1f), .62f);

    private void OnEnable()
    {
        Refresh();
        ambientTint = AmbientTarget;
        ApplyAmbient();
    }
    private void LateUpdate()
    {
        if (!refreshed || lastTint != CurrentTint)
            Refresh();
        float step = ambientTransitionSeconds <= 0f ? 1f : Time.unscaledDeltaTime / ambientTransitionSeconds;
        ambientTint = Application.isPlaying
            ? (Color)Vector4.MoveTowards(ambientTint, AmbientTarget, step)
            : AmbientTarget;
        ApplyAmbient();
    }

    private void ApplyAmbient()
    {
        foreach (var wash in ambientWashes)
            if (wash != null)
                wash.color = new Color(ambientTint.r, ambientTint.g, ambientTint.b, wash.color.a);
    }

    public void Refresh()
    {
        Color tint = CurrentTint;
        Color core = Color.Lerp(tint, Color.white, .30f);
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
        if (!Application.isPlaying)
        {
            ambientTint = AmbientTarget;
            ApplyAmbient();
        }
    }
}
