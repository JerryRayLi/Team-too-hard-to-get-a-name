using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameHUDJ : MonoBehaviour
{
    private void Start()
    {
        GameHUD.CreateControls(transform, GameHUD.GetExtraControls(gameObject.scene.name));
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToMainMenu();
        }
    }

    private void OnGUI()
    {
        GameHUD.DrawHeader(ReturnToMainMenu);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerRespawnJ.ResetDeathCount();
        SceneManager.LoadScene("MainMenu");
    }
}
