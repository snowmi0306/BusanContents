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

        bool healed = player.TryHeal(healAmount);

        if (healed)
            Destroy(gameObject);
    }
}