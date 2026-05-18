using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private bool triggerOnce;

    private bool isActivated;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggerOnce && isActivated)
        {
            return;
        }

        PlayController player = collision.GetComponentInParent<PlayController>();
        if (player == null)
        {
            return;
        }

        player.SetCheckpoint(transform.position);
        isActivated = true;
    }
}
