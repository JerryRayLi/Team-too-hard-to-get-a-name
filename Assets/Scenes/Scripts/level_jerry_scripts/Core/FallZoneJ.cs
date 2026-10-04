using UnityEngine;

public class FallZoneJ : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRespawnJ player = other.GetComponent<PlayerRespawnJ>();

        if (player != null)
        {
            player.Respawn();
        }
    }
}