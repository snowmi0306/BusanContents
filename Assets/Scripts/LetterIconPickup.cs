using UnityEngine;

public class LetterIconPickup : MonoBehaviour
{
    [SerializeField] private bool disableInsteadOfDestroy = false;

    private bool pickedUp;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (pickedUp || other == null)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlayerLetterInventory inventory = other.GetComponentInParent<PlayerLetterInventory>();
        if (inventory == null)
            return;

        pickedUp = true;
        inventory.AddLetter(1);
        AudioManager.PlaySfx("sfx_item_letter");

        Debug.Log($"Letter icon picked up: {inventory.GetCurrentLetterCount()}");

        if (disableInsteadOfDestroy)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }
}
