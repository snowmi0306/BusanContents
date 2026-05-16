using UnityEngine;

public class Checkpoint : MonoBehaviour
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
            player.SetCheckpoint(transform.position);
        }
    }
}