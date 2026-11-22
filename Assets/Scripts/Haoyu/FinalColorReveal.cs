using UnityEngine;
// Disguised trap tile: looks like plain ground on the red layer and shows the blue-layer colour
// while the blue layer is active, so switching colour reveals it.
[RequireComponent(typeof(SpriteRenderer))]
public sealed class FinalColorReveal : MonoBehaviour
{
    public Color revealColor = new Color(63f / 255f, 101f / 255f, 112f / 255f, 1f);
    SpriteRenderer source, overlay;
    ColorLayerManager colors;
    FinalWeightBrick brick;
    void Awake()
    {
        source = GetComponent<SpriteRenderer>();
        brick = GetComponent<FinalWeightBrick>();
        var child = new GameObject("ColorReveal");
        child.transform.SetParent(transform, false);
        overlay = child.AddComponent<SpriteRenderer>();
        overlay.sprite = source.sprite;
        overlay.sharedMaterial = source.sharedMaterial;
        overlay.sortingLayerID = source.sortingLayerID;
        overlay.sortingOrder = source.sortingOrder + 1;
        overlay.color = revealColor;
        overlay.enabled = false;
    }
    void Start() { colors = FindFirstObjectByType<ColorLayerManager>(); }
    // A collapsed tile stays hidden, and a triggered weight brick keeps its own warning colour.
    void LateUpdate() => overlay.enabled = colors && colors.IsBlueActive && source.enabled && !(brick && brick.Triggered);
}
