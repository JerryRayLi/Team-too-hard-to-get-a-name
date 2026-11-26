using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CorpseReleaseAbility))]
public class CorpseReleaseRangeIndicator : MonoBehaviour
{
    public Material ringMaterial;
    [Min(8)] public int dashCount = 48;
    [Range(0.1f, 0.9f)] public float dashFraction = 0.55f;
    [Min(0.01f)] public float lineWidth = 0.04f;
    [Tooltip("Set to 0 to keep the hint visible for the entire level.")]
    [Min(0f)] public float hintDuration = 4f;
    [Min(0f)] public float hintOffset = 0.4f;
    public string hintText = "Release ALL Corpses (E)";
    [Header("Release feedback")]
    [Min(.05f)] public float releasePulseDuration = .28f;
    public Color releasePulseColor = new Color(1f,.91f,.62f,.96f);

    private CorpseReleaseAbility ability;
    private PlayerRespawnJ respawn;
    private GameObject ringObject;
    private MeshRenderer ringRenderer;
    private Mesh ringMesh;
    private float meshRadius = -1f;
    private float hintStartTime;
    private GUIStyle hintStyle;
    private float pulseRemaining, pulseStrength, meshWidth = -1f;
    private MaterialPropertyBlock ringProperties;
    private Color restRingColor;
    private Vector3[] ringVertices;
    private int[] ringTriangles;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private bool IsVisible => ability != null && ability.isActiveAndEnabled &&
        (respawn == null || !respawn.IsRespawning) && ability.releaseRange > 0f;

