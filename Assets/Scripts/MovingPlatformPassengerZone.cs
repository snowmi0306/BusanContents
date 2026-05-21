using UnityEngine;

public class MovingPlatformPassengerZone : MonoBehaviour
{
    [SerializeField] private Transform platformRoot;

    private void Reset()
    {
        platformRoot = transform.parent;
    }

    private void Awake()
    {
        if (platformRoot == null)
        {
            platformRoot = transform.parent;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayController player = other.GetComponentInParent<PlayController>();

        if (player == null)
            return;

        player.transform.SetParent(platformRoot, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayController player = other.GetComponentInParent<PlayController>();

        if (player == null)
            return;

        if (player.transform.parent == platformRoot)
        {
            player.transform.SetParent(null, true);
        }
    }
}