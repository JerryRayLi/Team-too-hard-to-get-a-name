using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerRespawn : MonoBehaviour
{
    public static int DeathCount { get; private set; }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            DeathCount++;
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        }
    }

    public void Respawn()
    {
        DeathCount++;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    public static void ResetDeathCount()
    {
        DeathCount = 0;
    }
}
