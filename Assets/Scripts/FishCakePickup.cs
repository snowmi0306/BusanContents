using UnityEngine;

public class FishCakePickup : MonoBehaviour
{
    [SerializeField] private int healAmount = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayController player = other.GetComponent<PlayController>();
        if (player == null)
            return;

        // 플레이어의 체력 회복을 시도합니다.
        // (체력이 꽉 차 있다면 내부적으로 무시되지만 함수는 정상 실행됩니다)
        player.TryHeal(healAmount);

        // 체력을 회복했든 안 했든 상관없이 아이템 오브젝트를 파괴합니다 (먹어짐 처리).
        Destroy(gameObject);
    }
}