using UnityEngine;

public class LetterIconPickup : MonoBehaviour
{
    [SerializeField] private bool disableInsteadOfDestroy = false;

    private bool pickedUp;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (pickedUp || other == null)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerLetterInventory inventory = other.GetComponentInParent<PlayerLetterInventory>();
        if (inventory == null)
        {
            return;
        }

        pickedUp = true;
        inventory.AddLetter(1);

        Debug.Log($"편지 아이콘 획득: {inventory.GetCurrentLetterCount()}/{inventory.GetRequiredLetterCount()}");

        if (disableInsteadOfDestroy)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
