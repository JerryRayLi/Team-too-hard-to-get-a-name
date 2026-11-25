using UnityEngine;
// Disguised trap tile: looks like plain ground on the blue background and shows yellow-and-black
// hazard stripes once the player switches to the red background, so switching colour reveals it.
[RequireComponent(typeof(SpriteRenderer))]
public sealed class FinalColorReveal : MonoBehaviour
{
    public Color stripeColor = new Color(.96f, .78f, .26f, 1f); // same yellow as the SLOW 1 chaser wall
    public Color gapColor = new Color(.13f, .12f, .12f, 1f);
    [Min(.1f)] public float stripeWidth = .25f;
    SpriteRenderer source, overlay;
    ColorLayerManager colors;
    FinalWeightBrick brick;
    void Awake()
    {
        source = GetComponent<SpriteRenderer>();
        brick = GetComponent<FinalWeightBrick>();
        var size = source.bounds.size;
        var child = new GameObject("ColorReveal");
        child.transform.SetParent(transform, false);
        var s = transform.lossyScale;
        child.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
        child.transform.localPosition = new Vector3(0, 0, -.05f);
        overlay = child.AddComponent<SpriteRenderer>();
        overlay.sprite = Stripes(size.x, size.y);
        overlay.sharedMaterial = source.sharedMaterial;
        overlay.sortingLayerID = source.sortingLayerID;
        overlay.sortingOrder = source.sortingOrder + 1;
        overlay.enabled = false;
    }
    void Start() { colors = FindFirstObjectByType<ColorLayerManager>(); }
    // A collapsed tile stays hidden, and a triggered weight brick keeps its own warning colour.
    void LateUpdate() => overlay.enabled = colors && colors.IsBlueActive && source.enabled && !(brick && brick.Triggered);

    Sprite Stripes(float width, float height)
    {
        const int ppu = 32;
        int w = Mathf.Max(1, Mathf.RoundToInt(width * ppu)), h = Mathf.Max(1, Mathf.RoundToInt(height * ppu));
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color[w * h];
        float period = stripeWidth * 2 * ppu;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool border = x < 2 || y < 2 || x >= w - 2 || y >= h - 2;
                pixels[y * w + x] = border || Mathf.Repeat(x + y, period) >= period / 2 ? gapColor : stripeColor;
            }
        tex.SetPixels(pixels); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), ppu);
    }
}
