using UnityEngine;
// Fills the whole screen without black bars and always shows at least the level's 16:9 view;
// narrower screens zoom out so the full width stays visible, wider screens show extra background.
[ExecuteAlways,RequireComponent(typeof(Camera))]
public sealed class FinalFixedCamera : MonoBehaviour
{
    [Min(1)] public float orthographicSize = 9f;
    void OnEnable(){Fit();}
    void Update(){Fit();}
    void Fit()
    {
        var camera=GetComponent<Camera>(); float actual=(float)Screen.width/Mathf.Max(1,Screen.height), target=16f/9f;
        camera.orthographic=true;camera.rect=new Rect(0,0,1,1);
        camera.orthographicSize=orthographicSize*Mathf.Max(1f,target/actual);
    }
}
