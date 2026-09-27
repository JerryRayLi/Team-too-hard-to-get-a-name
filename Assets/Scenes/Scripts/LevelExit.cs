using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExit : MonoBehaviour
{
    private bool isLoading;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading)
        {
            return;
        }

        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
        {
            return;
        }

        isLoading = true;

        int nextScene =
            SceneManager.GetActiveScene().buildIndex + 1;

        if (nextScene >= SceneManager.sceneCountInBuildSettings)
        {
            nextScene = 0;
        }

        SceneManager.LoadScene(nextScene);
    }
}