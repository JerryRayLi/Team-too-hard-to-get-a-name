using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CorpseReleaseAbility))]
public class CorpseReleaseRangeIndicator : MonoBehaviour
{
    public Material ringMaterial;
    [Min(8)] public int dashCount = 48;
    [Range(0.1f, 0.9f)] public float dashFraction = 0.55f;
    [Min(0.01f)] public float lineWidth = 0.04f;
    [Min(0f)] public float hintDuration = 4f;
    [Min(0f)] public float hintOffset = 0.4f;
    public string hintText = "Releasing Range";

    private CorpseReleaseAbility ability;
    private PlayerRespawnJ respawn;
    private GameObject ringObject;
    private MeshRenderer ringRenderer;
    private Mesh ringMesh;
    private float meshRadius = -1f;
    private float hintStartTime;
    private GUIStyle hintStyle;

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
    }

    private void Start()
    {
        hintStartTime = Time.unscaledTime;
    }

    private void LateUpdate()
    {
        if (ringRenderer == null)
            return;

        ringRenderer.enabled = IsVisible;
        if (!IsVisible)
            return;

        if (!Mathf.Approximately(meshRadius, ability.releaseRange))
            RebuildRing(ability.releaseRange);

        ringObject.transform.position = transform.position;
    }

    private void RebuildRing(float radius)
    {
        const int segmentsPerDash = 4;
        int count = Mathf.Clamp(dashCount, 8, 256);
        int segmentCount = count * segmentsPerDash;
        Vector3[] vertices = new Vector3[segmentCount * 4];
        int[] triangles = new int[segmentCount * 6];
        float innerRadius = Mathf.Max(0f, radius - Mathf.Max(0.01f, lineWidth));
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

        ringMesh.Clear();
        ringMesh.vertices = vertices;
        ringMesh.triangles = triangles;
        ringMesh.RecalculateBounds();
        meshRadius = radius;
    }

    private void OnGUI()
    {
        float remaining = hintDuration - (Time.unscaledTime - hintStartTime);
        if (!IsVisible || remaining <= 0f || Camera.main == null)
            return;

        Vector3 anchor = Camera.main.WorldToScreenPoint(
            transform.position + Vector3.up * (ability.releaseRange + hintOffset));
        if (anchor.z <= 0f)
            return;

        if (hintStyle == null)
            hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.fontStyle = FontStyle.Bold;
        hintStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 45f), 16, 24);
        hintStyle.normal.textColor = Color.white;

        Rect labelRect = new Rect(anchor.x - 140f, Screen.height - anchor.y - 16f, 280f, 32f);
        Color previousColor = GUI.color;
        float opacity = Mathf.Clamp01(remaining);
        GUI.color = new Color(0f, 0f, 0f, opacity * 0.8f);
        GUI.Label(new Rect(labelRect.x + 1f, labelRect.y + 1f, labelRect.width, labelRect.height),
            hintText, hintStyle);
        GUI.color = new Color(1f, 1f, 1f, opacity);
        GUI.Label(labelRect, hintText, hintStyle);
        GUI.color = previousColor;
    }

    private void OnDisable()
    {
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
