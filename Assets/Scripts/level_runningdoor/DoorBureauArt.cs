using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// All children are decorative. This component never edits gameplay transforms,
// colliders, camera settings, source colors, or color-layer object lists.
[ExecuteAlways]
[DefaultExecutionOrder(1600)]
public sealed class DoorBureauArt : MonoBehaviour
{
    public Sprite square;
    [HideInInspector] public Sprite steelWall; // Kept only for existing serialized scenes.
    public Material surfaceMaterial;
    [Range(1, 3)] public int roomNumber = 1;

    private sealed class Surface
    {
        public SpriteRenderer source;
        public GameObject art;
        public SpriteRenderer[] indicators;
    }
    private readonly List<Surface> surfaces = new List<Surface>();
    private readonly List<SpriteRenderer> modeLamps = new List<SpriteRenderer>();
    private GameObject generated;
    private ColorLayerManager colors;
    private Font labelFont;
    private bool built;
    private SpriteRenderer upperWall, lowerWall, wallDivider, fascia;
    [Header("Background color modes")]
    public Color blueWallColor = new Color(.75f, .86f, .92f, 1f);
    public Color redWallColor = new Color(.94f, .78f, .71f, 1f);
    public Color blueLowerWallColor = new Color(.58f, .73f, .80f, 1f);
    public Color redLowerWallColor = new Color(.80f, .60f, .53f, 1f);
    // Flat painted surfaces and paper notices, not the Jerry steel-room palette.
    private static readonly Color Metal = new Color(.24f, .39f, .33f, 1f);
    private static readonly Color Edge = new Color(.73f, .81f, .68f, 1f);
    private static readonly Color Dark = new Color(.12f, .22f, .19f, 1f);
    private static readonly Color Paper = new Color(1f, .98f, .88f, 1f);
    private static readonly Color Blue = new Color(.08f, .52f, .77f, 1f);
    private static readonly Color Red = new Color(.85f, .25f, .15f, 1f);
    private static readonly Color Amber = new Color(.95f, .59f, .14f, 1f);

    private void OnEnable() { built = false; }

    private void LateUpdate()
    {
        if (!built) Build();
        if (!built) return;
        ApplyRoomMode();
        foreach (Surface surface in surfaces)
        {
            if (surface.source == null || surface.art == null) continue;
            // The elevator feedback animates a renderer copy, not the real plate.
            if (surface.source.name == "DoorLift" && !surface.source.enabled)
            {
                Transform animated = surface.source.transform.Find("AnimatedLiftVisual/Plate");
                if (animated != null)
                {
                    SpriteRenderer copy = animated.GetComponent<SpriteRenderer>();
                    if (copy != null)
                    {
                        surface.art.transform.SetParent(animated, false);
                        surface.source = copy;
                    }
                }
            }
            bool visible = surface.source.enabled &&
                surface.source.gameObject.activeInHierarchy;
            surface.art.SetActive(visible);
            if (surface.indicators != null)
                foreach (SpriteRenderer indicator in surface.indicators)
                    if (indicator != null)
                        indicator.color = surface.source.color;
        }
    }

    private void ApplyRoomMode()
    {
        bool isBlue = colors == null || colors.IsBlueBackground;
        Color mode = isBlue ? Blue : Red;
        if (upperWall != null)
            upperWall.color = isBlue ? blueWallColor : redWallColor;
        if (lowerWall != null)
            lowerWall.color = isBlue ? blueLowerWallColor : redLowerWallColor;
        if (wallDivider != null)
            wallDivider.color = isBlue ?
                new Color(.34f,.52f,.60f,1f) : new Color(.60f,.37f,.30f,1f);
        if (fascia != null)
            fascia.color = isBlue ?
                new Color(.12f,.24f,.30f,1f) : new Color(.31f,.17f,.14f,1f);
        foreach (SpriteRenderer lamp in modeLamps)
            if (lamp != null) lamp.color = mode;
    }

