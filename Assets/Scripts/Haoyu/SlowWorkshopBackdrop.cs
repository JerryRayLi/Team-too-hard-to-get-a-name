using System.Collections.Generic;
using UnityEngine;

// Clockwork workshop behind the SLOW levels: striped wallpaper, a ceiling beam with three lamps, brass
// pipes, a shelf, a pendulum clock and gears, wooden wainscoting down to the floor, and a dark gear
// pit under it. The wall and lamp light follow the colour switch; wood and brass stay the same.
// Upper-wall items are laid out on a 1600 x 900 sketch of the camera view and scaled with it; the
// wainscoting and the pit are placed from the floor line.
public sealed class SlowWorkshopBackdrop : MonoBehaviour
{
    const float WainscotHeight = 3.5f;
    const int Back = -210; // behind the level title canvas (-100) and all gameplay sprites

    sealed class Tinted { public SpriteRenderer r; public Color blue, red; }
    readonly List<Tinted> tinted = new List<Tinted>();
    readonly List<(Transform t, float speed)> spinners = new List<(Transform, float)>();
    Transform pendulum;
    ColorLayerManager colors;
    Material material;
    bool? shownBlue;
    float k, left, top;

    static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
    static readonly Color Wain = H("#4B392D"), WainFrame = H("#5C4636"), WainInset = H("#433327"), Shade = H("#1E1713");
    static readonly Color Pipe = H("#5C4A36"), Brass = H("#B08A52"), Case = H("#4A3426"), Face = H("#D8CBB0"), Hand = H("#2A1E17");

