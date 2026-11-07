using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public sealed class JerryCrumblingFloor : MonoBehaviour
{
    [Min(0.1f)] public float collapseDelay = 4f;
    [Min(0.02f)] public float flashInterval = 0.15f;
    [Min(0f)] public float contactTolerance = 0.03f;
    public Button receiver;
    public JerryCheckpoint requiredCheckpoint;
    [Tooltip("After collapse, allow a short grace period to reach the level exit.")]
    public bool requiresLevelExit;
    public JerryLevelProgress progress;

    private readonly List<Collider2D> nearby = new List<Collider2D>();
    private readonly HashSet<Corpse> supportedCorpses = new HashSet<Corpse>();
    private Collider2D floorCollider;
    private Collider2D receiverCollider;
    private SpriteRenderer floorRenderer;
    private SpriteRenderer receiverRenderer;
    private ContactFilter2D filter;
    private Color normalColor;
    private bool triggered;
    private bool collapsed;
    private float elapsed;

    private void Awake()
    {
        floorCollider = GetComponent<Collider2D>();
        floorRenderer = GetComponent<SpriteRenderer>();
        normalColor = floorRenderer.color;
        if (receiver != null)
        {
            receiverCollider = receiver.GetComponent<Collider2D>();
            receiverRenderer = receiver.GetComponent<SpriteRenderer>();
        }
        filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
    }

    private void FixedUpdate()
    {
        if (collapsed)
            return;
        supportedCorpses.Clear();
        // The thin receiver covers the basin floor. Contact with either surface
        // loads the same structure, including static corpses without callbacks.
        bool touching = InspectSurface(floorCollider);
        touching |= InspectSurface(receiverCollider);
        if (touching)
            triggered = true;
        if (!triggered)
            return;

        elapsed += Time.fixedDeltaTime;
        float interval = Mathf.Lerp(flashInterval, flashInterval * 0.4f,
            Mathf.Clamp01(elapsed / collapseDelay));
        floorRenderer.color = ((int)(elapsed / interval) % 2 == 0)
            ? normalColor : new Color(1f, 0.48f, 0.25f, 0.35f);
        if (elapsed >= collapseDelay)
            Collapse();
    }

    private bool InspectSurface(Collider2D surface)
    {
        if (surface == null || !surface.enabled)
            return false;
        Bounds bounds = surface.bounds;
        Physics2D.OverlapBox(bounds.center,
            (Vector2)bounds.size + Vector2.one * (contactTolerance * 2f), 0f, filter, nearby);
        bool touching = false;
        foreach (Collider2D other in nearby)
        {
            if (other == floorCollider || other == receiverCollider)
                continue;
            ColliderDistance2D distance = surface.Distance(other);
            if (!distance.isValid || distance.distance > contactTolerance)
                continue;
            Corpse corpse = other.GetComponentInParent<Corpse>();
            if (corpse != null)
            {
                supportedCorpses.Add(corpse);
                touching = true;
            }
            else
            {
                PlayerRespawnJ player = other.GetComponentInParent<PlayerRespawnJ>();
                touching |= player != null && !player.IsRespawning;
            }
        }
        return touching;
    }

    private void Collapse()
    {
        collapsed = true;
        floorCollider.enabled = false;
        floorRenderer.enabled = false;
        if (receiverCollider != null)
            receiverCollider.enabled = false;
        if (receiverRenderer != null)
            receiverRenderer.enabled = false;
        // Releasing static corpses is necessary for gravity to take over.
        foreach (Corpse corpse in supportedCorpses)
            if (corpse != null)
                corpse.Release();
        if (progress != null)
            progress.NotifyCollapse(requiredCheckpoint, requiresLevelExit);
    }
}
