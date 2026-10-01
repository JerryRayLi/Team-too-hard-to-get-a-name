using UnityEngine;
using UnityEngine.InputSystem;

public class ColorLayerManager : MonoBehaviour
{
    public GameObject[] redObjects = new GameObject[0];
    public GameObject[] blueObjects = new GameObject[0];

    private bool redLayerActive = true;
    public bool IsBlueActive => !redLayerActive;
    private void Start()
    {
        ApplyLayer();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame ||
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            redLayerActive = !redLayerActive;
            ApplyLayer();
        }
    }

    private void ApplyLayer()
    {
        foreach (GameObject item in redObjects)
        {
            item.SetActive(redLayerActive);
        }

        foreach (GameObject item in blueObjects)
        {
            item.SetActive(!redLayerActive);
        }

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = redLayerActive
                ? new Color(63f / 255f, 101f / 255f, 112f / 255f) // #3F6570
                : new Color(138f / 255f, 80f / 255f, 74f / 255f);  // #8A504A
        }
    }
}   
