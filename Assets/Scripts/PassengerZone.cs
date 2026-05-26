using UnityEngine;

public class PassengerZone : MonoBehaviour
{
    // 플레이어가 발판 위에 올라왔을 때
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        foreach (var c in collision.contacts)
        {
            // 플레이어가 발판 위에 있을 때(플랫폼 입장에서 위쪽으로 힘 받음)
            if (c.normal.y < -0.2f)
            {
                collision.transform.SetParent(transform);
                return;
            }
        }
    }

    // 플레이어가 발판에서 점프하거나 벗어났을 때
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 부모 관계를 끊어줍니다. (원래대로 독립)
            collision.transform.SetParent(null);
        }
    }
}