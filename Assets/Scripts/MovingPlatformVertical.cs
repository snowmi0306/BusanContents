using UnityEngine;

public class VerticalPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    public float speed = 2f;       // 이동 속도
    public float bottomY = 0f;     // 최하단 위치 (n)
    public float topY = 5f;        // 최상단 위치 (m)

    private bool isMovingUp = true;

    void Update()
    {
        // 이동 로직
        if (isMovingUp)
        {
            transform.Translate(Vector2.up * speed * Time.deltaTime);
            if (transform.position.y >= topY)
            {
                isMovingUp = false; // 꼭대기에 도달하면 아래로 방향 전환
            }
        }
        else
        {
            transform.Translate(Vector2.down * speed * Time.deltaTime);
            if (transform.position.y <= bottomY)
            {
                isMovingUp = true; // 바닥에 도달하면 위로 방향 전환
            }
        }
    }

    
}