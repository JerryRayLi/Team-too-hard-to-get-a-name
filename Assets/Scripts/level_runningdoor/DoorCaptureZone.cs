using UnityEngine;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(BoxCollider2D))]
public class DoorCaptureZone : MonoBehaviour
{
    public HerdingDoor door;
    public SpriteRenderer indicator;
    public ColorLayerManager colorManager;

    private BoxCollider2D captureArea;
    private bool captured;

    private void Awake()
    {
        captureArea = GetComponent<BoxCollider2D>();
        captureArea.isTrigger = true;
    }

    private void Update()
    {
        if (captured)
            return;

        // 你当前项目中，IsBlueActive 为 true 时显示红色背景。
        bool trapActive =
            colorManager != null && colorManager.IsBlueActive;

        if (indicator != null)
        {
            indicator.color = trapActive
                ? Color.yellow
                : Color.gray;
        }

        if (!trapActive || door == null ||
            !captureArea.enabled || Time.deltaTime <= 0f)
        {
            return;
        }

        if (captureArea.OverlapPoint(
            (Vector2)door.transform.position))
        {
            captured = true;
            door.Capture();

            if (indicator != null)
                indicator.color = Color.green;
        }
    }
}