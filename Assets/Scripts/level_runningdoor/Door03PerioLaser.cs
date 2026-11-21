using UnityEngine;

public class Door03PeriodicLaser : MonoBehaviour
{
    // Keep the original references and timing values.
    public SpriteRenderer beam;
    public BoxCollider2D damageCollider;
    [Min(0.1f)] public float offTime = 2.4f;
    [Min(0.1f)] public float warningTime = 0.6f;
    [Min(0.1f)] public float activeTime = 1.2f;

    [Header("Lightning")]
    [Range(8, 48)] public int segments = 24;
    [Min(0.01f)] public float redrawInterval = 0.035f;
    [Min(0f)] public float electricWobble = 0.055f;
    [Min(0.005f)] public float coreWidth = 0.04f;
    [Min(0.05f)] public float sparkLength = 0.25f;

    private enum Phase { Off, Warning, Active }
    private Phase phase;
    private float timer;
    private float redrawTimer;
    private Material lineMaterial;
    private GameObject visuals;
    private LineRenderer core, blueEdge, halo, topRing, bottomRing;
    private LineRenderer[] sparks;
    private Vector3[] points;
    private bool ready;

    private void Awake()
    {
        if (beam != null)
            beam.enabled = false;
        if (damageCollider != null)
        {
            damageCollider.isTrigger = true;
            damageCollider.enabled = false;
        }
    }

    private void Start()
    {
        if (beam == null || damageCollider == null)
        {
            Debug.LogError("Lightning: assign Beam and Damage Collider.", this);
            enabled = false;
            return;
        }
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("Lightning: Sprites/Default shader is unavailable.", this);
            enabled = false;
            return;
        }

