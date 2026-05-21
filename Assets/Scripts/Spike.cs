using UnityEngine;

public class Spike : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float damageCooldown = 0.5f;
    private float lastDamageTime = -1f;

    void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
        {
            return;
        }

        if (Time.time - lastDamageTime < damageCooldown)
        {
            return;
        }

        PlayController player = collision.GetComponent<PlayController>();
        if (player == null)
        {
            return;
        }

        player.ApplyKnockback(transform.position);
        player.TakeDamage(damageAmount, false);
        lastDamageTime = Time.time;
    }
}
