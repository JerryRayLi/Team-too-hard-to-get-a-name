using UnityEngine;

// Plain white shapes generated at runtime for the SLOW level art. Each sprite is one world unit wide,
// so a renderer's scale is its size in world units and its colour comes from the renderer.
public static class SlowArtSprites
{
    static Sprite square, ring, disc, gear, cone, triangle;
    public static Sprite Square => square ? square : square = Make("Square", 4, (x, y) => 1);
    public static Sprite Ring => ring ? ring : ring = Make("Ring", 256, (x, y) => Band(Radius(x, y), .9f, 1f));
    public static Sprite Disc => disc ? disc : disc = Make("Disc", 64, (x, y) => Band(Radius(x, y), -1f, 1f));
    public static Sprite Gear => gear ? gear : gear = Make("Gear", 256, (x, y) =>
    {
        bool tooth = Mathf.Repeat(Mathf.Atan2(y, x) / (2 * Mathf.PI) * 10, 1) < .5f;
        return Band(Radius(x, y), .32f, tooth ? 1f : .8f);
    });
    // Narrow at the top and wide at the bottom, for lamp light.
    public static Sprite Cone => cone ? cone : cone = Make("Cone", 128, (x, y) => Mathf.Abs(x) <= Mathf.Lerp(1f, .12f, (y + 1) / 2) ? 1 : 0);
    // Point at the bottom, flat edge on top.
    public static Sprite Triangle => triangle ? triangle : triangle = Make("Triangle", 64, (x, y) => Mathf.Abs(x) <= (y + 1) / 2 ? 1 : 0);

    static float Radius(float x, float y) => Mathf.Sqrt(x * x + y * y);

    // Soft-edged coverage of the radius band [inner, outer] in unit-circle coordinates.
    static float Band(float r, float inner, float outer)
    {
        const float edge = .02f;
        return Mathf.Clamp01((r - inner) / edge + .5f) * Mathf.Clamp01((outer - r) / edge + .5f);
    }

    static Sprite Make(string name, int px, System.Func<float, float, float> alpha)
    {
        var tex = new Texture2D(px, px, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[px * px];
        for (int y = 0; y < px; y++)
            for (int x = 0; x < px; x++)
            {
                float u = (x + .5f) / px * 2 - 1, v = (y + .5f) / px * 2 - 1;
                pixels[y * px + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(u, v)) * 255));
            }
        tex.SetPixels32(pixels); tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(.5f, .5f), px);
        sprite.name = name;
        return sprite;
    }
}
