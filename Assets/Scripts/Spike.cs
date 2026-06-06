using UnityEngine;

public class Spike : MonoBehaviour
{
    private const float DamageCooldown = 0.5f;

    private PlayController player;
    private float lastDamageTime = -1f;

    private void Start()
    {
        player = FindObjectOfType<PlayController>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || player == null)
        {
            return;
        }

        if (Time.time - lastDamageTime < DamageCooldown)
        {
            return;
        }

        player.OnObstacleHit(transform.position);
        lastDamageTime = Time.time;
    }
}
