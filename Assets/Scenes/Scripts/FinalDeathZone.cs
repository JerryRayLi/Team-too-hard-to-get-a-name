using UnityEngine;
public sealed class FinalDeathZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D c){Hit(c);}
    void OnTriggerStay2D(Collider2D c){Hit(c);}
    void Hit(Collider2D c){if(c.GetComponent<FinalLevelPlayerController>())FinalLevelManager.Instance.Die("fall");}
}

