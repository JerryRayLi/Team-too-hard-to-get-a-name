using UnityEngine;

public class Corpse : MonoBehaviour
{
    [Range(0f, 1f)] public float surfaceFriction = 0.9f;

    private Rigidbody2D rb;
    private PhysicsMaterial2D gripMaterial;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        // The player uses a zero-friction material. Give corpses their own
        // grip without changing the shared material or the player's movement.
        gripMaterial = new PhysicsMaterial2D("Corpse Grip")
        {
            friction = surfaceFriction,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine2D.Maximum,
            bounceCombine = PhysicsMaterialCombine2D.Minimum
        };
        foreach (Collider2D corpseCollider in GetComponentsInChildren<Collider2D>(true))
            corpseCollider.sharedMaterial = gripMaterial;
    }

    public void Release()
    {
        if (rb.bodyType == RigidbodyType2D.Dynamic)
            return;

        // Preserve the horizontal spawn pose instead of tipping on impact.
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.WakeUp();
    }

    private void OnDestroy()
    {
        if (gripMaterial != null)
            Destroy(gripMaterial);
    }
}