    public void Build(Camera cam, float floorTop, Material spriteMaterial)
    {
        material = spriteMaterial;
        colors = FindFirstObjectByType<ColorLayerManager>();
        var fit = cam.GetComponent<FinalFixedCamera>();
        float size = fit ? fit.orthographicSize : cam.orthographicSize, halfW = size * 16f / 9f;
        var c = (Vector2)cam.transform.position;
        k = size / 450f; left = c.x - halfW; top = c.y + size;
        float bottom = c.y - size, wideW = halfW * 6, wainTop = floorTop + WainscotHeight;

        // Wall, wallpaper stripes and ceiling beam.
        Rect(new Vector2(c.x, c.y), new Vector2(wideW, size * 4), H("#26363F"), H("#3E2623"), Back);
        float stripeTop = top - 70 * k;
        for (float x = left - halfW * 2; x < left + halfW * 4; x += 80 * k)
            Rect(new Vector2(x + 20 * k, (stripeTop + wainTop) / 2), new Vector2(40 * k, stripeTop - wainTop), H("#2A3C46"), H("#432A26"), Back + 1);
        Rect(new Vector2(c.x, top - 35 * k + size), new Vector2(wideW, 70 * k + size * 2), H("#1C2328"), H("#241817"), Back + 2);
        Rect(new Vector2(c.x, top - 68 * k), new Vector2(wideW, 8 * k), Fade(Brass, .55f), Fade(Brass, .55f), Back + 3);

        // Lamps: their light colour is the clearest cue for which background is active.
        foreach (float x in new[] { 260f, 800f, 1520f })
        {
            float glowTop = top - 120 * k, glowH = glowTop - wainTop;
            Add(SlowArtSprites.Cone, new Vector2(Px(x), glowTop - glowH / 2), new Vector2(300 * k, glowH), Fade(H("#7FC4E6"), .05f), Fade(H("#F08A6A"), .05f), Back + 4);
            Rect(P(x, 87), new Vector2(2 * k, 34 * k), Hand, Hand, Back + 5);
            Rect(P(x, 112), new Vector2(40 * k, 16 * k), Shade, Shade, Back + 5);
            Add(SlowArtSprites.Disc, P(x, 123), new Vector2(18 * k, 9 * k), H("#BFE6F7"), H("#FFD0BD"), Back + 6);
        }

        // Pipes.
        Rect(new Vector2(c.x, top - 167 * k), new Vector2(wideW, 14 * k), Pipe, Pipe, Back + 4);
        foreach (float x in new[] { 120f, 520f, 980f, 1450f }) Rect(P(x + 9, 167), new Vector2(18 * k, 24 * k), Brass, Brass, Back + 5);
        float pipeTop = top - 174 * k;
        Rect(new Vector2(Px(1447), (pipeTop + wainTop) / 2), new Vector2(14 * k, pipeTop - wainTop), Pipe, Pipe, Back + 4);
        Rect(P(1447, 308), new Vector2(24 * k, 16 * k), Brass, Brass, Back + 5);

        // Shelf with a small clock, a jar and an hourglass.
        Color silB = H("#33474F"), silR = H("#553532");
        Rect(P(185, 255), new Vector2(230 * k, 10 * k), WainFrame, WainFrame, Back + 5);
        Rect(P(108, 232), new Vector2(26 * k, 35 * k), silB, silR, Back + 5);
        Add(SlowArtSprites.Disc, P(160, 232), Vector2.one * 36 * k, silB, silR, Back + 5);
        Add(SlowArtSprites.Triangle, P(229, 221), new Vector2(28 * k, 19 * k), silB, silR, Back + 5);
        var lower = Add(SlowArtSprites.Triangle, P(229, 240), new Vector2(28 * k, 19 * k), silB, silR, Back + 5);
        lower.transform.localRotation = Quaternion.Euler(0, 0, 180);
        Rect(P(273, 238), new Vector2(22 * k, 24 * k), silB, silR, Back + 5);

        // Gears on the wall.
        Spin(Add(SlowArtSprites.Gear, P(560, 300), Vector2.one * 140 * k, silB, silR, Back + 5).transform, 8f);
        Spin(Add(SlowArtSprites.Gear, P(655, 355), Vector2.one * 84 * k, silB, silR, Back + 5).transform, -13.3f);
        Spin(Add(SlowArtSprites.Gear, P(1010, 270), Vector2.one * 96 * k, silB, silR, Back + 5).transform, 11f);

        BuildClock(wainTop);

        // Wainscoting from the floor up.
        Rect(new Vector2(c.x, floorTop + WainscotHeight / 2), new Vector2(wideW, WainscotHeight), Wain, Wain, Back + 8);
        Rect(new Vector2(c.x, wainTop - 5 * k), new Vector2(wideW, 10 * k), WainFrame, WainFrame, Back + 9);
        float panelTop = wainTop - 26 * k, panelH = Mathf.Min(100 * k, WainscotHeight - 40 * k);
        for (float x = left - halfW * 2 + 20 * k; x < left + halfW * 4; x += 160 * k)
        {
            Rect(new Vector2(x + 65 * k, panelTop - panelH / 2), new Vector2(130 * k, panelH), WainInset, WainInset, Back + 9);
            Rect(new Vector2(x + 65 * k, panelTop - 2 * k), new Vector2(130 * k, 4 * k), WainFrame, WainFrame, Back + 10);
        }

        // The pit under the floor: dark, with slow gears, so gaps read as a drop into machinery.
        Color under = H("#182126"), underR = H("#261716"), gearB = H("#1F2C33"), gearR = H("#331F1D");
        Rect(new Vector2(c.x, (floorTop + bottom - size) / 2), new Vector2(wideW, floorTop - bottom + size), under, underR, Back + 8);
        float pitTop = floorTop - 1.2f;
        Spin(Add(SlowArtSprites.Gear, new Vector2(Px(300), pitTop - size * .3f), Vector2.one * 220 * k, gearB, gearR, Back + 9).transform, 4f);
        Spin(Add(SlowArtSprites.Gear, new Vector2(Px(470), pitTop - size * .1f), Vector2.one * 120 * k, gearB, gearR, Back + 9).transform, -7f);
        Spin(Add(SlowArtSprites.Gear, new Vector2(Px(1050), pitTop - size * .22f), Vector2.one * 180 * k, gearB, gearR, Back + 9).transform, 5f);
        Spin(Add(SlowArtSprites.Gear, new Vector2(Px(1380), pitTop - size * .4f), Vector2.one * 140 * k, gearB, gearR, Back + 9).transform, -6f);

        Apply(true);
    }

