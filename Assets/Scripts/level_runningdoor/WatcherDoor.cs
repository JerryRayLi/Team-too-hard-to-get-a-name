using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(LevelExit))]
public class WatcherDoor : MonoBehaviour
{
    public DoorCameraVision vision;
    public BoxCollider2D barrier;

    [Header("Movement")]
    public float fleeSpeed = 7.5f;

    [Tooltip("发现玩家后，等待多久才开始逃跑")]
    public float reactionTime = 0.5f;

    [Tooltip("失去视线后，继续逃跑多久")]
    public float blindRunTime = 1f;

    private BoxCollider2D exitTrigger;

    private bool sawPlayerLastFrame;
    private bool running;

    private float noticedAt;
    private float lastSeenAt;

    private void Start()
    {
        exitTrigger = GetComponent<BoxCollider2D>();
        exitTrigger.isTrigger = true;

        // 门始终可以进入，不再要求先撞墙。
        exitTrigger.enabled = true;
    }

    private void Update()
    {
        if (vision == null || Time.deltaTime <= 0f)
            return;

        bool seesPlayer = vision.CanSeePlayer;

        if (seesPlayer)
        {
            if (!sawPlayerLastFrame)
            {
                // 刚发现玩家：留出反应时间。
                noticedAt = Time.time;
                running = false;
            }

            lastSeenAt = Time.time;

            if (Time.time - noticedAt >= reactionTime)
                running = true;
        }
        else
        {
            if (Time.time - lastSeenAt >= blindRunTime)
                running = false;
        }

        sawPlayerLastFrame = seesPlayer;

        if (!running)
            return;

        MoveRight();
    }

    private void MoveRight()
    {
        float currentX = transform.position.x;
        float nextX = currentX + fleeSpeed * Time.deltaTime;

        if (IsWallSolid())
        {
            float scaleX = transform.lossyScale.x;

            float rightOffset =
                exitTrigger.offset.x * scaleX +
                exitTrigger.size.x * Mathf.Abs(scaleX) * 0.5f;

            float stopX =
                barrier.bounds.min.x - rightOffset - 0.02f;

            float centerY = transform.TransformPoint(
                exitTrigger.offset).y;

            float halfHeight =
                exitTrigger.size.y *
                Mathf.Abs(transform.lossyScale.y) * 0.5f;

            bool overlapsHeight =
                centerY + halfHeight > barrier.bounds.min.y &&
                centerY - halfHeight < barrier.bounds.max.y;

            if (overlapsHeight &&
                currentX <= stopX &&
                nextX >= stopX)
            {
                // 墙挡住门，但不会锁死门的状态。
                // 墙消失以后，门仍能继续逃跑。
                nextX = stopX;
            }
        }

        Vector3 position = transform.position;
        position.x = nextX;
        transform.position = position;
    }

    private bool IsWallSolid()
    {
        return barrier != null &&
               barrier.gameObject.activeInHierarchy &&
               barrier.enabled &&
               !barrier.isTrigger;
    }
}