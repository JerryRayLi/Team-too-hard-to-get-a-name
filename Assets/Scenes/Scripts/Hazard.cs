using UnityEngine;

public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRespawn player = other.GetComponent<PlayerRespawn>();

        if (player != null)
        {
            player.Respawn();
        }
    }
}