using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        SetPlayerParenting(collision, transform);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        SetPlayerParenting(collision, null);
    }

    private static bool IsPlayerCollision(Collision2D collision)
    {
        return collision.gameObject.CompareTag("Player");
    }

    private static void SetPlayerParenting(Collision2D collision, Transform parent)
    {
        if (!IsPlayerCollision(collision))
        {
            return;
        }

        collision.transform.SetParent(parent);
    }
}
