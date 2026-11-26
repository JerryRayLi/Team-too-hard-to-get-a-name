using UnityEngine;
using UnityEngine.Rendering;

// Corner brackets only: no sprite tint, collision, or rigidbody changes.
public sealed class JerryCorpseSelection : MonoBehaviour
{
    public Color outlineColor = new Color(1f,.91f,.62f,.85f);
    [Min(.005f)] public float lineWidth = .025f;
    [Min(0f)] public float padding = .08f;
    public Corpse Source { get; private set; }
    public bool IsSelected { get; private set; }
    private SpriteRenderer[] sprites;
    private MeshRenderer outline;
    private GameObject visual;
    private Mesh mesh;
    private MaterialPropertyBlock properties;
    private readonly Vector3[] vertices = new Vector3[32];
    private readonly int[] indices = new int[48];
    private Bounds lastBounds;
    private bool hasBounds;

    public void SetSelected(bool selected)
    {
        if (Source == null) Source = GetComponent<Corpse>();
        IsSelected = selected;
        if (!selected) { if (outline != null) outline.enabled = false; return; }
        if (outline == null) Initialize();
        RefreshOutline();
    }
    private void Initialize()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        visual = new GameObject("Corpse release selection");
        visual.transform.SetParent(transform,false);
        mesh = new Mesh { name = "Corpse selection corners" };
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        outline = visual.AddComponent<MeshRenderer>();
        outline.sharedMaterial = Resources.Load<Material>("CorpseReleaseRange");
        outline.shadowCastingMode = ShadowCastingMode.Off;
        outline.receiveShadows = false;
        int order = 0;
        foreach(var r in sprites) if(r.enabled) { order=Mathf.Max(order,r.sortingOrder);outline.sortingLayerID=r.sortingLayerID; }
        outline.sortingOrder = Mathf.Max(30,order+4);
        properties = new MaterialPropertyBlock();
        for(int i=0;i<8;i++){int v=i*4,t=i*6;indices[t]=v;indices[t+1]=v+1;indices[t+2]=v+2;indices[t+3]=v;indices[t+4]=v+2;indices[t+5]=v+3;}
    }
    private void LateUpdate()
    {
        if (!IsSelected) return;
        var body=Source!=null?Source.GetComponent<Rigidbody2D>():null;
        if(Source==null||!Source.isActiveAndEnabled||body==null||body.bodyType!=RigidbodyType2D.Static)
        {SetSelected(false);return;}
        RefreshOutline();
    }
    private void RefreshOutline()
    {
        if (outline == null) return;
        bool found=false;Bounds bounds=default;
        foreach(var r in sprites)
            if(r!=null&&r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff)
            {if(!found){bounds=r.bounds;found=true;}else bounds.Encapsulate(r.bounds);}
        outline.enabled=found&&IsSelected;
        if(!found)return;
        properties.SetColor("_BaseColor",outlineColor);properties.SetColor("_Color",outlineColor);outline.SetPropertyBlock(properties);
        if(hasBounds&&bounds==lastBounds)return;
        lastBounds=bounds;hasBounds=true;
        float left=bounds.min.x-padding,right=bounds.max.x+padding,bottom=bounds.min.y-padding,top=bounds.max.y+padding;
        float length=Mathf.Clamp(Mathf.Min(right-left,top-bottom)*.28f,.12f,.26f),w=lineWidth,z=bounds.center.z-.01f;
        int index=0;
        Quad(ref index,left,bottom,length,w,z);Quad(ref index,left,bottom,w,length,z);
        Quad(ref index,right-length,bottom,length,w,z);Quad(ref index,right-w,bottom,w,length,z);
        Quad(ref index,left,top-w,length,w,z);Quad(ref index,left,top-length,w,length,z);
        Quad(ref index,right-length,top-w,length,w,z);Quad(ref index,right-w,top-length,w,length,z);
        mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateBounds();
    }
    private void Quad(ref int n,float x,float y,float width,float height,float z)
    {
        vertices[n++]=visual.transform.InverseTransformPoint(new Vector3(x,y,z));
        vertices[n++]=visual.transform.InverseTransformPoint(new Vector3(x+width,y,z));
        vertices[n++]=visual.transform.InverseTransformPoint(new Vector3(x+width,y+height,z));
        vertices[n++]=visual.transform.InverseTransformPoint(new Vector3(x,y+height,z));
    }
    private void OnDisable(){IsSelected=false;if(outline!=null)outline.enabled=false;}
    private void OnDestroy()
    {
        if(Application.isPlaying){if(visual!=null)Destroy(visual);if(mesh!=null)Destroy(mesh);}
        else{if(visual!=null)DestroyImmediate(visual);if(mesh!=null)DestroyImmediate(mesh);}
    }
}
