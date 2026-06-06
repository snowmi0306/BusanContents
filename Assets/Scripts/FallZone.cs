using UnityEngine;

public class FallZone : MonoBehaviour
{
    private PlayController player;

    private void Start()
    {
        player = FindObjectOfType<PlayController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && player != null)
        {
            Debug.Log("¶³¾îÁü!");
            player.OnFallZoneHit();
        }
    }
}