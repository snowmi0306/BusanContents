using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Spike : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 0.5f;

    private float lastDamageTime = float.NegativeInfinity;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Time.time - lastDamageTime < damageCooldown)
        {
            return;
        }

        PlayController player = collision.GetComponentInParent<PlayController>();
        if (player == null)
        {
            return;
        }

        Debug.Log("가시에 닿았습니다");
        player.TakeDamage(damage);
        lastDamageTime = Time.time;
    }
}
