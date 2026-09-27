using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameComplete : MonoBehaviour
{
    private void Start()
    {
        if (Camera.main != null)
        {
            Camera.main.backgroundColor =
                new Color(0.08f, 0.10f, 0.16f);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            PlayerRespawn.ResetDeathCount();
            SceneManager.LoadScene(0);
        }
    }

    private void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 48;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = 24;
        bodyStyle.alignment = TextAnchor.MiddleCenter;
        bodyStyle.normal.textColor = Color.white;

        GUI.Label(
            new Rect(0, Screen.height / 2f - 110, Screen.width, 70),
            "YOU WIN!",
            titleStyle
        );

        GUI.Label(
            new Rect(0, Screen.height / 2f - 25, Screen.width, 45),
            "Total deaths: " + PlayerRespawn.DeathCount,
            bodyStyle
        );

        GUI.Label(
            new Rect(0, Screen.height / 2f + 25, Screen.width, 45),
            "Press R to restart",
            bodyStyle
        );
    }
}