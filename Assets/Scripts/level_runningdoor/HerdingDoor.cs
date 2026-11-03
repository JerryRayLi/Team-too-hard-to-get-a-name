using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(LevelExit))]
public class HerdingDoor : MonoBehaviour
{
    [Header("摄像头")]
    public DoorCameraVision vision;

    [Header("移动")]
    public float fleeSpeed = 4f;
    public float reactionTime = 0.4f;
    public float blindRunTime = 0.5f;

    [Header("逃跑边界")]
    public float leftLimit = -6f;
    public float rightLimit = 6f;

    private BoxCollider2D exitTrigger;

    private bool previouslyVisible;
    private bool running;
    private bool captured;

    private float noticedAt;
    private float lastSeenAt;
    private float direction = 1f;

    private void Start()
    {
        exitTrigger = GetComponent<BoxCollider2D>();
        exitTrigger.isTrigger = true;

        // 未被捕捉时锁住出口，允许玩家穿过门绕后。
        exitTrigger.enabled = false;
    }

    private void Update()
    {
        if (captured || vision == null ||
            vision.player == null || Time.deltaTime <= 0f)
        {
            return;
        }

        bool visible = vision.CanSeePlayer;

        if (visible)
        {
            if (!previouslyVisible)
            {
                noticedAt = Time.time;
                running = false;
            }

            lastSeenAt = Time.time;

            // 玩家在左边，门向右跑；反之向左跑。
            float difference =
                transform.position.x - vision.player.position.x;

            if (Mathf.Abs(difference) > 0.05f)
                direction = Mathf.Sign(difference);

            if (Time.time - noticedAt >= reactionTime)
                running = true;
        }
        else if (Time.time - lastSeenAt >= blindRunTime)
        {
            running = false;
        }

        previouslyVisible = visible;

        if (!running)
            return;

        float nextX = transform.position.x
            + direction * fleeSpeed * Time.deltaTime;

        Vector3 position = transform.position;

        position.x = Mathf.Clamp(
            nextX,
            Mathf.Min(leftLimit, rightLimit),
            Mathf.Max(leftLimit, rightLimit)
        );

        transform.position = position;
    }

    // 捕捉区会调用这个方法：停住门并解锁出口。
    public void Capture()
    {
        if (captured)
            return;

        captured = true;
        running = false;

        if (exitTrigger == null)
            exitTrigger = GetComponent<BoxCollider2D>();

        exitTrigger.enabled = true;

        if (vision != null)
        {
            vision.enabled = false;

            if (vision.cameraEye != null)
                vision.cameraEye.color = Color.green;
        }
    }
}