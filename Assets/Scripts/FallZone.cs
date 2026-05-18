using UnityEngine;

public class FallZone : MonoBehaviour
{
    private PlayController player;

    void Start()
    {
        player = FindObjectOfType<PlayController>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && player != null)
        {
            Debug.Log("!");
            player.TakeDamage(1);
        }
    }
}