    [ContextMenu("Rebuild Bureau Art")]
    public void Build()
    {
        Clear();
        if (square == null || surfaceMaterial == null)
            return;

        // Snapshot the original scene objects before generating decorations.
        var originals = new List<Transform>();
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            if (root != gameObject)
                originals.AddRange(root.GetComponentsInChildren<Transform>(true));
        Camera camera = null;
        foreach (Transform item in originals)
        {
            if (colors == null) colors = item.GetComponent<ColorLayerManager>();
            if (item.name == "Main Camera") camera = item.GetComponent<Camera>();
        }
        if (camera == null) return;

        labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        generated = NewObject("BureauArt_Generated_DoNotSave", transform);
        float halfH = camera.orthographicSize;
        float halfW = halfH * Mathf.Max(16f / 9f, camera.aspect);
        Vector2 center = camera.transform.position;
        float left = center.x - halfW;
        float right = center.x + halfW;
        float top = center.y + halfH;
        float bottom = center.y - halfH;
        float floorTop = center.y - halfH * .45f;
        Transform floor = originals.Find(t => t.name == "LowerStart" &&
            t.gameObject.activeInHierarchy);
        if (floor == null)
            floor = originals.Find(t => t.name == "StartGround" &&
                t.gameObject.activeInHierarchy);
        if (floor != null)
        {
            SpriteRenderer floorRenderer = floor.GetComponent<SpriteRenderer>();
            if (floorRenderer != null && floorRenderer.sprite != null)
            {
                Bounds b = floorRenderer.sprite.bounds;
                floorTop = floor.TransformPoint(
                    new Vector3(b.center.x, b.max.y, 0f)).y;
            }
        }

        upperWall = SpritePart(generated.transform, "Color-mode office wall",
            center, new Vector2(halfW * 2f + 12f, halfH * 2f + 10f),
            new Color(.91f, .9f, .84f, 1f), -220, square);
        lowerWall = Part(generated.transform, "Office wall lower paint", center.x,
            floorTop - halfH * .55f, halfW * 2f + 12f, halfH + .8f,
            new Color(.79f,.83f,.75f,1f), -215);
        wallDivider = Part(generated.transform, "Office wall dado", center.x,
            floorTop - .75f, halfW * 2f + 12f, .06f,
            new Color(.54f,.64f,.54f,1f), -210);
        // Dark fascia keeps the existing white HUD legible on the light wall.
        fascia = Part(generated.transform, "Office fascia", center.x, top - .48f,
            halfW * 2f + 12f, 1.2f, Dark, -205);
        Frame(generated.transform, "Unstaffed service window", left + halfW * .52f,
            floorTop + 5.8f, 4.6f, 1.55f, -190);
        Text(generated.transform, "NEXT: NEVER",
            new Vector2(left + halfW * .52f, floorTop + 5.95f),
            3.9f, .62f, Paper, -185);
        Text(generated.transform, "TAKE A NUMBER",
            new Vector2(left + halfW * .52f, floorTop + 5.42f),
            3.9f, .30f, Paper, -185);
        for (int i = 0; i < 3; i++)
            modeLamps.Add(Part(generated.transform, "Room status lamp",
                left + halfW * .52f + .9f + i * .16f,
                floorTop + 5.1f, .08f, .07f, Blue, -185));

        string[] subtitles = { "INSPECTION", "APPROVAL", "COMPLAINTS" };
        // Placed beneath the HUD area instead of across the gameplay path.
        Sign(generated.transform, "Room identification",
            left + halfW * .52f, floorTop + 3.45f,
            6.2f, 1.4f, "EXIT OFFICE 0" + roomNumber,
            subtitles[Mathf.Clamp(roomNumber - 1, 0, 2)], -180);
        Sign(generated.transform, "Bureau notice",
            left + halfW * .47f, floorTop + 1.65f,
            5.8f, 1.35f,
            roomNumber == 1 ? "DO NOT CHASE" :
            roomNumber == 2 ? "GET APPROVAL" : "EXIT DENIED",
            roomNumber == 1 ? "ON BREAK" :
            roomNumber == 2 ? "PERMIT FIRST" : "GOOD LUCK", -180);

        // A single oversized administrative stamp carries the visual joke.
        Stamp(generated.transform, center.x - halfW * .05f,
            Mathf.Min(floorTop + 5.6f, top - 2.2f),
            roomNumber == 1 ? "ON BREAK" : roomNumber == 2 ? "PENDING" : "DENIED");

        // Flat footprints painted on a real platform, never across a gap.
        if (floor != null)
        {
            SpriteRenderer floorRenderer = floor.GetComponent<SpriteRenderer>();
            if (floorRenderer != null)
            {
                Bounds floorBounds = floorRenderer.bounds;
                for (int i = 0; i < 3; i++)
                {
                    float x = floorBounds.min.x + 1.5f + i * .65f;
                    if (x > floorBounds.max.x - .4f) break;
                    Part(generated.transform, "Queue footprint", x,
                        floorTop + .025f, .18f, .035f, Edge, 5);
                    Part(generated.transform, "Queue footprint", x + .24f,
                        floorTop + .025f, .18f, .035f, Edge, 5);
                }
            }
        }

        foreach (Transform item in originals)
        {
            SpriteRenderer source = item.GetComponent<SpriteRenderer>();
            if (source == null || source.sprite == null) continue;
            string n = item.name;
            bool platform = n == "StartGround" || n == "EndGround" ||
                n == "TrapPlatfoem" || n == "TrapPlatform" ||
                n == "LowerStart" || n == "LowerMiddle" || n == "LowerRight" ||
                n == "UpperLanding" || n == "UpperCapture" ||
                n == "BlueStep1" || n == "BlueStep2" || n == "BlueStep3" ||
                n == "DoorLift";
            if (platform) DressPlatform(source, n.StartsWith("BlueStep"));
            else if (n == "ExitDoor") DressDoor(source);
            else if (n == "BlueBarrier") DressBarrier(source);
            else if (n == "HideBox") DressBox(source);
            else if (n == "CapturePad") DressPad(source);
        }

        // Static rails sit behind the actual moving plate.
        if (roomNumber == 3)
        {
            Transform lift = originals.Find(t => t.name == "DoorLift");
            Transform door = originals.Find(t => t.name == "ExitDoor");
            if (lift != null && door != null)
            {
                // The configured Door03 route spans 6.2 world units.
                float railBottom = -3.2f;
                float railTop = 3.45f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = lift.position.x + side * .95f;
                    Part(generated.transform, "Door-only lift guide", x,
                        (railBottom + railTop) * .5f, .075f,
                        railTop - railBottom, new Color(.48f,.60f,.48f,1f), -175);
                }
                Sign(generated.transform, "Door-only lift sign",
                    Mathf.Min(right - 1.4f, lift.position.x + 2f),
                    .0f, 3.3f, 1.2f, "DOORS ONLY", "NO HUMANS", -170);
            }
            Transform spike = originals.Find(t => t.name == "FallingSpike");
            if (spike != null)
            {
                float x = spike.position.x;
                float y = 8.25f;
                Part(generated.transform, "Employee care dispenser", x, y,
                    2.3f, .38f, Paper, -4);
                Part(generated.transform, "Spike hatch lip", x, y - .14f,
                    2.3f, .07f, Metal, -3);
                for (int i = 0; i < 3; i++)
                    Part(generated.transform, "Hatch caution mark",
                        x - .6f + i * .6f, y + .02f, .10f, .12f, Red, -2);
                Text(generated.transform, "EMPLOYEE CARE", new Vector2(x, y + .4f),
                    2.6f, .30f, Dark, -170);
            }
        }
        built = true;
        ApplyRoomMode();
    }

    private void DressPlatform(SpriteRenderer source, bool colored)
    {
        Bounds b = source.sprite.bounds;
        var art = NewObject("Bureau_Surface", source.transform);
        float x = b.center.x, y = b.center.y, w = b.size.x, h = b.size.y;
        int order = source.sortingOrder + 1;
        float sy = Mathf.Max(.01f, Mathf.Abs(source.transform.lossyScale.y));
        Part(art.transform, "Painted office walkway", x, y, w, h,
            colored ? new Color(.36f,.55f,.62f,1f) : Metal, order);
        Part(art.transform, "Flat underside", x, y - h * .39f,
            w, h * .12f, Dark, order + 1);
        SpriteRenderer trim = Part(art.transform, "Walkable top edge",
            x, b.max.y - .025f / sy, w, Mathf.Min(h * .16f, .055f / sy),
            colored ? source.color : Edge, order + 2);
        // No rivets, grooves or industrial texture on the playable surfaces.
        Watch(source, art, colored ? new[] { trim } : null);
    }

    private void DressDoor(SpriteRenderer source)
    {
        var art = NewObject("Bureau_DoorSkin", source.transform);
        Bounds b = source.sprite.bounds;
        int order = source.sortingOrder + 2;
        float x = b.center.x, y = b.center.y, w = b.size.x, h = b.size.y;
        Part(art.transform, "Runaway office door", x, y, w * .93f, h * .95f,
            new Color(.39f,.57f,.39f,1f), order);
        for (int side = -1; side <= 1; side += 2)
            Part(art.transform, "Green frame", x + side * w * .45f, y,
                w * .055f, h * .95f, Dark, order + 1);
        Part(art.transform, "Exit sign card", x, y + h * .20f,
            w * .62f, h * .18f, Paper, order + 1);
        Part(art.transform, "Exit arrow stem", x, y + h * .20f,
            w * .32f, h * .025f, Dark, order + 2);
        SpriteRenderer arrow = Part(art.transform, "Exit arrow upper",
            x + w * .12f, y + h * .23f, w * .09f, h * .02f, Dark, order + 2);
        arrow.transform.localRotation = Quaternion.Euler(0f,0f,-40f);
        arrow = Part(art.transform, "Exit arrow lower",
            x + w * .12f, y + h * .17f, w * .09f, h * .02f, Dark, order + 2);
        arrow.transform.localRotation = Quaternion.Euler(0f,0f,40f);
        Part(art.transform, "Door employee badge", x - w * .12f, y - h * .15f,
            w * .25f, h * .12f, Paper, order + 1);
        Part(art.transform, "Door handle", x + w * .29f, y - h * .14f,
            w * .13f, h * .025f, Edge, order + 2);
        Part(art.transform, "Handle base", x + w * .34f, y - h * .12f,
            w * .035f, h * .11f, Metal, order + 1);
        Watch(source, art, null);
    }

    private void DressBarrier(SpriteRenderer source)
    {
        var art = NewObject("Bureau_InspectionBarrier", source.transform);
        Bounds b = source.sprite.bounds;
        int order = source.sortingOrder + 1;
        Part(art.transform, "Barrier casing", b.center.x, b.center.y,
            b.size.x, b.size.y, Dark, order);
        SpriteRenderer lens = Part(art.transform, "Barrier state strip",
            b.center.x, b.center.y, b.size.x * .45f, b.size.y * .92f,
            Blue, order + 1);
        modeLamps.Add(lens);
        Watch(source, art, null);
    }

    private void DressBox(SpriteRenderer source)
    {
        var art = NewObject("Bureau_CoverCabinet", source.transform);
        Bounds b = source.sprite.bounds;
        int order = source.sortingOrder + 1;
        Part(art.transform, "Cabinet body", b.center.x, b.center.y,
            b.size.x, b.size.y, Metal, order);
        Part(art.transform, "Cabinet top", b.center.x, b.max.y - b.size.y * .025f,
            b.size.x, b.size.y * .05f, Edge, order + 1);
        for (int i = 0; i < 2; i++)
        {
            float y = b.min.y + b.size.y * (.27f + .45f * i);
            Part(art.transform, "Drawer seam", b.center.x, y,
                b.size.x * .86f, b.size.y * .02f, Dark, order + 1);
            Part(art.transform, "Drawer handle", b.center.x, y + b.size.y * .13f,
                b.size.x * .22f, b.size.y * .025f, Edge, order + 1);
        }
        Watch(source, art, null);
    }

    private void DressPad(SpriteRenderer source)
    {
        var art = NewObject("Bureau_ApprovalPlate", source.transform);
        Bounds b = source.sprite.bounds;
        int order = source.sortingOrder + 1;
        Part(art.transform, "Approval plate casing", b.center.x, b.center.y,
            b.size.x, b.size.y, Dark, order);
        SpriteRenderer state = Part(art.transform, "Approval state strip",
            b.center.x, b.center.y, b.size.x * .86f, b.size.y * .45f,
            source.color, order + 1);
        Watch(source, art, new[] { state });
    }

    private void Watch(SpriteRenderer source, GameObject art, SpriteRenderer[] indicators)
    {
        surfaces.Add(new Surface { source = source, art = art, indicators = indicators });
    }

    private void Frame(Transform parent, string name, float x, float y, float w, float h, int order)
    {
        Part(parent, name + " painted surround", x, y, w, h, Metal, order);
        Part(parent, name + " closed service opening", x, y,
            w - .12f, h - .12f, Dark, order + 1);
        Part(parent, name + " counter", x, y - h * .5f,
            w + .25f, .11f, Paper, order + 2);
    }

    private void Sign(Transform parent, string name, float x, float y,
        float w, float h, string heading, string subtitle, int order)
    {
        Part(parent, name + " notice shadow", x+.045f,y-.045f,w,h,
            new Color(.64f,.68f,.59f,1f),order);
        Part(parent, name + " paper notice", x,y,w,h,Paper,order+1);
        Part(parent, name + " notice clip", x,y+h*.5f-.02f,
            .38f,.11f,Metal,order+3);
        Part(parent, name + " red margin", x-w*.5f+.07f,y,
            .035f,h-.16f,Red,order+2);
        Text(parent, heading, new Vector2(x,y+.24f),w-.35f,.60f,
            Dark,order+2);
        Text(parent, subtitle, new Vector2(x,y-.32f),w-.35f,.44f,
            new Color(.20f,.29f,.23f,1f),order+2);
        modeLamps.Add(Part(parent,name+" lamp",x-w*.5f+.13f,y+h*.5f-.13f,
            .07f,.07f,Blue,order+3));
    }

    private void Stamp(Transform parent, float x, float y, string caption)
    {
        var stamp = NewObject("Bureau rubber stamp", parent);
        stamp.transform.localPosition = new Vector3(x,y,0f);
        stamp.transform.localRotation = Quaternion.Euler(0f,0f,-11f);
        Color ink = new Color(.72f,.18f,.12f,.85f);
        Part(stamp.transform,"Stamp top",0f,.58f,3.9f,.055f,ink,-176);
        Part(stamp.transform,"Stamp bottom",0f,-.58f,3.9f,.055f,ink,-176);
        Part(stamp.transform,"Stamp left",-1.95f,0f,.055f,1.21f,ink,-176);
        Part(stamp.transform,"Stamp right",1.95f,0f,.055f,1.21f,ink,-176);
        Text(stamp.transform,caption,Vector2.zero,3.65f,.8f,ink,-175);
    }

    private void Text(Transform parent, string text, Vector2 position,
        float maxWidth, float height, Color tint, int order)
    {
        // UI Text participates in the font-atlas rebuild system. The old
        // tiny TextMesh glyphs could become fragmented after HUD font updates.
        const float pixelScale = .01f;
        var obj = new GameObject(text, typeof(RectTransform), typeof(Canvas));
        obj.hideFlags = HideFlags.DontSave;
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.localPosition = new Vector3(position.x, position.y, -.06f);
        rect.localScale = Vector3.one * pixelScale;
        rect.sizeDelta = new Vector2(maxWidth / pixelScale, height / pixelScale);
        Canvas canvas = obj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = order;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
        RectMask2D clip = obj.AddComponent<RectMask2D>();
        clip.padding = new Vector4(2f, 1f, 2f, 1f);

        var textObject = new GameObject("Readable label", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
        textObject.hideFlags = HideFlags.DontSave;
        textObject.transform.SetParent(rect, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        UnityEngine.UI.Text label = textObject.GetComponent<UnityEngine.UI.Text>();
        label.font = labelFont;
        label.text = text;
        label.fontSize = height >= .5f ? 64 : 44;
        label.fontStyle = FontStyle.Bold;
        label.resizeTextForBestFit = false;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.color = tint;
        label.raycastTarget = false;

        // Fit the complete single line using real font metrics. A minimum
        // font size must never force a narrow sign to overflow its panel.
        TextGenerationSettings measure = label.GetGenerationSettings(Vector2.zero);
        measure.scaleFactor = 1f;
        measure.resizeTextForBestFit = false;
        measure.horizontalOverflow = HorizontalWrapMode.Overflow;
        measure.verticalOverflow = VerticalWrapMode.Overflow;
        float availableWidth = Mathf.Max(1f, rect.sizeDelta.x - 12f);
        float availableHeight = Mathf.Max(1f, rect.sizeDelta.y - 6f);
        float preferredWidth = label.cachedTextGeneratorForLayout
            .GetPreferredWidth(text, measure);
        float preferredHeight = label.cachedTextGeneratorForLayout
            .GetPreferredHeight(text, measure);
        float fit = Mathf.Min(1f,
            availableWidth / Mathf.Max(1f, preferredWidth),
            availableHeight / Mathf.Max(1f, preferredHeight));
        label.fontSize = Mathf.Max(1, Mathf.FloorToInt(label.fontSize * fit));
    }

    private SpriteRenderer Part(Transform parent, string name, float x, float y,
        float w, float h, Color tint, int order)
    {
        return SpritePart(parent,name,new Vector2(x,y),new Vector2(w,h),tint,order,square);
    }

    private SpriteRenderer SpritePart(Transform parent, string name, Vector2 position,
        Vector2 size, Color tint, int order, Sprite sprite)
    {
        var obj = NewObject(name,parent);
        obj.transform.localPosition = new Vector3(position.x,position.y,0f);
        Bounds bounds = sprite.bounds;
        obj.transform.localScale = new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,1f);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = surfaceMaterial;
        renderer.color = tint;
        renderer.sortingOrder = order;
        return renderer;
    }

    private GameObject NewObject(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.hideFlags = HideFlags.DontSave;
        obj.transform.SetParent(parent,false);
        return obj;
    }

    private void Clear()
    {
        foreach (Surface surface in surfaces)
            if (surface.art != null) Remove(surface.art);
        surfaces.Clear();
        modeLamps.Clear();
        if (generated != null) Remove(generated);
        generated = null;
        upperWall = lowerWall = wallDivider = fascia = null;
        built = false;
        colors = null;
    }

    private static void Remove(GameObject obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
    private void OnDisable() { Clear(); }
}
