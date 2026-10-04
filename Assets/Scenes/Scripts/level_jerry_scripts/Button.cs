using UnityEngine;

public class Button : MonoBehaviour
{
    public Door door;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Corpse>() != null)
        {
            door.Open();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Corpse>() != null)
        {
            door.Close();
        }
    }
}