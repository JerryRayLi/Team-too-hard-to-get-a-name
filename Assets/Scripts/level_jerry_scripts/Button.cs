using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour
{
    public Door door;
    public bool useColorRules;
    public ColorLayerManager colorManager;
    public Color redColor = new Color(138f / 255f, 80f / 255f, 74f / 255f);
    public Color blueColor = new Color(63f / 255f, 101f / 255f, 112f / 255f);

    [Min(0f)] public float contactTolerance = 0.02f;

    private readonly List<Collider2D> nearbyColliders = new List<Collider2D>();
    private Collider2D buttonCollider;
    private SpriteRenderer buttonRenderer;
    private ContactFilter2D contactFilter;
    private bool isPressed;

    private void Awake()
    {
        buttonCollider = GetComponent<Collider2D>();
        buttonRenderer = GetComponent<SpriteRenderer>();
        if (useColorRules && colorManager == null)
            colorManager = FindFirstObjectByType<ColorLayerManager>();

        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        contactFilter.useTriggers = false;
    }

    private void LateUpdate()
    {
        // Read after ColorLayerManager.Update so Q also re-evaluates objects
        // already resting on the button, without requiring another collision.
        bool isBlue = colorManager != null && colorManager.IsBlueActive;
        if (useColorRules && buttonRenderer != null)
            buttonRenderer.color = isBlue ? blueColor : redColor;

        SetPressed(HasValidContact(isBlue));
    }

    private bool HasValidContact(bool isBlue)
    {
        if (buttonCollider == null || !buttonCollider.enabled)
            return false;
        if (useColorRules && colorManager == null)
            return false;

        Bounds buttonBounds = buttonCollider.bounds;
        // Static corpses do not generate collision callbacks against a static
        // button. Query touching shapes directly, including resting corpses.
        Physics2D.OverlapBox(buttonBounds.center,
            (Vector2)buttonBounds.size + Vector2.one * (contactTolerance * 2f),
            0f, contactFilter, nearbyColliders);

        foreach (Collider2D other in nearbyColliders)
        {
            if (other == buttonCollider)
                continue;

            ColliderDistance2D separation = buttonCollider.Distance(other);
            if (!separation.isValid || separation.distance > contactTolerance)
                continue;

            if (!useColorRules || isBlue)
            {
                if (other.GetComponentInParent<Corpse>() != null)
                    return true;
            }
            else
            {
                PlayerRespawnJ player = other.GetComponentInParent<PlayerRespawnJ>();
                if (player != null && !player.IsRespawning &&
                    other.bounds.center.y > buttonBounds.max.y &&
                    other.bounds.min.y >= buttonBounds.max.y - contactTolerance)
                    return true;
            }
        }

        return false;
    }

    private void SetPressed(bool pressed)
    {
        if (isPressed == pressed)
            return;

        isPressed = pressed;
        if (door == null)
            return;
        if (pressed)
            door.Open();
        else
            door.Close();
    }

    private void OnDisable()
    {
        SetPressed(false);
    }
}
