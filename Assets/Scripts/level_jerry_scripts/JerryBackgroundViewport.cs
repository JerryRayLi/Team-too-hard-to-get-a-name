using UnityEngine;

// Only decorative layers follow the view. Serialized source positions prevent
// accumulated scaling when editing, resizing, reloading or entering Play mode.
[ExecuteAlways, DefaultExecutionOrder(1400)]
public sealed class JerryBackgroundViewport : MonoBehaviour
{
    public Vector2 referenceCenter;
    public Vector2 referenceSize = new Vector2(28.44445f, 16f);
    public SpriteRenderer steelWall;
    public Transform stateRails;
    public Transform ambientLighting;
    [HideInInspector] public Transform[] railParts = new Transform[0];
    [HideInInspector] public Vector3[] railPositions = new Vector3[0];
    [HideInInspector] public Vector3[] railScales = new Vector3[0];
    [HideInInspector] public float[] railAnchors = new float[0];
    [HideInInspector] public bool[] continuousRails = new bool[0];
    private Camera view;
    private MaterialPropertyBlock wallProperties;
    private static readonly int WallOrigin = Shader.PropertyToID("_WallOrigin");

    private void OnEnable() => RefreshLayout();
    private void LateUpdate() => RefreshLayout();

    public void RefreshLayout()
    {
        // Do not adapt prefab assets or a prefab isolation stage to another scene.
        if (!gameObject.scene.IsValid() || !gameObject.scene.path.EndsWith(".unity") ||
            steelWall == null || stateRails == null || ambientLighting == null ||
            railParts.Length != railPositions.Length || railParts.Length != railScales.Length ||
            railParts.Length != railAnchors.Length || railParts.Length != continuousRails.Length)
            return;
        if (view == null || !view.isActiveAndEnabled || view.gameObject.scene != gameObject.scene)
            view = Camera.main;
        if (view == null || !view.orthographic || view.gameObject.scene != gameObject.scene)
            return;

        Vector3 low = transform.InverseTransformPoint(view.ViewportToWorldPoint(new Vector3(0, 0, 0)));
        Vector3 high = transform.InverseTransformPoint(view.ViewportToWorldPoint(new Vector3(1, 1, 0)));
        Vector2 center = (low + high) * .5f;
        Vector2 size = new Vector2(Mathf.Abs(high.x - low.x), Mathf.Abs(high.y - low.y));
        if (size.x <= 0 || size.y <= 0 || referenceSize.x <= 0 || referenceSize.y <= 0) return;

        // The quad extends; world-space texture sampling keeps every plate and
        // scratch at a fixed size, including while resizing the Game view.
        var wall = steelWall.transform;
        wall.localPosition = new Vector3(center.x, center.y, 0);
        wall.localScale = new Vector3(Mathf.Max(128f, size.x + 2) / steelWall.sprite.bounds.size.x,
            Mathf.Max(72f, size.y + 2) / steelWall.sprite.bounds.size.y, 1);
        if (wallProperties == null) wallProperties = new MaterialPropertyBlock();
        steelWall.GetPropertyBlock(wallProperties);
        Vector3 origin = transform.TransformPoint(referenceCenter);
        wallProperties.SetVector(WallOrigin, new Vector4(origin.x, origin.y, 0, 0));
        steelWall.SetPropertyBlock(wallProperties);

        stateRails.localPosition = Vector3.zero;
        stateRails.localScale = Vector3.one;
        float ratioX = (size.x + .2f) / Mathf.Max(.1f, referenceSize.x - .6f);
        float shiftY = (size.y - referenceSize.y) * .5f;
        for (int i = 0; i < railParts.Length; i++)
        {
            if (railParts[i] == null) continue;
            Vector3 p = railPositions[i], scale = railScales[i];
            p.x += center.x - referenceCenter.x + (railAnchors[i] - referenceCenter.x) * (ratioX - 1);
            p.y += center.y - referenceCenter.y + Mathf.Sign(p.y - referenceCenter.y) * shiftY;
            if (continuousRails[i]) scale.x *= ratioX;
            railParts[i].localPosition = p;
            railParts[i].localScale = scale;
        }
        // Only the procedural gradient changes extent, never the lamp cartridges.
        Vector2 wash = new Vector2(size.x / referenceSize.x * 1.25f, size.y / referenceSize.y);
        ambientLighting.localScale = new Vector3(wash.x, wash.y, 1);
        ambientLighting.localPosition = new Vector3(center.x - referenceCenter.x * wash.x,
            center.y - referenceCenter.y * wash.y, 0);
    }
}
