using System.Collections.Generic;
using UnityEngine;

// Clockwork-workshop dressing for the SLOW levels: wooden floor, exit door and the workshop backdrop.
// Decorative only: it never moves colliders, changes gameplay colours or touches the colour-layer
// lists. Every solid ground tile, the disguised traps included, gets the same planks, so the traps
// still only show after switching to the red background.
[DefaultExecutionOrder(1500)]
public sealed class SlowLevelArt : MonoBehaviour
{
    static readonly Color Wood = new Color(.56f, .43f, .31f), WoodDark = new Color(.38f, .28f, .20f);
    static readonly Color Grain = new Color(.50f, .38f, .27f), Brass = new Color(.82f, .66f, .40f);
    const float SeamSpacing = 1.5f;

    sealed class Surface { public SpriteRenderer source; public GameObject art; public Color original; }
    readonly List<Surface> surfaces = new List<Surface>();
    readonly List<(Transform t, float speed)> spinners = new List<(Transform, float)>();
    Material material;
    static Sprite square => SlowArtSprites.Square;
    static Sprite ring => SlowArtSprites.Ring;
    static Sprite disc => SlowArtSprites.Disc;

    void Start()
    {
        var grounds = new List<SpriteRenderer>();
        foreach (var r in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            var box = r.GetComponent<BoxCollider2D>();
            if (box && !box.isTrigger && !r.GetComponentInParent<FinalLevelPlayerController>()) grounds.Add(r);
        }
        if (grounds.Count > 0) material = grounds[0].sharedMaterial;
        foreach (var g in grounds) Dress(g, grounds);
        var goal = FindFirstObjectByType<LevelExit>(FindObjectsInactive.Include);
        if (goal && goal.TryGetComponent(out SpriteRenderer door)) DressDoor(door);
        var cam = Camera.main;
        if (cam && grounds.Count > 0)
        {
            float floorTop = float.MinValue;
            foreach (var g in grounds) floorTop = Mathf.Max(floorTop, g.bounds.max.y);
            gameObject.AddComponent<SlowWorkshopBackdrop>().Build(cam, floorTop, material);
        }
    }

    void LateUpdate()
    {
        foreach (var s in surfaces)
        {
            if (!s.source || !s.art) continue;
            // A collapsed or broken tile disappears; a triggered weight brick keeps its own warning colour.
            bool show = s.source.enabled && s.source.gameObject.activeInHierarchy && Same(s.source.color, s.original);
            if (s.art.activeSelf != show) s.art.SetActive(show);
        }
        foreach (var (t, speed) in spinners) t.Rotate(0, 0, speed * Time.deltaTime);
    }

    static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < .02f;

