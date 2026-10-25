using UnityEngine;

public class DoorCameraVision : MonoBehaviour
{
    public Transform player;
    public Collider2D hideBox;
    public SpriteRenderer cameraEye;

    public float viewDistance = 10f;

    public bool CanSeePlayer { get; private set; }

    private Collider2D playerCollider;

    private void Start()
    {
        if (player != null)
            playerCollider = player.GetComponentInChildren<Collider2D>();
    }

    private void Update()
    {
        CanSeePlayer = CheckVision();

        if (cameraEye != null)
        {
            cameraEye.color = CanSeePlayer
                ? Color.red
                : Color.yellow;
        }
    }

    private bool CheckVision()
    {
        if (player == null)
            return false;

        Vector2 origin = transform.position;

        Vector2 target = playerCollider != null
            ? (Vector2)playerCollider.bounds.center
            : (Vector2)player.position;

        // 摄像头朝左，只能看到左侧的玩家。
        if (target.x >= origin.x)
            return false;

        if (Vector2.Distance(origin, target) > viewDistance)
            return false;

        // 检查摄像头到玩家之间是否有遮挡箱。
        RaycastHit2D[] hits =
            Physics2D.LinecastAll(origin, target);

        foreach (RaycastHit2D hit in hits)
        {
            if (hideBox != null && hit.collider == hideBox)
                return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Gizmos.color = CanSeePlayer
            ? Color.red
            : Color.yellow;

        Vector3 target = playerCollider != null
            ? playerCollider.bounds.center
            : player.position;

        Gizmos.DrawLine(transform.position, target);
    }
}