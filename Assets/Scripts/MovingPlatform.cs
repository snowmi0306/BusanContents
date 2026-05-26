using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    // 충돌이 '유지'되고 있을 때도 계속 부모를 체크해 줍니다.
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어가 발판보다 위쪽에 있을 때만 자식으로 삼음 (벽에 부딪힐 때 자식되는 것 방지)
            if (collision.transform.position.y > transform.position.y)
            {
                collision.transform.SetParent(transform);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 부모 관계를 안전하게 해제
            if (collision.transform.parent == transform)
            {
                collision.transform.SetParent(null);
            }
        }
    }
}