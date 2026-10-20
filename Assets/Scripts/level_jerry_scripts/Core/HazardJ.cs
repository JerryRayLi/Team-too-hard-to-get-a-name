using UnityEngine;

public class HazardJ : MonoBehaviour
{
    public GameObject corpsePrefab;
    public Transform respawnPoint;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRespawnJ player = other.GetComponent<PlayerRespawnJ>();

        if (player == null)
            return;

        player.TryRespawn(corpsePrefab);
    }
}
