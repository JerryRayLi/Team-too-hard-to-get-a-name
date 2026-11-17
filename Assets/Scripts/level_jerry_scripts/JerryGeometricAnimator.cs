using UnityEngine;

// All deformation stays below Visual; the root collider and rigidbody never scale.
public sealed class JerryGeometricAnimator : MonoBehaviour
{
    public Transform visual;
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform leftArm;
    public Transform rightArm;
    [Range(0f, 15f)] public float legSwing = 7f;
    [Min(0f)] public float walkFrequency = 11f;
    [Range(0f, .2f)] public float jumpStretch = .055f;
    [Range(0f, .25f)] public float landingSquash = .10f;
    [Min(.05f)] public float landingDuration = .16f;

    private Rigidbody2D body;
    private Collider2D shape;
    private PlayerRespawnJ respawn;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[12];
    private ContactFilter2D groundFilter;
    private Vector3 restPosition;
    private Vector3 restScale;
    private Quaternion leftLegRest, rightLegRest, leftArmRest, rightArmRest;
    private bool initialized, observedGround, wasGrounded;
    private float phase, landingRemaining;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized || visual == null)
            return;
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<Collider2D>();
        respawn = GetComponent<PlayerRespawnJ>();
        restPosition = visual.localPosition;
        restScale = visual.localScale;
        leftLegRest = leftLeg != null ? leftLeg.localRotation : Quaternion.identity;
        rightLegRest = rightLeg != null ? rightLeg.localRotation : Quaternion.identity;
        leftArmRest = leftArm != null ? leftArm.localRotation : Quaternion.identity;
        rightArmRest = rightArm != null ? rightArm.localRotation : Quaternion.identity;
        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        groundFilter.useTriggers = false;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f)
            return;
        Initialize();
        if (!initialized || body == null)
            return;
        if (respawn != null && respawn.IsRespawning)
        {
            ResetPose();
            return;
        }
        bool grounded = false;
        if (shape != null && shape.enabled && body.linearVelocity.y <= .15f)
        {
            int count = shape.Cast(Vector2.down, groundFilter, groundHits, .045f);
            for (int i = 0; i < count; i++)
                if (groundHits[i].collider != null && groundHits[i].normal.y > .55f)
                {
                    grounded = true;
                    break;
                }
        }
        AnimatePose(body.linearVelocity, grounded, Time.deltaTime);
    }

    private void AnimatePose(Vector2 velocity, bool grounded, float deltaTime)
    {
        Initialize();
        if (!initialized)
            return;
        if (observedGround && grounded && !wasGrounded)
            landingRemaining = landingDuration;
        observedGround = true;
        wasGrounded = grounded;
        float moving = grounded ? Mathf.Clamp01(Mathf.Abs(velocity.x) / 6f) : 0f;
        phase += deltaTime * walkFrequency * Mathf.Lerp(.6f, 1f, moving);
        float swing = Mathf.Sin(phase) * legSwing * moving;
        Rotate(leftLeg, leftLegRest, swing);
        Rotate(rightLeg, rightLegRest, -swing);
        Rotate(leftArm, leftArmRest, -swing * .5f);
        Rotate(rightArm, rightArmRest, swing * .5f);

        float scaleY = !grounded ? 1f + jumpStretch * (velocity.y > .15f ? 1f : .5f) : 1f;
        float scaleX = !grounded ? 1f - jumpStretch * .55f : 1f;
        if (grounded && landingRemaining > 0f)
        {
            float amount = landingRemaining / Mathf.Max(.05f, landingDuration);
            scaleY = 1f - landingSquash * amount;
            scaleX = 1f + landingSquash * .65f * amount;
        }
        landingRemaining = Mathf.Max(0f, landingRemaining - deltaTime);
        Vector3 target = Vector3.Scale(restScale, new Vector3(scaleX, scaleY, 1f));
        visual.localScale = Vector3.Lerp(visual.localScale, target, 1f - Mathf.Exp(-24f * deltaTime));
        // Keep deformation anchored to the feet at local y=-0.5.
        float currentY = visual.localScale.y / restScale.y;
        float bob = grounded ? Mathf.Abs(Mathf.Sin(phase)) * .006f * moving : 0f;
        visual.localPosition = restPosition + Vector3.up * ((currentY - 1f) * .5f * restScale.y + bob);
    }

    private static void Rotate(Transform part, Quaternion rest, float degrees)
    {
        if (part != null)
            part.localRotation = rest * Quaternion.Euler(0f, 0f, degrees);
    }

    private void ResetPose()
    {
        if (!initialized || visual == null)
            return;
        visual.localScale = restScale;
        visual.localPosition = restPosition;
        Rotate(leftLeg, leftLegRest, 0f);
        Rotate(rightLeg, rightLegRest, 0f);
        Rotate(leftArm, leftArmRest, 0f);
        Rotate(rightArm, rightArmRest, 0f);
        phase = landingRemaining = 0f;
        observedGround = false;
    }

    private void OnDisable() => ResetPose();
}
