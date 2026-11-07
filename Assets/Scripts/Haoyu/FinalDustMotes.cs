using UnityEngine;
// Faint motes drifting slowly down: the only hint that this area makes things fall slowly.
public sealed class FinalDustMotes : MonoBehaviour
{
    public SpriteRenderer template;
    public Color color = new Color(1f, 1f, 1f, .35f);
    public Vector2 area = new Vector2(7f, 8f);
    [Min(1)] public int count = 18;
    public float fallSpeed = .35f, moteSize = .08f;
    Transform[] motes;
    float[] phases;
    void Start()
    {
        var random = new System.Random(526);
        motes = new Transform[count]; phases = new float[count];
        for (int i = 0; i < count; i++)
        {
            var mote = new GameObject("Mote");
            mote.transform.SetParent(transform, false);
            mote.transform.localPosition = new Vector3(((float)random.NextDouble() - .5f) * area.x, ((float)random.NextDouble() - .5f) * area.y, 0);
            mote.transform.localScale = Vector3.one * moteSize;
            var r = mote.AddComponent<SpriteRenderer>();
            if (template) { r.sprite = template.sprite; r.sharedMaterial = template.sharedMaterial; r.sortingLayerID = template.sortingLayerID; r.sortingOrder = template.sortingOrder - 1; }
            r.color = color;
            motes[i] = mote.transform; phases[i] = (float)random.NextDouble() * 6.28f;
        }
    }
    void Update()
    {
        for (int i = 0; i < motes.Length; i++)
        {
            var p = motes[i].localPosition;
            p.y -= fallSpeed * Time.deltaTime;
            p.x += Mathf.Sin(Time.time * .8f + phases[i]) * .08f * Time.deltaTime;
            if (p.y < -area.y / 2) p.y += area.y;
            motes[i].localPosition = p;
        }
    }
}
