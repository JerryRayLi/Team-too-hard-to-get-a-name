using UnityEngine;
using UnityEngine.InputSystem;

public class ColorLayerManager : MonoBehaviour
{
    public GameObject[] redObjects = new GameObject[0];
    public GameObject[] blueObjects = new GameObject[0];

    private bool redLayerActive = true;

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
                ? new Color(48f / 255f, 122f / 255f, 189f / 255f) // #307ABD
                : new Color(210f / 255f, 57f / 255f, 57f / 255f);   // #D23939
        }
    }
}