    private void Awake()
    {
        ability = GetComponent<CorpseReleaseAbility>();
        respawn = GetComponent<PlayerRespawnJ>();
        if (ringMaterial == null)
        {
            Debug.LogWarning("Assign a material to the corpse release range indicator.", this);
            enabled = false;
            return;
        }

        // Keep world-space dimensions independent of the player's nonuniform
        // scale, matching the Vector2.Distance check used by the ability.
        ringObject = new GameObject("Corpse Release Range");
        ringObject.layer = gameObject.layer;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ringObject, gameObject.scene);
        ringMesh = new Mesh { name = "Dashed Corpse Release Range" };
        ringObject.AddComponent<MeshFilter>().sharedMesh = ringMesh;
        ringRenderer = ringObject.AddComponent<MeshRenderer>();
        ringRenderer.sharedMaterial = ringMaterial;
        ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
        ringRenderer.receiveShadows = false;
        ringRenderer.sortingOrder = 20;
        ringRenderer.enabled = false;
        ringProperties = new MaterialPropertyBlock();
        restRingColor = ringMaterial.HasProperty(BaseColorId) ? ringMaterial.GetColor(BaseColorId) : ringMaterial.GetColor(ColorId);
    }

    private void Start()
    {
        hintStartTime = Time.unscaledTime;
    }

    public void PulseRelease(int releasedCount)
    {
        pulseRemaining = Mathf.Max(.05f,releasePulseDuration);
        pulseStrength = releasedCount > 0 ? 1f : .35f;
        RefreshVisuals(0f);
    }

    private void LateUpdate() => RefreshVisuals(Time.deltaTime);
    private void RefreshVisuals(float seconds)
    {
        pulseRemaining = Mathf.Max(0f,pulseRemaining-Mathf.Max(0f,seconds));
        if (ringRenderer == null)
            return;

        ringRenderer.enabled = IsVisible;
        if (!IsVisible)
            return;

        float pulse = Mathf.SmoothStep(0,1,pulseRemaining/Mathf.Max(.05f,releasePulseDuration))*pulseStrength;
        Color tint = Color.Lerp(restRingColor,releasePulseColor,pulse);
        ringProperties.SetColor(BaseColorId,tint);
        ringProperties.SetColor(ColorId,tint);
        ringRenderer.SetPropertyBlock(ringProperties);
        float width = lineWidth*Mathf.Lerp(1f,2f,pulse);
        if (!Mathf.Approximately(meshRadius, ability.releaseRange) || !Mathf.Approximately(meshWidth,width))
            RebuildRing(ability.releaseRange,width);

        ringObject.transform.position = transform.position;
    }

    private void RebuildRing(float radius,float width)
    {
        const int segmentsPerDash = 4;
        int count = Mathf.Clamp(dashCount, 8, 256);
        int segmentCount = count * segmentsPerDash;
        bool resize = ringVertices == null || ringVertices.Length != segmentCount*4;
        if (resize) { ringVertices = new Vector3[segmentCount*4]; ringTriangles = new int[segmentCount*6]; }
        Vector3[] vertices = ringVertices;
        int[] triangles = ringTriangles;
        float innerRadius = Mathf.Max(0f, radius - Mathf.Max(0.01f, width));
        float dashAngle = Mathf.PI * 2f / count;

        for (int dash = 0; dash < count; dash++)
        {
            for (int segment = 0; segment < segmentsPerDash; segment++)
            {
                int index = dash * segmentsPerDash + segment;
                int vertex = index * 4;
                int triangle = index * 6;
                float start = dashAngle * (dash + segment * dashFraction / segmentsPerDash);
                float end = dashAngle * (dash + (segment + 1) * dashFraction / segmentsPerDash);
                Vector3 from = new Vector3(Mathf.Cos(start), Mathf.Sin(start), 0f);
                Vector3 to = new Vector3(Mathf.Cos(end), Mathf.Sin(end), 0f);
                vertices[vertex] = from * innerRadius;
                vertices[vertex + 1] = from * radius;
                vertices[vertex + 2] = to * radius;
                vertices[vertex + 3] = to * innerRadius;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 2;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }
        }

        if (resize) ringMesh.Clear();
        ringMesh.vertices = vertices;
        if (resize) ringMesh.triangles = triangles;
        ringMesh.RecalculateBounds();
        meshRadius = radius;
        meshWidth = width;
    }

    private void OnGUI()
    {
        bool permanent = hintDuration <= 0f;
        float remaining = permanent
            ? float.PositiveInfinity
            : hintDuration - (Time.unscaledTime - hintStartTime);

        if (!IsVisible || (!permanent && remaining <= 0f) || Camera.main == null)
            return;

        Vector3 anchor = Camera.main.WorldToScreenPoint(
            transform.position + Vector3.up * (ability.releaseRange + hintOffset));
        if (anchor.z <= 0f)
            return;

        if (hintStyle == null)
            hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.fontStyle = FontStyle.Bold;
        hintStyle.fontSize = GameHUD.ScaleFont(22);
        hintStyle.normal.textColor = Color.white;

        GUIContent content = new GUIContent(hintText);
        Vector2 textSize = hintStyle.CalcSize(content);
        float labelWidth = Mathf.Max(280f, textSize.x + 32f);
        float labelHeight = Mathf.Max(40f, textSize.y + 12f);
        Rect labelRect = new Rect(
            anchor.x - labelWidth * 0.5f,
            Screen.height - anchor.y - labelHeight * 0.5f,
            labelWidth,
            labelHeight
        );
        Color previousColor = GUI.color;
        float opacity = permanent ? 1f : Mathf.Clamp01(remaining);
        GUI.color = new Color(0f, 0f, 0f, opacity * 0.8f);
        GUI.Label(new Rect(labelRect.x + 1f, labelRect.y + 1f, labelRect.width, labelRect.height),
            content, hintStyle);
        GUI.color = new Color(1f, 1f, 1f, opacity);
        GUI.Label(labelRect, content, hintStyle);
        GUI.color = previousColor;
    }

    private void OnDisable()
    {
        pulseRemaining = 0f;
        if (ringRenderer != null)
            ringRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (ringObject != null)
            Destroy(ringObject);
        if (ringMesh != null)
            Destroy(ringMesh);
    }
}
