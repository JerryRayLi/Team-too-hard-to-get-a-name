using UnityEngine;
public sealed class FinalGoal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D c){Hit(c);}
    void OnTriggerStay2D(Collider2D c){Hit(c);}
    void Hit(Collider2D c){if(c.GetComponent<FinalLevelPlayerController>())FinalLevelManager.Instance.CompleteLevel();}
}

