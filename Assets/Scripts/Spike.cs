using UnityEngine;

public class Spike : MonoBehaviour
{
    private PlayController player;
    private float damageCooldown = 0.5f;
    private float lastDamageTime = -1f;

    void Start()
    {
        player = FindObjectOfType<PlayController>();
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && player != null)
        {
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                player.TakeDamage(1, false);
                player.TakeDamage(1);
                lastDamageTime = Time.time;
            }
        }
    }
}