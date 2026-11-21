using UnityEngine;

// Cosmetic only: does not move the door, plate, player, or any collider.
[DefaultExecutionOrder(300)]
[RequireComponent(typeof(BoxCollider2D))]
public class Door03VisualFeedback : MonoBehaviour
{
    public Door03LiftRoute route;
    public DoorCameraVision cameraVision;
    [Header("Optional overrides; leave empty for automatic setup")]
    public Transform player;
    public SpriteRenderer capturePad;
    public Transform liftPlate;
    [Range(0f, 25f)] public float scanAngle = 12f;
    [Min(0.1f)] public float scanSpeed = 2f;
    [Range(0f, 0.08f)] public float liftShake = 0.025f;

    private BoxCollider2D exitCollider;
    private Material lineMaterial;
    private GameObject effects;
    private Transform cameraVisual, liftVisual;
    private SpriteRenderer originalCamera, originalEye, originalLift;
    private SpriteRenderer cameraCopy, eyeCopy, liftCopy;
    private Vector3 eyeBaseScale;
    private bool cameraVisible, eyeVisible, liftVisible;
    private LineRenderer lens, padOutline, captureRing, captureBrackets;
    private LineRenderer liftRing;
    private LineRenderer[] thrust;
    private float lastLiftY, liftEventAge = 100f, captureAge;
    private bool wasMoving, wasCaptured, ready;
    private float clock;
    private readonly Color cyan = new Color(0.2f, 0.8f, 1f, 0.9f);
    private readonly Color green = new Color(0.25f, 1f, 0.45f, 1f);

    private void Start()
    {
        if (route == null) route = GetComponent<Door03LiftRoute>();
        if (cameraVision == null)
            cameraVision = GetComponentInChildren<DoorCameraVision>();
        if (route != null && route.isActiveAndEnabled)
        {
            if (player == null) player = route.player;
            if (capturePad == null) capturePad = route.capturePad;
            if (liftPlate == null) liftPlate = route.liftPlate;
        }
        if (player == null && cameraVision != null)
            player = cameraVision.player;
        if (capturePad == null)
        {
            HerdingDoor herdingDoor = GetComponent<HerdingDoor>();
            if (herdingDoor != null)
            {
                foreach (DoorCaptureZone zone in
                    FindObjectsByType<DoorCaptureZone>(FindObjectsSortMode.None))
                {
                    if (zone.door == herdingDoor)
                    {
                        capturePad = zone.indicator;
                        break;
                    }
                }
            }
        }
        exitCollider = GetComponent<BoxCollider2D>();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null || exitCollider == null)
        {
            Debug.LogError("Door feedback: missing exit collider or sprite shader.", this);
            enabled = false;
            return;
        }

        lineMaterial = new Material(shader);
        effects = new GameObject("Door03_VisualEffects");
        effects.transform.SetParent(transform, false);

        if (cameraVision != null && cameraVision.cameraEye != null)
        {
            originalEye = cameraVision.cameraEye;
            originalCamera = cameraVision.GetComponent<SpriteRenderer>();
            if (originalCamera != null)
            {
                cameraVisible = originalCamera.enabled;
                eyeVisible = originalEye.enabled;
                cameraVisual = new GameObject("AnimatedCameraVisual").transform;
                cameraVisual.SetParent(originalCamera.transform, false);
                cameraCopy = CopySprite(originalCamera, cameraVisual, "Body");
                eyeCopy = CopySprite(originalEye, cameraVisual, "Eye");
                eyeCopy.transform.localPosition =
                    cameraVisual.InverseTransformPoint(originalEye.transform.position);
                Vector3 relativeScale = originalEye.transform.localScale;
                eyeCopy.transform.localScale = relativeScale;
                eyeBaseScale = relativeScale;
            }
        }

        if (liftPlate != null)
        {
            lastLiftY = liftPlate.position.y;
            originalLift = liftPlate.GetComponent<SpriteRenderer>();
            if (originalLift != null)
            {
                liftVisible = originalLift.enabled;
                liftVisual = new GameObject("AnimatedLiftVisual").transform;
                liftVisual.SetParent(originalLift.transform, false);
                liftCopy = CopySprite(originalLift, liftVisual, "Plate");
            }
        }