        lineMaterial = new Material(shader);
        visuals = new GameObject("Lightning_Visuals");
        visuals.transform.SetParent(transform, false);
        core = CreateLine("WhiteCore", 3);
        blueEdge = CreateLine("BlueEdge", 2);
        halo = CreateLine("SoftHalo", 1);
        topRing = CreateLine("EmitterRing", 4);
        bottomRing = CreateLine("ImpactRing", 4);
        sparks = new LineRenderer[5];
        for (int i = 0; i < sparks.Length; i++)
            sparks[i] = CreateLine("EmitterSpark_" + i, 4);
        ready = true;
        Enter(Phase.Off);
    }

    private LineRenderer CreateLine(string name, int sortingOffset)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(visuals.transform, false);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = lineMaterial;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 3;
        line.numCornerVertices = 2;
        line.sortingLayerID = beam.sortingLayerID;
        line.sortingOrder = beam.sortingOrder + sortingOffset;
        return line;
    }

    private void Update()
    {
        if (!ready || Time.deltaTime <= 0f)
            return;
        // The square sprite is only a layout guide; it never renders in play.
        beam.enabled = false;
        timer += Time.deltaTime;
        if (phase == Phase.Off && timer >= offTime)
            Enter(Phase.Warning);
        else if (phase == Phase.Warning && timer >= warningTime)
            Enter(Phase.Active);
        else if (phase == Phase.Active && timer >= activeTime)
            Enter(Phase.Off);

        if (phase == Phase.Off)
            return;

        // Damage does not depend on the randomly redrawn lightning.
        damageCollider.enabled = phase == Phase.Active;
        if (!damageCollider.gameObject.activeInHierarchy)
        {
            visuals.SetActive(false);
            return;
        }
        visuals.SetActive(true);
        redrawTimer -= Time.deltaTime;
        if (redrawTimer <= 0f)
        {
            redrawTimer = Mathf.Max(0.01f, redrawInterval);
            DrawLightning();
        }
    }

    private void Enter(Phase next)
    {
        phase = next;
        timer = 0f;
        redrawTimer = 0f;
        damageCollider.enabled = next == Phase.Active;
        visuals.SetActive(next != Phase.Off);
    }

    private void DrawLightning()
    {
        Transform source = damageCollider.transform;
        Vector2 center = damageCollider.offset;
        Vector2 size = damageCollider.size;
        Vector3 top = source.TransformPoint(center + Vector2.up * size.y * 0.5f);
        Vector3 bottom = source.TransformPoint(center - Vector2.up * size.y * 0.5f);
        Vector3 sideways = source.TransformVector(Vector3.right).normalized;
        float worldWidth = source.TransformVector(Vector3.right * size.x).magnitude;
        float wobble = Mathf.Min(electricWobble, worldWidth * 0.28f);
        float width = Mathf.Min(coreWidth, worldWidth * 0.25f);
        bool firing = phase == Phase.Active;
        // Warning is confined to the overhead emitter, not the walking route.
        core.enabled = firing;
        blueEdge.enabled = firing;
        halo.enabled = firing;
        bottomRing.enabled = firing;
        topRing.enabled = true;
        int count = Mathf.Clamp(segments, 8, 48) + 1;
        if (points == null || points.Length != count)
            points = new Vector3[count];

        // Endpoints stay fixed. Only intermediate points form the jagged bolt.
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            float offset = i == 0 || i == count - 1
                ? 0f : Random.Range(-wobble, wobble);
            points[i] = Vector3.Lerp(top, bottom, t) + sideways * offset;
        }

        float chargingPulse = 0.5f + 0.5f * Mathf.Sin(timer * 24f);
        Color charge = new Color(1f, 0.55f, 0.12f,
            Mathf.Lerp(0.35f, 0.95f, chargingPulse));
        Color electricBlue = new Color(0.1f, 0.55f, 1f, 0.9f);
        SetBolt(core, width,
            firing ? new Color(0.9f, 0.98f, 1f, 1f) : charge);
        SetBolt(blueEdge, width * 2.5f,
            firing ? electricBlue : new Color(1f, 0.65f, 0.05f, 0.18f));
        SetBolt(halo, width * 6f,
            firing ? new Color(0.15f, 0.45f, 1f, 0.16f) : Color.clear);

        Color ringColor = firing ? electricBlue : charge;
        DrawRing(topRing, top, firing ? 0.18f :
            Mathf.Lerp(0.1f, 0.22f,
                Mathf.Clamp01(timer / Mathf.Max(0.1f, warningTime))), ringColor);
        DrawRing(bottomRing, bottom, firing ? 0.14f : 0.08f, ringColor);

        // Small branches surround the upper emitter, away from the walking route.
        for (int i = 0; i < sparks.Length; i++)
        {
            sparks[i].enabled = firing;
            if (!firing)
                continue;
            Vector2 ray = Random.insideUnitCircle.normalized * sparkLength;
            Vector3 end = top + new Vector3(ray.x, ray.y, 0f);
            sparks[i].positionCount = 4;
            sparks[i].SetPosition(0, top);
            sparks[i].SetPosition(1, Vector3.Lerp(top, end, 0.35f) +
                sideways * Random.Range(-0.06f, 0.06f));
            sparks[i].SetPosition(2, Vector3.Lerp(top, end, 0.7f) -
                sideways * Random.Range(-0.06f, 0.06f));
            sparks[i].SetPosition(3, end);
            SetStyle(sparks[i], width * 0.45f,
                new Color(0.6f, 0.85f, 1f, 0.8f));
        }
    }

    private void SetBolt(LineRenderer line, float width, Color color)
    {
        line.positionCount = points.Length;
        line.SetPositions(points);
        SetStyle(line, width, color);
    }

    private void DrawRing(LineRenderer line, Vector3 center, float radius, Color color)
    {
        const int count = 20;
        line.loop = true;
        line.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            line.SetPosition(i, center + new Vector3(
                Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
        SetStyle(line, 0.02f, color);
    }

    private static void SetStyle(LineRenderer line, float width, Color color)
    {
        line.startWidth = line.endWidth = width;
        line.startColor = line.endColor = color;
    }

    private void OnDisable()
    {
        if (beam != null)
            beam.enabled = false;
        if (damageCollider != null)
            damageCollider.enabled = false;
        if (visuals != null)
            visuals.SetActive(false);
        phase = Phase.Off;
        timer = 0f;
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);
    }
}
