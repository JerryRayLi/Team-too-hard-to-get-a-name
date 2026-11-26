using UnityEngine;
using UnityEngine.UI;

// Crisp UI geometry at any Canvas scale; no world objects or collisions.
[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class JerryHelpMarkerGraphic : MaskableGraphic
{
    public JerryHelpMarkerGraphic() { useLegacyMeshGeneration = false; }
    public Vector2 labelCenter = new Vector2(0,82);
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        const int count=36; const float radius=24f, stroke=3f;
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
            Quad(vh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius-stroke),
                new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,
                new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(radius-stroke));
        }
        Vector2 direction=labelCenter.normalized;
        float edge=Mathf.Min(128f/Mathf.Max(.001f,Mathf.Abs(direction.x)),18f/Mathf.Max(.001f,Mathf.Abs(direction.y)));
        Vector2 start=labelCenter-direction*(edge+4),tip=direction*28;
        Vector2 normal=new Vector2(-direction.y,direction.x);
        Quad(vh,start-normal*1.7f,start+normal*1.7f,tip+direction*8+normal*1.7f,tip+direction*8-normal*1.7f);
        int v=vh.currentVertCount;
        vh.AddVert(tip+direction*10-normal*7,color,Vector2.zero);vh.AddVert(tip+direction*10+normal*7,color,Vector2.zero);vh.AddVert(tip,color,Vector2.zero);vh.AddTriangle(v,v+1,v+2);
    }
    private void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
    {
        int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddVert(d,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
    }
}
