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
        if (!collision.CompareTag("Player"))
        {
            return;
        }

        AudioManager.PlaySfx("sfx_fall_respawn");

        PlayController hitPlayer = collision.GetComponentInParent<PlayController>();
        if (hitPlayer == null)
            hitPlayer = player;

        if (hitPlayer == null)
            return;

        Debug.Log("¶³¾îÁü!");
        hitPlayer.OnFallZoneHit();
    }
}