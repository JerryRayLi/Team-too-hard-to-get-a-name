using UnityEngine;

// Adds Jerry's geometric presentation to an existing player without changing
// that player's controller, collider, rigidbody, respawn, or level abilities.
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public sealed class SharedGeometricPlayerVisual : MonoBehaviour
{
    [Header("Visual References")]
    public SpriteRenderer sourceRenderer;
    public Transform visual;
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform leftArm;
    public Transform rightArm;

    [Header("Motion")]
    [Range(0f, 15f)] public float legSwing = 13.54f;
    [Min(0f)] public float walkFrequency = 11f;
    [Range(0f, .2f)] public float jumpStretch = .055f;
    [Range(0f, .25f)] public float landingSquash = .10f;
    [Min(.05f)] public float landingDuration = .16f;

    private Rigidbody2D body;
    private Collider2D shape;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[12];
    private ContactFilter2D groundFilter;
    private SpriteRenderer[] visualRenderers;
    private Color[] normalVisualColors;
    private Color normalSourceColor;
    private Vector3 restPosition;
    private Vector3 restScale;
    private Quaternion leftLegRest;
    private Quaternion rightLegRest;
    private Quaternion leftArmRest;
    private Quaternion rightArmRest;
    private bool initialized;
    private bool observedGround;
    private bool wasGrounded;
    private float phase;
    private float landingRemaining;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized || visual == null)
        {
            return;
        }

        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<Collider2D>();

        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponent<SpriteRenderer>();
        }

        visualRenderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
        normalVisualColors = new Color[visualRenderers.Length];
        for (int i = 0; i < visualRenderers.Length; i++)
        {
            normalVisualColors[i] = visualRenderers[i].color;
        }

        normalSourceColor = sourceRenderer != null
            ? sourceRenderer.color
            : Color.black;

        restPosition = visual.localPosition;
        restScale = visual.localScale;
        leftLegRest = leftLeg != null ? leftLeg.localRotation : Quaternion.identity;
        rightLegRest = rightLeg != null ? rightLeg.localRotation : Quaternion.identity;
        leftArmRest = leftArm != null ? leftArm.localRotation : Quaternion.identity;
        rightArmRest = rightArm != null ? rightArm.localRotation : Quaternion.identity;

        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(
            Physics2D.GetLayerCollisionMask(gameObject.layer)
        );
        groundFilter.useTriggers = false;
        initialized = true;
        SyncColorAndHideSource();
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        Initialize();
        if (!initialized || body == null)
        {
            return;
        }

        SyncColorAndHideSource();

        // Existing respawn scripts own renderer visibility. When they hide the
        // original renderer, leave the geometric renderers in their state and
        // only reset the pose.
        if (sourceRenderer != null && !sourceRenderer.enabled)
        {
            ResetPose();
            return;
        }

        bool grounded = false;
        if (shape != null && shape.enabled && body.linearVelocity.y <= .15f)
        {
            int count = shape.Cast(
                Vector2.down,
                groundFilter,
                groundHits,
                .045f
            );

            for (int i = 0; i < count; i++)
            {
                if (groundHits[i].collider != null &&
                    groundHits[i].normal.y > .55f)
                {
                    grounded = true;
                    break;
                }
            }
        }

        AnimatePose(body.linearVelocity, grounded, Time.deltaTime);
    }

    private void SyncColorAndHideSource()
    {
        if (sourceRenderer == null)
        {
            return;
        }

        Color sourceColor = sourceRenderer.color;
        bool hasStatusTint =
            Mathf.Abs(sourceColor.r - normalSourceColor.r) > .01f ||
            Mathf.Abs(sourceColor.g - normalSourceColor.g) > .01f ||
            Mathf.Abs(sourceColor.b - normalSourceColor.b) > .01f;

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            SpriteRenderer renderer = visualRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (hasStatusTint)
            {
                renderer.color = new Color(
                    sourceColor.r,
                    sourceColor.g,
                    sourceColor.b,
                    normalVisualColors[i].a
                );
            }
            else
            {
                renderer.color = normalVisualColors[i];
            }
        }

        // Keep the old renderer enabled so existing status and respawn scripts
        // can continue using it, but make its sprite visually transparent.
        sourceColor.a = 0f;
        sourceRenderer.color = sourceColor;
    }

    private void AnimatePose(
        Vector2 velocity,
        bool grounded,
        float deltaTime
    )
    {
        if (observedGround && grounded && !wasGrounded)
        {
            landingRemaining = landingDuration;
        }

        observedGround = true;
        wasGrounded = grounded;

        float moving = grounded
            ? Mathf.Clamp01(Mathf.Abs(velocity.x) / 6f)
            : 0f;

        phase += deltaTime * walkFrequency * Mathf.Lerp(.6f, 1f, moving);
        float swing = Mathf.Sin(phase) * legSwing * moving;

        Rotate(leftLeg, leftLegRest, swing);
        Rotate(rightLeg, rightLegRest, -swing);
        Rotate(leftArm, leftArmRest, -swing * .5f);
        Rotate(rightArm, rightArmRest, swing * .5f);

        float scaleY = !grounded
            ? 1f + jumpStretch * (velocity.y > .15f ? 1f : .5f)
            : 1f;
        float scaleX = !grounded ? 1f - jumpStretch * .55f : 1f;

        if (grounded && landingRemaining > 0f)
        {
            float amount =
                landingRemaining / Mathf.Max(.05f, landingDuration);
            scaleY = 1f - landingSquash * amount;
            scaleX = 1f + landingSquash * .65f * amount;
        }

        landingRemaining =
            Mathf.Max(0f, landingRemaining - deltaTime);

        Vector3 target = Vector3.Scale(
            restScale,
            new Vector3(scaleX, scaleY, 1f)
        );

        visual.localScale = Vector3.Lerp(
            visual.localScale,
            target,
            1f - Mathf.Exp(-24f * deltaTime)
        );

        float currentY = visual.localScale.y / restScale.y;
        float bob = grounded
            ? Mathf.Abs(Mathf.Sin(phase)) * .006f * moving
            : 0f;

        visual.localPosition =
            restPosition +
            Vector3.up * (
                (currentY - 1f) * .5f * restScale.y + bob
            );
    }

    private static void Rotate(
        Transform part,
        Quaternion rest,
        float degrees
    )
    {
        if (part != null)
        {
            part.localRotation =
                rest * Quaternion.Euler(0f, 0f, degrees);
        }
    }

    private void ResetPose()
    {
        if (!initialized || visual == null)
        {
            return;
        }

        visual.localScale = restScale;
        visual.localPosition = restPosition;
        Rotate(leftLeg, leftLegRest, 0f);
        Rotate(rightLeg, rightLegRest, 0f);
        Rotate(leftArm, leftArmRest, 0f);
        Rotate(rightArm, rightArmRest, 0f);
        phase = 0f;
        landingRemaining = 0f;
        observedGround = false;
    }

    private void OnDisable()
    {
        ResetPose();
    }
}