    // A child whose scale cancels the parent's, so the art below works in world units.
    static Transform WorldUnitRoot(Transform parent, string name)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        var s = parent.lossyScale;
        root.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
        return root;
    }

    SpriteRenderer Add(Transform parent, Sprite sprite, Vector2 pos, Vector2 size, Color color, int order, float z = 0)
    {
        var go = new GameObject(sprite.name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, z);
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite; r.color = color; r.sortingOrder = order;
        if (material) r.sharedMaterial = material;
        return r;
    }

    void Dress(SpriteRenderer source, List<SpriteRenderer> all)
    {
        var b = source.bounds;
        float w = b.size.x, h = b.size.y, left = b.min.x, right = b.max.x, top = h / 2, bottom = -h / 2;
        var root = WorldUnitRoot(source.transform, "SlowArt");
        int o = source.sortingOrder;
        // Same sorting order as the tile but nearer the camera, so the blue-layer reveal (order + 1)
        // and the crack lines still draw on top.
        Add(root, square, Vector2.zero, new Vector2(w, h), Wood, o, -.01f);
        Add(root, square, new Vector2(0, -.08f), new Vector2(w, .04f), Grain, o, -.02f);
        Add(root, square, new Vector2(0, bottom + .25f), new Vector2(w, .03f), Grain, o, -.02f);
        Add(root, square, new Vector2(0, bottom + .05f), new Vector2(w, .1f), WoodDark, o, -.02f);
        Add(root, square, new Vector2(0, top - .155f), new Vector2(w, .05f), WoodDark, o, -.02f);
        Add(root, square, new Vector2(0, top - .065f), new Vector2(w, .13f), Brass, o, -.03f);
        // Seams sit on a world grid, so neighbouring tiles line up and trap edges don't show.
        for (float x = Mathf.Ceil(left / SeamSpacing) * SeamSpacing; x < right; x += SeamSpacing)
        {
            if (x - left < .1f || right - x < .1f) continue;
            float lx = x - b.center.x;
            Add(root, square, new Vector2(lx, -.09f), new Vector2(.05f, h - .36f), WoodDark, o, -.02f);
            Add(root, square, new Vector2(lx - .14f, top - .32f), new Vector2(.07f, .07f), Brass, o, -.03f);
            Add(root, square, new Vector2(lx + .14f, top - .32f), new Vector2(.07f, .07f), Brass, o, -.03f);
        }
        // Only the true ends of a floor run get an end board.
        if (!Touches(all, source, left)) Add(root, square, new Vector2(-w / 2 + .04f, 0), new Vector2(.08f, h), WoodDark, o, -.03f);
        if (!Touches(all, source, right)) Add(root, square, new Vector2(w / 2 - .04f, 0), new Vector2(.08f, h), WoodDark, o, -.03f);
        surfaces.Add(new Surface { source = source, art = root.gameObject, original = source.color });
    }

    static bool Touches(List<SpriteRenderer> all, SpriteRenderer self, float x)
    {
        var b = self.bounds;
        foreach (var other in all)
        {
            if (other == self) continue;
            var ob = other.bounds;
            if (Mathf.Abs(ob.center.y - b.center.y) < .2f && (Mathf.Abs(ob.min.x - x) < .05f || Mathf.Abs(ob.max.x - x) < .05f)) return true;
        }
        return false;
    }

    // A wooden workshop door that keeps the team's green exit cue as a lit window, with a clock above it.
    void DressDoor(SpriteRenderer door)
    {
        var b = door.bounds;
        float w = b.size.x, h = b.size.y;
        var root = WorldUnitRoot(door.transform, "SlowDoorArt");
        int o = door.sortingOrder;
        Color frame = new Color(.29f, .20f, .15f), panel = new Color(.47f, .34f, .24f), inset = new Color(.39f, .28f, .19f);
        Color exitGreen = new Color(.30f, .86f, .48f), face = new Color(.85f, .80f, .69f), hand = new Color(.16f, .12f, .09f);
        Add(root, square, new Vector2(0, .08f), new Vector2(w + .32f, h + .16f), frame, o - 1);
        Add(root, square, Vector2.zero, new Vector2(w, h), panel, o + 1);
        Add(root, square, new Vector2(0, h * .22f), new Vector2(w * .62f, h * .36f), exitGreen, o + 2);
        Add(root, square, new Vector2(0, h * .22f), new Vector2(.05f, h * .36f), inset, o + 3);
        Add(root, square, new Vector2(0, -h * .24f), new Vector2(w * .62f, h * .32f), inset, o + 2);
        Add(root, disc, new Vector2(w * .32f, -h * .02f), new Vector2(.13f, .13f), Brass, o + 3);
        Add(root, square, new Vector2(0, h / 2 + .19f), new Vector2(w + .5f, .1f), Brass, o + 1);
        // Clock above the door.
        var clockAt = new Vector2(0, h / 2 + .7f);
        Add(root, disc, clockAt, new Vector2(.86f, .86f), frame, o + 1);
        Add(root, disc, clockAt, new Vector2(.72f, .72f), face, o + 2);
        Add(root, ring, clockAt, new Vector2(.78f, .78f), Brass, o + 3);
        for (int i = 0; i < 12; i += 3)
        {
            var tick = Add(root, square, clockAt + new Vector2(Mathf.Sin(i * Mathf.PI / 6), Mathf.Cos(i * Mathf.PI / 6)) * .27f, new Vector2(.04f, .09f), hand, o + 3);
            tick.transform.localRotation = Quaternion.Euler(0, 0, -i * 30);
        }
        DoorHand(root, clockAt, .28f, .035f, 6f, hand, o + 4);
        DoorHand(root, clockAt, .19f, .05f, .5f, hand, o + 4);
    }

    void DoorHand(Transform root, Vector2 at, float length, float width, float degreesPerSecond, Color color, int order)
    {
        var pivot = new GameObject("DoorClockHand").transform;
        pivot.SetParent(root, false); pivot.localPosition = at;
        pivot.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        Add(pivot, square, new Vector2(0, length / 2), new Vector2(width, length), color, order);
        spinners.Add((pivot, -degreesPerSecond));
    }
}
