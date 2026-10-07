using UnityEngine;

public class PlatformTrigger : MonoBehaviour
{
    public MovingPlatform platform;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            platform.enabled = true;
        }
    }
}