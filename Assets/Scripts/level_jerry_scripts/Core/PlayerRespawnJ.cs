using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerRespawnJ : MonoBehaviour
{
    public static int DeathCount { get; private set; }

    public Transform respawnPoint;

    private Rigidbody2D rb;
    private bool isRespawning;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        
        DeathCount = 0;
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartLevel();
        }
    }

    public void Respawn()
    {
        if (isRespawning)
            return;

        isRespawning = true;

        DeathCount++;
        PlaySessionStats.RecordDeath();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        transform.position = respawnPoint.position;

        isRespawning = false;
    }

    private void RestartLevel()
    {
        DeathCount++;
        PlaySessionStats.RecordDeath();

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().path
        );
    }

    public static void ResetDeathCount()
    {
        DeathCount = 0;
    }
}