    void BuildClock(float wainTop)
    {
        float caseTop = top - 120 * k, caseH = caseTop - wainTop;
        Rect(new Vector2(Px(1275), caseTop - caseH / 2), new Vector2(190 * k, caseH), Case, Case, Back + 5);
        Rect(new Vector2(Px(1275), caseTop - 5 * k), new Vector2(190 * k, 10 * k), Fade(Brass, .7f), Fade(Brass, .7f), Back + 6);
        Add(SlowArtSprites.Disc, P(1275, 200), Vector2.one * 124 * k, Fade(Face, .9f), Fade(Face, .9f), Back + 6);
        Add(SlowArtSprites.Ring, P(1275, 200), Vector2.one * 128 * k, Brass, Brass, Back + 7);
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6;
            var tick = Rect(P(1275 + 50 * Mathf.Sin(a), 200 - 50 * Mathf.Cos(a)), new Vector2(4 * k, (i % 3 == 0 ? 10 : 6) * k), Hand, Hand, Back + 7);
            tick.transform.localRotation = Quaternion.Euler(0, 0, -i * 30);
        }
        ClockHand(46 * k, 4 * k, 6f);   // one turn a minute
        ClockHand(32 * k, 7 * k, .5f);  // one turn every twelve minutes
        float insetTop = top - 280 * k, insetH = insetTop - wainTop - 15 * k;
        Rect(new Vector2(Px(1275), insetTop - insetH / 2), new Vector2(120 * k, insetH), WainInset, WainInset, Back + 6);
        pendulum = new GameObject("Pendulum").transform;
        pendulum.SetParent(transform, false); pendulum.position = P(1275, 285);
        float rod = Mathf.Min(98 * k, insetH - 24 * k);
        Rect(pendulum, new Vector2(0, -rod / 2), new Vector2(3 * k, rod), Brass, Back + 7);
        Add(SlowArtSprites.Disc, pendulum, new Vector2(0, -rod), Vector2.one * 32 * k, Brass, Back + 7);
    }

    void ClockHand(float length, float width, float degreesPerSecond)
    {
        var pivot = new GameObject("ClockHand").transform;
        pivot.SetParent(transform, false); pivot.position = P(1275, 200);
        pivot.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        Rect(pivot, new Vector2(0, length / 2), new Vector2(width, length), Hand, Back + 8);
        Spin(pivot, -degreesPerSecond);
    }

    void Update()
    {
        foreach (var (t, speed) in spinners) t.Rotate(0, 0, speed * Time.deltaTime);
        // A slow swing: about three seconds there and back.
        if (pendulum) pendulum.localRotation = Quaternion.Euler(0, 0, 10f * Mathf.Sin(Time.time * 2 * Mathf.PI / 3.2f));
        Apply(false);
    }

    void Apply(bool force)
    {
        bool blue = !colors || colors.IsBlueBackground;
        if (!force && shownBlue == blue) return;
        shownBlue = blue;
        foreach (var t in tinted) if (t.r) t.r.color = blue ? t.blue : t.red;
    }

    float Px(float x) => left + x * k;
    Vector2 P(float x, float y) => new Vector2(left + x * k, top - y * k);
    static Color Fade(Color c, float a) { c.a = a; return c; }
    void Spin(Transform t, float speed) => spinners.Add((t, speed));

    SpriteRenderer Rect(Vector2 pos, Vector2 size, Color blue, Color red, int order) => Add(SlowArtSprites.Square, pos, size, blue, red, order);
    void Rect(Transform parent, Vector2 local, Vector2 size, Color color, int order) => Add(SlowArtSprites.Square, parent, local, size, color, order);

    SpriteRenderer Add(Sprite sprite, Vector2 pos, Vector2 size, Color blue, Color red, int order)
    {
        var r = Make(sprite, transform, size, order);
        r.transform.position = pos;
        tinted.Add(new Tinted { r = r, blue = blue, red = red });
        return r;
    }

    void Add(Sprite sprite, Transform parent, Vector2 local, Vector2 size, Color color, int order)
    {
        var r = Make(sprite, parent, size, order);
        r.transform.localPosition = local;
        r.color = color;
    }

    SpriteRenderer Make(Sprite sprite, Transform parent, Vector2 size, int order)
    {
        var go = new GameObject(sprite.name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite; r.sortingOrder = order;
        if (material) r.sharedMaterial = material;
        return r;
    }
}
