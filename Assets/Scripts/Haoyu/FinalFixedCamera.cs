using UnityEngine;
[ExecuteAlways,RequireComponent(typeof(Camera))]
public sealed class FinalFixedCamera : MonoBehaviour
{
    void OnEnable(){Fit();}
    void Update(){Fit();}
    void OnGUI()
    {
        var r=GetComponent<Camera>().rect; var before=GUI.color; GUI.color=Color.black; GUI.depth=1000;
        if(r.width<1){float w=(1-r.width)*Screen.width/2;GUI.DrawTexture(new Rect(0,0,w,Screen.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(Screen.width-w,0,w,Screen.height),Texture2D.whiteTexture);}
        if(r.height<1){float h=(1-r.height)*Screen.height/2;GUI.DrawTexture(new Rect(0,0,Screen.width,h),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,Screen.height-h,Screen.width,h),Texture2D.whiteTexture);}
        GUI.color=before;
    }
    void Fit()
    {
        var camera=GetComponent<Camera>(); float actual=(float)Screen.width/Mathf.Max(1,Screen.height), target=16f/9f;
        camera.orthographic=true;camera.orthographicSize=9;
        camera.rect=actual>=target ? new Rect((1-target/actual)/2,0,target/actual,1) : new Rect(0,(1-actual/target)/2,1,actual/target);
    }
}

