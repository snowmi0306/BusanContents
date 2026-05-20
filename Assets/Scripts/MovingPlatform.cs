using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    // 플레이어가 발판 위에 올라왔을 때
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 충돌한 오브젝트의 태그가 "Player"인지 확인
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어의 부모를 이 발판(transform)으로 설정합니다.
            collision.transform.SetParent(transform);
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