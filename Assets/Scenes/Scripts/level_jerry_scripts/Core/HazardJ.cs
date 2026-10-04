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

        
        Instantiate(
            corpsePrefab,
            player.transform.position,
            Quaternion.Euler(0f, 0f, 90f)
        );

        
        player.Respawn();
    }
}