        lens = MakeLine("CameraLens", 12);
        padOutline = MakeLine("CapturePadOutline", 10);
        captureRing = MakeLine("CaptureSuccessRing", 12);
        captureBrackets = MakeLine("CaptureLockBrackets", 11);
        liftRing = MakeLine("LiftArrivalRing", 10);
        thrust = new LineRenderer[6];
        for (int i = 0; i < thrust.Length; i++)
            thrust[i] = MakeLine("LiftTrail_" + i, 8);
        ready = true;
    }

    private SpriteRenderer CopySprite(SpriteRenderer source, Transform parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var copy = obj.AddComponent<SpriteRenderer>();
        copy.sprite = source.sprite;
        copy.sharedMaterial = source.sharedMaterial;
        copy.color = source.color;
        copy.flipX = source.flipX;
        copy.flipY = source.flipY;
        copy.sortingLayerID = source.sortingLayerID;
        copy.sortingOrder = source.sortingOrder + 1;
        return copy;
    }

    private LineRenderer MakeLine(string name, int order)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(effects.transform, false);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = lineMaterial;
        line.useWorldSpace = true;
        line.sortingOrder = order;
        line.numCapVertices = 3;
        line.numCornerVertices = 2;
        line.enabled = false;
        return line;
    }

    private void LateUpdate()
    {
        if (!ready || Time.deltaTime <= 0f) return;
        clock += Time.deltaTime;
        effects.SetActive(true);
        // Both HerdingDoor and Door03LiftRoute enable the exit upon capture.
        bool captured = exitCollider.enabled;
        if (captured && !wasCaptured) captureAge = 0f;
        if (captured) captureAge += Time.deltaTime;
        AnimateCamera(captured);
        AnimatePad(captured);
        AnimateLift();
        wasCaptured = captured;
    }

    private void AnimateCamera(bool captured)
    {
        lens.enabled = false;
        if (cameraVisual == null || originalEye == null || originalCamera == null)
            return;
        originalCamera.enabled = false;
        originalEye.enabled = false;
        cameraVisual.gameObject.SetActive(true);
        bool tracking = cameraVision.enabled && cameraVision.CanSeePlayer;
        float angle = Mathf.Sin(clock * scanSpeed) * Mathf.Min(scanAngle, 4f);
        float facing = -1f;
        if (player != null &&
            player.position.x > cameraVision.transform.position.x)
            facing = 1f;
        if (tracking) angle = facing < 0f ? -2f : 2f;
        if (captured) angle = Mathf.Lerp(0f, -12f,
            Mathf.SmoothStep(0f, 1f, captureAge / 0.65f));
        cameraVisual.localRotation = Quaternion.Slerp(
            cameraVisual.localRotation, Quaternion.Euler(0f, 0f, angle),
            1f - Mathf.Exp(-8f * Time.deltaTime));
        cameraCopy.color = originalCamera.color;
        Vector3 eyePosition = eyeCopy.transform.localPosition;
        float targetEyeX = captured ? 0f :
            tracking ? facing * 0.28f : Mathf.Sin(clock * scanSpeed) * 0.25f;
        eyePosition.x = Mathf.MoveTowards(
            eyePosition.x, targetEyeX, Time.deltaTime * 0.9f);
        eyeCopy.transform.localPosition = eyePosition;
        Color light = captured ? green : (tracking ? Color.red : Color.yellow);
        if (captured && captureAge > 0.8f)
            light = new Color(0.12f, 0.2f, 0.16f, 1f);
        eyeCopy.color = light;
        // Keep the small original lamp shape, without an oversized glowing eye.
        float closeAmount = captured ?
            Mathf.SmoothStep(0f, 1f, captureAge / 0.65f) : 0f;
        eyeCopy.transform.localScale = new Vector3(
            eyeBaseScale.x,
            eyeBaseScale.y * Mathf.Lerp(1f, 0.15f, closeAmount),
            eyeBaseScale.z);
    }

    private void AnimatePad(bool captured)
    {
        padOutline.enabled = captureRing.enabled = captureBrackets.enabled = false;
        if (capturePad == null || !capturePad.gameObject.activeInHierarchy)
            return;
        Bounds pad = capturePad.bounds;
        // Read the gameplay script's yellow/gray status; do not overwrite its color.
        Color status = capturePad.color;
        bool armed = status.r > 0.7f && status.g > 0.7f && status.b < 0.35f;
        Vector3 center = pad.center + Vector3.up * 0.04f;
        float halfWidth = Mathf.Max(0.3f, pad.extents.x);
        padOutline.enabled = true;
        Color light = captured ? green : armed ?
            new Color(1f, 0.75f, 0.15f, 0.9f) :
            new Color(0.4f, 0.5f, 0.6f, 0.35f);
        float pulse = armed ? (0.5f + 0.5f * Mathf.Sin(clock * 7f)) : 0f;
        Ellipse(padOutline, center, halfWidth + pulse * 0.12f,
            0.11f + pulse * 0.07f, 0.025f, light);
        if (!captured) return;

        float lockAmount = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(captureAge / 0.4f));
        captureBrackets.enabled = true;
        float w = halfWidth + Mathf.Lerp(0.4f, 0.08f, lockAmount);
        float h = 0.5f * lockAmount;
        captureBrackets.positionCount = 4;
        captureBrackets.SetPosition(0, center + new Vector3(-w, h, 0f));
        captureBrackets.SetPosition(1, center + new Vector3(-w, 0f, 0f));
        captureBrackets.SetPosition(2, center + new Vector3(w, 0f, 0f));
        captureBrackets.SetPosition(3, center + new Vector3(w, h, 0f));
        Style(captureBrackets, 0.045f, green);
        if (captureAge < 0.9f)
        {
            float t = captureAge / 0.9f;
            captureRing.enabled = true;
            Ellipse(captureRing, center + Vector3.up * t * 1.6f,
                halfWidth * (1f + t * 0.25f), 0.15f, 0.04f,
                new Color(green.r, green.g, green.b, 1f - t));
        }
    }

    private void AnimateLift()
    {
        liftRing.enabled = false;
        for (int i = 0; i < thrust.Length; i++) thrust[i].enabled = false;
        if (liftPlate == null || !liftPlate.gameObject.activeInHierarchy)
            return;
        float y = liftPlate.position.y;
        bool moving = Mathf.Abs(y - lastLiftY) > 0.0001f;
        lastLiftY = y;
        if (moving != wasMoving) liftEventAge = 0f;
        liftEventAge += Time.deltaTime;
        if (liftVisual != null)
        {
            originalLift.enabled = false;
            liftVisual.gameObject.SetActive(true);
            float shake = moving ? liftShake : 0f;
            // Move only the renderer copy. The actual lift transform stays untouched.
            Vector3 worldJitter = new Vector3(
                Mathf.Sin(clock * 70f) * shake,
                Mathf.Sin(clock * 53f) * shake * 0.35f, 0f);
            liftVisual.position = originalLift.transform.position + worldJitter;
            liftVisual.localScale = new Vector3(1f,
                1f + Mathf.Sin(clock * 30f) *
                Mathf.Exp(-liftEventAge * 9f) * 0.15f, 1f);
            liftCopy.color = moving ? Color.Lerp(originalLift.color, cyan, 0.35f)
                : originalLift.color;
        }
        Bounds plate = liftCopy != null ? liftCopy.bounds :
            new Bounds(liftPlate.position, new Vector3(1.6f, 0.2f, 0f));
        if (moving)
        {
            for (int i = 0; i < thrust.Length; i++)
            {
                float t = Mathf.Repeat(clock * 2.6f + i / 6f, 1f);
                float x = Mathf.Lerp(plate.min.x + 0.1f,
                    plate.max.x - 0.1f, (i + 0.5f) / 6f);
                Vector3 a = new Vector3(x, plate.min.y - 0.1f - t * 0.8f, 0f);
                thrust[i].enabled = true;
                thrust[i].positionCount = 2;
                thrust[i].SetPosition(0, a);
                thrust[i].SetPosition(1, a + Vector3.down * 0.15f);
                Style(thrust[i], 0.025f, new Color(0.2f, 0.8f, 1f, 1f - t));
            }
        }
        else if (wasMoving || liftEventAge < 0.55f)
        {
            float t = Mathf.Clamp01(liftEventAge / 0.55f);
            liftRing.enabled = true;
            Ellipse(liftRing, plate.center,
                plate.extents.x + t * 0.45f, 0.1f + t * 0.2f,
                0.03f, new Color(0.2f, 0.8f, 1f, 1f - t));
        }
        wasMoving = moving;
    }

    private static void Ellipse(LineRenderer line, Vector3 center,
        float rx, float ry, float width, Color color)
    {
        const int count = 40;
        line.loop = true;
        line.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            line.SetPosition(i, center + new Vector3(
                Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry, -0.02f));
        }
        Style(line, width, color);
    }

    private static void Style(LineRenderer line, float width, Color color)
    {
        line.startWidth = line.endWidth = width;
        line.startColor = line.endColor = color;
    }

    private void OnDisable()
    {
        if (!ready) return;
        if (originalCamera != null) originalCamera.enabled = cameraVisible;
        if (originalEye != null) originalEye.enabled = eyeVisible;
        if (originalLift != null) originalLift.enabled = liftVisible;
        if (cameraVisual != null) cameraVisual.gameObject.SetActive(false);
        if (liftVisual != null) liftVisual.gameObject.SetActive(false);
        if (effects != null) effects.SetActive(false);
    }

    private void OnDestroy()
    {
        if (cameraVisual != null) Destroy(cameraVisual.gameObject);
        if (liftVisual != null) Destroy(liftVisual.gameObject);
        if (effects != null) Destroy(effects);
        if (lineMaterial != null) Destroy(lineMaterial);
    }
}
