using UnityEngine;

// A shallow ramp supports walking onto the pad. Its sensor stays fixed while
// the support surface and cap move, so pressing never toggles the gate by itself.
[DefaultExecutionOrder(1100)]
[RequireComponent(typeof(Button), typeof(BoxCollider2D))]
public sealed class JerryPressurePlate : MonoBehaviour
{
    public BoxCollider2D sensor;
    public PolygonCollider2D surface;
    public SpriteRenderer colorSource;
    public SpriteRenderer cap;
    public Transform[] capDetails = new Transform[0];
    [Range(0f, .8f)] public float travelFraction = .55f;
    [Min(.03f)] public float pressDuration = .10f;
    [Min(.03f)] public float releaseDuration = .16f;
    [Min(.005f)] public float weightTolerance = .045f;

    public float SurfaceTop => surface != null && surface.enabled ? surface.bounds.max.y : sensor.bounds.max.y;
    public float PressAmount => amount;

    private readonly Collider2D[] contacts = new Collider2D[128];
    private readonly Vector2[] points = new Vector2[4];
    private Vector3[] detailPositions;
    private Vector3 capPosition, capScale;
    private ContactFilter2D filter;
    private Vector2 sensorCenter, sensorSize;
    private float baseY, amount;
    private bool initialized;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized || sensor == null || surface == null || cap == null || colorSource == null)
            return;
        sensorCenter = sensor.offset;
        sensorSize = sensor.size;
        baseY = sensorCenter.y - sensorSize.y * .5f;
        filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        detailPositions = new Vector3[capDetails.Length];
        capPosition = cap.transform.localPosition;
        capScale = cap.transform.localScale;
        for (int i = 0; i < capDetails.Length; i++)
            if (capDetails[i] != null)
                detailPositions[i] = capDetails[i].localPosition;
        colorSource.forceRenderingOff = true;
        initialized = true;
        UpdateSurface();
        UpdateVisual();
    }

    private void FixedUpdate()
    {
        Initialize();
        if (!initialized)
            return;
        surface.enabled = sensor.enabled && sensor.gameObject.activeInHierarchy;
        if (!surface.enabled)
            return;
        bool weighted = HasWeight();
        float duration = weighted ? pressDuration : releaseDuration;
        float nextAmount = Mathf.MoveTowards(amount, weighted ? 1f : 0f,
            Time.fixedDeltaTime / Mathf.Max(.03f, duration));
        if (!Mathf.Approximately(amount, nextAmount))
        {
            amount = nextAmount;
            UpdateSurface();
        }
    }

    private bool HasWeight()
    {
        Bounds b = sensor.bounds;
        int count = gameObject.scene.GetPhysicsScene2D().OverlapBox(b.center,
            (Vector2)b.size + Vector2.one * weightTolerance * 2f, 0f, filter, contacts);
        float top = SurfaceTop;
        for (int i = 0; i < count; i++)
        {
            Collider2D other = contacts[i];
            if (other == null || other == surface || other == sensor)
                continue;
            PlayerRespawnJ player = other.GetComponentInParent<PlayerRespawnJ>();
            bool corpse = other.GetComponentInParent<Corpse>() != null;
            if (!corpse && (player == null || player.IsRespawning))
                continue;
            if (other.bounds.center.y <= top || other.bounds.min.y < top - weightTolerance)
                continue;
            ColliderDistance2D distance = surface.Distance(other);
            if (distance.isValid && distance.distance <= weightTolerance)
                return true;
        }
        return false;
    }

    private void UpdateSurface()
    {
        float ratio = 1f - travelFraction * amount;
        float sx = Mathf.Max(.0001f, Mathf.Abs(transform.lossyScale.x));
        float ramp = Mathf.Min(.35f, sensorSize.x * sx * .35f) / sx;
        float left = sensorCenter.x - sensorSize.x * .5f, right = sensorCenter.x + sensorSize.x * .5f;
        float top = baseY + sensorSize.y * ratio;
        points[0] = new Vector2(left, baseY);
        points[1] = new Vector2(left + ramp, top);
        points[2] = new Vector2(right - ramp, top);
        points[3] = new Vector2(right, baseY);
        surface.SetPath(0, points);
    }

    private void LateUpdate()
    {
        Initialize();
        if (initialized)
            UpdateVisual();
    }

    private void UpdateVisual()
    {
        float worldTravel = sensorSize.y * Mathf.Abs(transform.lossyScale.y) * travelFraction * amount;
        cap.color = colorSource.color;
        cap.sortingLayerID = colorSource.sortingLayerID;
        cap.sortingOrder = colorSource.sortingOrder + 2;
        cap.enabled = colorSource.enabled && sensor.enabled;
        // The readable housing is inset into the floor. Only the top travels;
        // its visual height no longer depends on the shallow collision ramp.
        Vector3 localTravel = cap.transform.parent.InverseTransformVector(Vector3.down * worldTravel);
        cap.transform.localPosition = capPosition + localTravel * .5f;
        cap.transform.localScale = new Vector3(capScale.x,
            Mathf.Max(.0001f, capScale.y + localTravel.y / cap.sprite.bounds.size.y), capScale.z);
        for (int i = 0; i < capDetails.Length; i++)
        {
            Transform detail = capDetails[i];
            if (detail == null)
                continue;
            detail.localPosition = detailPositions[i] + detail.parent.InverseTransformVector(Vector3.down * worldTravel);
        }
    }

    private void OnDisable()
    {
        if (!initialized)
            return;
        amount = 0;
        UpdateSurface(); UpdateVisual();
        surface.enabled = false;
    }
}
