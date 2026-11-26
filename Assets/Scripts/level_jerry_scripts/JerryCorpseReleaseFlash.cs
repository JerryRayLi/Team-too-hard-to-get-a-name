using UnityEngine;

// A single gentle flash on bodies actually released by E. Never changes physics.
public sealed class JerryCorpseReleaseFlash : MonoBehaviour
{
    [Min(.05f)] public float duration = .28f;
    private SpriteRenderer[] pieces;
    private Color[] restColors;
    private float remaining;

    public void Play()
    {
        if (pieces == null)
        {
            pieces = GetComponentsInChildren<SpriteRenderer>(true);
            restColors = new Color[pieces.Length];
            for (int i = 0; i < pieces.Length; i++) restColors[i] = pieces[i].color;
        }
        remaining = Mathf.Max(.05f, duration);
        enabled = true;
        Apply(.72f);
    }
    private void LateUpdate() => Advance(Time.deltaTime);
    private void Advance(float seconds)
    {
        remaining = Mathf.Max(0f, remaining - Mathf.Max(0f, seconds));
        Apply(.72f * Mathf.SmoothStep(0,1,remaining/Mathf.Max(.05f,duration)));
        if (remaining <= 0f) enabled = false;
    }
    private void Apply(float strength)
    {
        if (pieces == null) return;
        for (int i = 0; i < pieces.Length; i++)
            if (pieces[i] != null)
            {
                Color color = Color.Lerp(restColors[i],new Color(1f,.96f,.80f,1f),strength);
                color.a = restColors[i].a;
                pieces[i].color = color;
            }
    }
    private void OnDisable() => Apply(0f);
    private void OnDestroy() => Apply(0f);
}
