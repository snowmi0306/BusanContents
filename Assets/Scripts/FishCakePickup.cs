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

        player.TryHeal(healAmount);
        AudioManager.PlaySfx("sfx_item_fishcake", 1f, 0.5f);

        Destroy(gameObject);
    }
}
