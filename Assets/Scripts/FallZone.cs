using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FallZone : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayController player = collision.GetComponentInParent<PlayController>();
        if (player == null)
        {
            return;
        }

        Debug.Log("낙하 구역 진입");
        player.TakeDamage(damage);
    }
}
